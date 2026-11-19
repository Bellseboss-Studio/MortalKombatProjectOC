using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// PlayerCombat — HITBOX partial (T0.1 split): step/swing probes, targeting,
    /// aim/facing and the dev gizmos. Same type as <see cref="PlayerCombat"/>; the
    /// code below is the pre-split code, moved verbatim (no behavior change).
    /// </summary>
    public sealed partial class PlayerCombat
    {
        // Aim-assist runtime (m1-aim-assist): the XZ direction to turn toward at
        // attack entry + the seconds left of the entry correction window. Zero
        // direction = no active correction. SetFacing is incremental, so the
        // corrector ITERATES over the swing opening instead of one snap.
        private Vector3 aimDir;
        private float aimCorrectRemaining;

        // Within-step lunge COMMIT (target-switch iteration): the world XZ direction the
        // active step's lunge was committed to at step entry (the soft-locked target, or
        // the facing when there was none). ApplyLunge uses THIS instead of the live facing
        // so the body can re-orient between steps without the lunge drifting mid-step.
        private Vector3 lungeDir;

        // Reused candidate buffer for the step probe (combat honesty): the probe collects
        // the receivers that pass the vertical band + forward filter BEFORE hitting, then
        // applies the targeting policy (up to maxTargets victims, best-aligned first).
        // Never allocates per frame.
        private readonly List<HitReceiver> probeCandidates = new List<HitReceiver>(8);

        /// <summary>
        /// Aim-assist tuning used by the ATTACK entry (read-only debug seam). The dash
        /// has its OWN definition now (<c>PlayerDash.dashAimAssist</c>), so the attack and
        /// the dash tune their range/cone independently. Null / disabled = forward-only.
        /// </summary>
        public AimAssistDefinition AimAssistDef => aimAssist;

        // ------------------------------------------------------------------
        // Aim-assist (m1-aim-assist, decisions a/b/e): soft trajectory corrector.
        // At attack entry, pick the nearest alive HitReceiver inside the frontal
        // cone + range and turn visual.Facing toward it over the swing opening.
        // Facing is the SINGLE point: ApplyLunge (reads FacingVector) and
        // HitboxOrigin both follow -> lunge + hitbox redirect with NO motor change.
        // ------------------------------------------------------------------

        /// <summary>
        /// Computes the aim direction ONCE at attack entry (NOT continuous homing):
        /// nearest alive target inside the ±coneHalfAngle frontal cone and within
        /// [minRange, maxRange] METERS. On a hit, stores the direction and starts the
        /// entry correction window (worst-case cone error / turnSpeed). No target or
        /// disabled -> no-op (forward-only behavior intact). Applies in air/wall too.
        /// </summary>
        private void ApplyAimAssist()
        {
            ClearAimAssist();
            // Default lunge commit: the facing this step starts on. Overridden below with
            // the soft-locked target direction when one is found.
            lungeDir = FacingVector();
            if (visual == null) return;

            // Shared selection (slice 3quinquies, Code/Combat/AimAssist.cs): the dash
            // direction uses the EXACT same cone + range + nearest-alive-target math.
            // CONE AXIS = the player's AIM (target-switch iteration): the cone is centered
            // where the player POINTS (camera-relative stick), so pointing at another enemy
            // selects it (the nearest enemy inside THAT cone). Neutral stick = the current
            // facing, i.e. the pre-existing behavior.
            if (!AimAssist.TryFindAimDir(transform.position, rb, AimAxis(), aimAssist, out Vector3 aim))
                return;

            aimDir = aim;

            // SetFacing is incremental (~12°/frame at 720°/s): a single call cannot
            // align. Iterate over the swing opening — window sized to the worst-case
            // cone error at the configured turn speed.
            aimCorrectRemaining = Mathf.Clamp(aimAssist.coneHalfAngleDeg, 0f, 180f)
                / Mathf.Max(1f, aimAssist.turnSpeedDegPerSec);
            // The lunge is COMMITTED to the chosen target for the whole step, so the
            // between-steps re-orientation below never drags it mid-step.
            lungeDir = aim;
            visual.SetFacing(aimDir);
        }

        /// <summary>
        /// Iterates <c>visual.SetFacing</c> toward the stored aim direction during the
        /// swing OPENING (a smooth turn), then stops — NOT continuous homing. Scaled
        /// time: hitstop freezes the correction with the rest of the swing.
        /// </summary>
        private void UpdateAimCorrection()
        {
            if (aimCorrectRemaining <= 0f) return;
            aimCorrectRemaining -= Time.deltaTime;
            if (visual != null) visual.SetFacing(aimDir);
        }

        private void ClearAimAssist()
        {
            aimDir = Vector3.zero;
            aimCorrectRemaining = 0f;
        }

        /// <summary>
        /// MID-COMBO facing re-orientation (target-switch, DMC/Bayonetta feel): while the
        /// active step is PAST its strike (hit window closed) and inside its cancel window,
        /// keep turning the facing toward the player's AIM + soft-lock — the nearest alive
        /// enemy inside the cone centered on the stick. This is what lets the player swing
        /// the body toward ANOTHER enemy mid-combo so the NEXT step's hit goes there. The
        /// lunge stays committed (<see cref="ApplyLunge"/> uses <see cref="lungeDir"/>), so
        /// the body pivots without drifting. It only turns when an enemy is soft-locked:
        /// free-facing the raw stick would rotate the facing under the player's own "Back"
        /// input and break the facing-relative launcher. SetFacing is incremental, so
        /// calling it every frame converges over the window.
        /// </summary>
        private void UpdateMidComboFacing()
        {
            if (visual == null) return;
            if (aimCorrectRemaining > 0f) return; // the entry correction owns the opening
            if (IsInsideStepWindow()) return;     // committed through the strike
            if (!IsInCancelWindow()) return;      // only between steps / in the chain tail

            if (!AimAssist.TryFindAimDir(transform.position, rb, AimAxis(), aimAssist, out Vector3 lockDir))
                return; // no enemy in the aim cone -> keep the committed facing

            visual.SetFacing(lockDir);
        }

        /// <summary>
        /// Victims this step may damage: the live override when set, else the data cap
        /// (clamped to &gt;= 1 — an absent serialized key reads 0).
        /// </summary>
        private int Targets(in ComboStep step)
        {
            int over = TargetsOverride;
            return over > 0 ? over : Mathf.Max(1, step.maxTargets);
        }

        /// <summary>
        /// Scales the step hitbox ONCE, at the call site, so the probe and the gizmo
        /// consume the SAME radius/offset (what you see is what the probe tests).
        /// </summary>
        private void ScaledHitbox(in ComboStep step, out float radius, out Vector3 offset)
        {
            float reach = ReachScale;
            radius = step.hitboxRadius * reach;
            offset = step.hitboxOffset * reach;
        }

        /// <summary>Same scaling for the grey-box standalone swing definition.</summary>
        private void ScaledHitbox(AttackDefinition def, out float radius, out Vector3 offset)
        {
            float reach = ReachScale;
            radius = def.hitboxRadius * reach;
            offset = def.hitboxOffset * reach;
        }

        // ------------------------------------------------------------------
        // Shared hitbox probe (slice-1 seam extended 1:1 to combo steps)
        // ------------------------------------------------------------------

        private bool IsInsideStepWindow()
        {
            float duration = EffectiveDuration(currentStep);
            float t0 = Mathf.Clamp01(currentStep.hitNorm.x) * duration;
            float t1 = Mathf.Clamp01(currentStep.hitNorm.y) * duration;
            return chain.stepTime >= t0 && chain.stepTime <= t1;
        }

        private bool IsInsideActiveWindow()
        {
            float t0 = Mathf.Clamp01(current.hitNorm.x) * current.duration;
            float t1 = Mathf.Clamp01(current.hitNorm.y) * current.duration;
            return swingTime >= t0 && swingTime <= t1;
        }

        /// <summary>
        /// Sphere probe against <see cref="HitReceiver"/> targets, excluding the
        /// player's own Rigidbody (Face child collider shares it). Forward-only:
        /// the origin is offset along the facing, so a target behind never counts.
        /// Returns true when at least one target was hit.
        /// <paramref name="moveKey"/> (style-meter seam): when non-null, every NEWLY
        /// hit receiver fires <see cref="OnStepConnected"/> once with it.
        /// <paramref name="halfHeight"/> (B2, combat honesty): when &gt; 0, the probe is a
        /// CYLINDER — a victim only counts when its collider bounds overlap
        /// <c>origin.y ± halfHeight</c>. 0 = legacy sphere (no vertical filter), so old
        /// assets that omit the field keep their behavior.
        /// <paramref name="maxTargets"/> (B2): the step's victim CAP. 1 = ONE victim per
        /// step (best aim-lock / nearest in front); N &gt; 1 = cleave up to N victims.
        /// The probe COLLECTS before hitting, so a step can never damage more victims
        /// than its cap across frames because the first one stayed inside the sphere.
        /// </summary>
        /// invulnerable). It does NOT change `connected`: hitstop and the style seam keep the
        /// old "a victim was reached" semantics, so V1 behaves exactly as before.
        /// </summary>
        private bool ProbeAt(Vector3 origin, float radius, int damage, float knockback, float launchImpulse,
            out HitOutcome outcome, string moveKey = null, float halfHeight = 0f, int maxTargets = 1)
        {
            outcome = HitOutcome.Ignored;
            Collider[] cols = Physics.OverlapSphere(
                origin, radius, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            probeCandidates.Clear();

            Vector3 forwardAxis = lungeDir.sqrMagnitude > 0.0001f ? lungeDir : FacingVector();
            for (int i = 0; i < cols.Length; i++)
            {
                var col = cols[i];
                if (col == null) continue;
                if (col.attachedRigidbody == rb) continue; // own capsule / Face trigger

                var receiver = col.GetComponentInParent<HitReceiver>();
                if (receiver == null) continue;
                if (hitThisStep.Contains(receiver)) continue;     // already hit this step/swing
                if (probeCandidates.Contains(receiver)) continue; // two colliders -> one candidate

                // VERTICAL BAND (cylinder): reject victims whose collider is entirely
                // above/below the band around the sphere centre. 0 = off (legacy).
                if (halfHeight > 0f)
                {
                    Bounds bounds = col.bounds;
                    if (bounds.max.y < origin.y - halfHeight || bounds.min.y > origin.y + halfHeight)
                        continue;
                }

                Vector3 to = receiver.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude < 0.0001f) to = forwardAxis;
                to.Normalize();

                // FORWARD-ONLY: the committed step axis (aim-assist result at entry) is
                // the targeting axis, so a target behind the swing never counts.
                if (Vector3.Dot(to, forwardAxis) < 0f) continue;

                probeCandidates.Add(receiver);
            }

            // CAP (default 1): once a victim landed this step, never pick a SECOND one on a
            // later frame (the old loop could cleave across frames).
            if (maxTargets <= 1 && hitThisStep.Count > 0) return false;

            // Budget = the step's remaining cap (victims already hit this step already
            // consumed it). Select best-aligned first, swap-removing as we go: O(1) per
            // pick, no Sort, no LINQ, no per-frame allocation.
            int budget = Mathf.Max(0, Mathf.Max(1, maxTargets) - hitThisStep.Count);
            int hits = 0;
            while (hits < budget && probeCandidates.Count > 0)
            {
                int i = SelectBestIndex(forwardAxis);
                if (i < 0) break;
                HitReceiver pick = probeCandidates[i];
                probeCandidates[i] = probeCandidates[probeCandidates.Count - 1];
                probeCandidates.RemoveAt(probeCandidates.Count - 1);
                if (HitCandidate(pick, damage, knockback, launchImpulse, moveKey, out HitOutcome landed))
                {
                    hits++; // UNCHANGED: "a victim was reached" (hitstop / style seam semantics)
                    if (landed == HitOutcome.Hit) outcome = HitOutcome.Hit;
                    else if (landed == HitOutcome.Blocked && outcome != HitOutcome.Hit) outcome = HitOutcome.Blocked;
                }
            }
            bool connected = hits > 0;

            // One impact per probe call (not per receiver).
            if (connected) AudioDirector.Play(hitCue, transform.position);

            return connected;
        }

        /// <summary>
        /// Applies the hit to ONE receiver (dedup via <see cref="hitThisStep"/>) and fires
        /// the style seam. Returns false when the receiver was already hit this step.
        /// <paramref name="outcome"/> is what the receiver REPORTED (combo-grammar T2.2):
        /// Hit / Blocked / Ignored — a receiver that is invulnerable or already dead still
        /// counts as "reached" for the return value (old behavior), but reports Ignored.
        /// </summary>
        private bool HitCandidate(HitReceiver receiver, int damage, float knockback, float launchImpulse,
            string moveKey, out HitOutcome outcome)
        {
            outcome = HitOutcome.Ignored;
            if (!hitThisStep.Add(receiver)) return false; // one hit per target per step/swing

            Vector3 to = receiver.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) to = FacingVector();
            to.Normalize();

            // REMATE (R4): a CONNECTING Closer staggers BEFORE the hit. Stagger() calls
            // StopHorizontal(), so running it first lets the push that Feedback writes inside
            // TakeHit survive the stagger. Bosses expose no EnemyController => no-op.
            if (comboActive && ActiveMatrix != null && ActiveMatrix.RoleOf(CurrentMove) == ComboRole.Closer)
            {
                EnemyController enemy = receiver.GetComponentInParent<EnemyController>();
                if (enemy != null) enemy.Stagger();
            }

            // LAUNCHER (slice 3nonies): the vertical impulse is passed SEPARATELY
            // (not baked into the XZ push) so HitReceiver can drive the juggle-lite
            // arc (rise -> fall -> land) with its own gravity. 0 = normal hit.
            outcome = receiver.TakeHit(damage, to * knockback, launchImpulse);
            if (moveKey != null) OnStepConnected?.Invoke(moveKey);
            return true;
        }

        /// <summary>
        /// Targeting policy (B2): the best candidate is the one most aligned with the
        /// committed step axis (aim-soft-lock first, else nearest in the swing); a tie on
        /// the alignment breaks on the NEAREST (<c>sqrMagnitude</c>). Returns the INDEX in
        /// <see cref="probeCandidates"/> so the caller can swap-remove it in O(1); -1 when
        /// the buffer is empty.
        /// </summary>
        private int SelectBestIndex(Vector3 forwardAxis)
        {
            int bestIndex = -1;
            float bestDot = float.NegativeInfinity;
            float bestSqr = float.PositiveInfinity;
            for (int i = 0; i < probeCandidates.Count; i++)
            {
                HitReceiver candidate = probeCandidates[i];
                Vector3 to = candidate.transform.position - transform.position;
                to.y = 0f;
                float sqr = to.sqrMagnitude;
                Vector3 dir = sqr > 0.0001f ? to / Mathf.Sqrt(sqr) : forwardAxis;
                float dot = Vector3.Dot(dir, forwardAxis);

                if (bestIndex < 0 || dot > bestDot || (Mathf.Approximately(dot, bestDot) && sqr < bestSqr))
                {
                    bestIndex = i;
                    bestDot = dot;
                    bestSqr = sqr;
                }
            }
            return bestIndex;
        }

        private void ProbeHitboxStep()
        {
            float launch = currentStep.launch ? Mathf.Max(0f, currentStep.launchImpulse) : 0f;
            string moveKey = activeCombo != null ? activeCombo.name + "#" + chain.stepIndex : null;
            // LIVE reach: the SAME scaled geometry feeds the probe and the gizmo.
            ScaledHitbox(currentStep, out float radius, out Vector3 offset);
            bool connected = ProbeAt(HitboxOrigin(radius, offset),
                radius, AppliedDamage(currentStep), currentStep.knockback, launch,
                out HitOutcome outcome, moveKey, currentStep.hitboxHalfHeight, Targets(currentStep));
            if (connected)
            {
                chain.connectedThisStep = true; // wider cancel window (hit-cancel)
                BeginHitstop(currentStep.hitstopMs * HitstopScale);
            }

            // GRAMMAR feedback: the step's OUTCOME decides the asymmetric window (Hit = long,
            // whiff/blocked = short) and the connected-hit counter behind the damage curve.
            // Written unconditionally: V1 never reads Outcome.
            chain.MarkOutcome(connected ? outcome : HitOutcome.Ignored);
        }

        private void ProbeHitbox()
        {
            string moveKey = current != null ? current.name + "#0" : null;
            ScaledHitbox(current, out float radius, out Vector3 offset);
            if (ProbeAt(HitboxOrigin(radius, offset),
                    radius, current.damage, current.knockback, 0f, out HitOutcome _, moveKey, 0f, 1))
            {
                BeginHitstop(current.hitstopMs * HitstopScale);
            }
        }

        // ------------------------------------------------------------------
        // Dev visualization (decision: make the grey-box hitbox legible).
        // The origin math below is a DELIBERATE copy of the probe so what you
        // see is exactly what the sphere query tests.
        // ------------------------------------------------------------------

        /// <summary>World-space hitbox center for radius/offset — mirrors the probe.</summary>
        private Vector3 HitboxOrigin(float radius, Vector3 offset)
        {
            Vector3 facing = FacingForGizmos();
            return transform.position
                   + Vector3.up * offset.y
                   + facing * offset.z;
        }

        private Vector3 FacingVector()
        {
            Vector3 facing = visual != null ? visual.Facing : transform.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;
            return facing.normalized;
        }

        /// <summary>
        /// World-space AIM direction on XZ (unit) used as the soft-lock CONE AXIS: the
        /// camera-relative MOVE stick direction when the stick is past its deadzone (the
        /// player is pointing somewhere), else the current facing (keep the cone where the
        /// body already looks). Uses the SAME stick-to-world mapping the walk/dash use
        /// (<see cref="CameraRelativeMotion"/>) and the SAME deadzone as the facing-relative
        /// classifier (<see cref="AttackDirectionClassifier"/>), so the aim is consistent
        /// across the combat model.
        /// </summary>
        private Vector3 AimAxis()
        {
            Vector3 facing = FacingVector();
            if (input == null) return facing;
            RefreshCamera(); // el basis cambia por sala: releerlo siempre
            Vector2 stick = input.Player.Move.ReadValue<Vector2>();
            if (stick.magnitude < AttackDirectionClassifier.DefaultDeadzone) return facing;
            Vector3 dir = CameraRelativeMotion.WishDirection(cameraTransform, stick);
            return dir.sqrMagnitude > 0.000001f ? dir : facing;
        }

        /// <summary>
        /// Facing for gizmos: uses the cached visual, else resolves it lazily
        /// (Awake does not run in edit mode), else falls back to transform.forward.
        /// </summary>
        private Vector3 FacingForGizmos()
        {
            var v = visual != null ? visual : GetComponent<ICharacterVisual>();
            return FacingVector();
        }

        private void OnDrawGizmos()
        {
            // Live hitbox of the active combo step OR grey-box swing. Red while the
            // active hit window is open, amber otherwise, so the connect frame is obvious.
            if (comboActive)
            {
                Gizmos.color = IsInsideStepWindow()
                    ? Color.red
                    : new Color(1f, 0.6f, 0.1f, 0.6f);
                // LIVE reach: the gizmo consumes the SAME scaled geometry as the probe.
                ScaledHitbox(currentStep, out float stepRadius, out Vector3 stepOffset);
                Vector3 origin = HitboxOrigin(stepRadius, stepOffset);
                Gizmos.DrawWireSphere(origin, stepRadius);
                // CYLINDER (B2): draw the vertical band so the gizmo stays honest about
                // the elevated-target rejection the probe applies.
                if (currentStep.hitboxHalfHeight > 0f)
                {
                    Gizmos.DrawWireCube(origin, new Vector3(
                        stepRadius * 2f,
                        currentStep.hitboxHalfHeight * 2f,
                        stepRadius * 2f));
                }
                return;
            }
            if (current == null) return;
            Gizmos.color = IsInsideActiveWindow()
                ? Color.red
                : new Color(1f, 0.6f, 0.1f, 0.6f);
            ScaledHitbox(current, out float swingRadius, out Vector3 swingOffset);
            Gizmos.DrawWireSphere(HitboxOrigin(swingRadius, swingOffset), swingRadius);
        }

        private void OnDrawGizmosSelected()
        {
            // Selected + idle: show BOTH reaches so light-vs-heavy is comparable.
            DrawDefinitionGizmo(lightAttack, new Color(0.2f, 0.8f, 1f, 0.7f));
            DrawDefinitionGizmo(heavyAttack, new Color(1f, 0.4f, 0f, 0.7f));
        }

        private void DrawDefinitionGizmo(AttackDefinition def, Color color)
        {
            if (def == null) return;
            Gizmos.color = color;
            ScaledHitbox(def, out float radius, out Vector3 offset);
            Gizmos.DrawWireSphere(HitboxOrigin(radius, offset), radius);
        }
    }
}
