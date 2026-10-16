using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// m1-abilities runner (2026-10-15, molde <see cref="PlayerDash"/>): owns the
    /// H1 projectile / H2 area execution, data-driven from <see cref="AbilityDefinition"/>.
    /// Reads the Skill inputs through <see cref="PlayerCombat.ConsumeSkillTap"/> (the
    /// single consumer), so the combat controller stays thin (only the
    /// <c>ConsumeSkillTap</c> + <c>RequestHitstop</c> seams).
    ///
    /// TWO entry paths share the same execution:
    /// 1. STANDALONE — Skill1 (H1) / Skill2 (H2), whenever no combo/swing/ability is
    ///    active and not frozen by hitstop.
    /// 2. COMBO STEP — <see cref="PlayerCombat"/> calls <see cref="PlayFromCombo"/> when
    ///    it enters a <see cref="ComboStep"/> with <c>stepType == Ability</c>; the same
    ///    ability plays inside the chain (the combo owns the step timing).
    ///
    /// Kind is DATA-DRIVEN: <c>projectileSpeed &gt; 0</c> = projectile (H1);
    /// otherwise an area ability (H2) using <c>areaRadius</c> as a ONE-SHOT
    /// <c>OverlapSphere</c> that damages ALL <see cref="HitReceiver"/> once
    /// (multi-hit 12x2 deferred). Damage / hit window come from the SO
    /// (<c>dano</c> / <c>hitNorm</c>); NO Animation Events. Grey-box VFX:
    /// a particle burst on impact/area fire and a transparent ring showing the H2 radius.
    ///
    /// PROJECTILE AIM-ASSIST (m1-abilities): the launch DIRECTION runs the SAME shared
    /// selection the attack (<see cref="PlayerCombat"/>) and dash (<see cref="PlayerDash"/>)
    /// use (<see cref="AimAssist"/>, cone + range, nearest alive <see cref="HitReceiver"/>),
    /// against its OWN <see cref="AimAssistDefinition"/> (<see cref="projectileAimAssist"/>).
    /// A target inside the frontal cone + range corrects the direction ONCE at launch
    /// (NO homing); no target = straight along the facing (current behavior).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerAbilities : MonoBehaviour
    {
        [Header("Ability data (data-driven)")]
        [Tooltip("H1 ability definition (projectile). Skill1 runs it.")]
        [SerializeField] private AbilityDefinition h1;

        [Tooltip("H2 ability definition (area spin). Skill2 runs it.")]
        [SerializeField] private AbilityDefinition h2;

        [Tooltip("CrossFade duration (seconds) entering the Attack_H1 / Attack_H2 states.")]
        [SerializeField] private float crossFadeDuration = 0.08f;

        [Tooltip("Fallback ability duration (seconds) when the definition has no clip assigned (keeps the window readable without a real clip).")]
        [SerializeField] private float fallbackDuration = 0.5f;

        [Tooltip("Hitstop in MILLISECONDS applied when an ability connects (the SO has no hitstop field; this is the ability's shared value).")]
        [SerializeField] private int hitstopMs = 60;

        [Tooltip("Knockback push distance (metres) applied to a victim on connect.")]
        [SerializeField] private float knockback = 0.5f;

        [Tooltip("Aim-assist config for the PROJECTILE launch direction — its OWN definition, tunable independently from the attack/dash correctors (e.g. a longer range for a ranged shot). If a target sits inside the frontal cone + range at launch, the projectile travels toward it; no target = straight along the facing (current behavior). Null / disabled = straight.")]
        [SerializeField] private AimAssistDefinition projectileAimAssist;

        private PlayerCombat combat;
        private PlayerMotor motor;
        private AnimatorVisual animatorVisual;
        private ICharacterVisual visual;
        private Rigidbody rb;

        private AbilityDefinition active;
        private bool activeProjectile;
        private bool busy;
        private bool hitFired;
        private float time;
        private float duration;
        private GameObject ring;

        /// <summary>True while an ability window is running (read-only seam).</summary>
        public bool IsBusy => busy;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            combat = GetComponent<PlayerCombat>();
            motor = GetComponent<PlayerMotor>();
            visual = GetComponent<ICharacterVisual>();
            animatorVisual = GetComponent<AnimatorVisual>();
        }

        private void OnDisable()
        {
            // Rigorous release: never leave a stale ability state / ring if disabled mid-window.
            Stop();
        }

        private void Update()
        {
            if (busy)
            {
                // Scaled time: hitstop (timeScale = 0) freezes the ability too.
                time += Time.deltaTime;
                TickWindow();
                if (time >= duration) Stop();
                return;
            }

            if (combat == null) return;
            if (combat.IsHitstopped) return;   // no ability during the freeze
            if (combat.Attacking) return;      // standalone only when idle (combo-step uses PlayFromCombo)

            ComboInput which;
            if (!combat.ConsumeSkillTap(out which)) return;
            // ACTION GATE (change action-gating): standalone abilities are ground-only —
            // denied in the air, while hanging or in a scripted state (deny by default).
            // Per-verb so Skill1/Skill2 can diverge later; identical rule today.
            PlayerAction action = which == ComboInput.H1 ? PlayerAction.Skill1 : PlayerAction.Skill2;
            if (motor != null && !motor.CanPerform(action)) return;
            AbilityDefinition def = which == ComboInput.H1 ? h1 : h2;
            if (def == null) return;
            StartAbility(def, def.projectileSpeed > 0f);
        }

        /// <summary>
        /// Runs the given ability as a COMBO STEP (called by <see cref="PlayerCombat"/>
        /// when it enters a <c>stepType == Ability</c> step). Kind is data-driven.
        /// </summary>
        public void PlayFromCombo(AbilityDefinition def)
        {
            if (def == null) return;
            StartAbility(def, def.projectileSpeed > 0f);
        }

        /// <summary>
        /// Ends the current ability window (idempotent): releases the Animator slot and
        /// hides the ring. Called on window end, when the combo leaves the step, and on
        /// OnDisable.
        /// </summary>
        public void Stop()
        {
            if (!busy && active == null) return;
            busy = false;
            active = null;
            hitFired = false;
            time = 0f;
            HideRing();
            if (animatorVisual != null) animatorVisual.ReleaseAbility();
        }

        private void StartAbility(AbilityDefinition def, bool projectile)
        {
            active = def;
            activeProjectile = projectile;
            busy = true;
            hitFired = false;
            time = 0f;

            // Speed pass (m1-abilities): ONE data field retimes the whole ability. The
            // clip's speed multiplier drives the Animator state speed AND divides the
            // window, so hitNorm stays aligned with the visible impact and the lockout
            // shrinks with the animation (mirrors PlayerParams.mantleClipSpeed /
            // mantleDuration = clipLength / duration).
            float speed = def.clipSpeedMultiplier > 0.01f ? def.clipSpeedMultiplier : 1f;
            float clipLength = def.clip != null && def.clip.length > 0.01f
                ? def.clip.length
                : Mathf.Max(0.05f, fallbackDuration);
            duration = clipLength / speed;

            if (animatorVisual != null)
                animatorVisual.PlayAbility(projectile ? 1 : 2, crossFadeDuration, speed);

            // H2 (area): show the radius ring for the whole window (grey-box indicator).
            if (!projectile) ShowRing(def);
        }

        private void TickWindow()
        {
            if (active == null) return;
            float t0 = Mathf.Clamp01(active.hitNorm.x) * duration;
            float t1 = Mathf.Clamp01(active.hitNorm.y) * duration;

            if (!hitFired && time >= t0)
            {
                hitFired = true;
                if (activeProjectile) LaunchProjectile(active);
                else FireArea(active);
            }

            if (!activeProjectile && ring != null && time >= t1) HideRing();
        }

        // ------------------------------------------------------------------
        // H1 — projectile
        // ------------------------------------------------------------------

        private void LaunchProjectile(AbilityDefinition def)
        {
            Vector3 facing = FacingNow();

            // Aim-assist ON TOP of the launch facing (m1-abilities): the SAME shared
            // selection the attack and dash use (Code/Combat/AimAssist.cs). If a target
            // sits inside the frontal cone + range, the projectile travels TOWARD it
            // (corrected once at launch — NO homing). No target -> straight along the
            // facing (current behavior). Correcting BEFORE the muzzle keeps the spawn
            // origin on the same line as the flight direction.
            if (AimAssist.TryFindAimDir(transform.position, rb, facing, projectileAimAssist, out Vector3 aim))
                facing = aim;

            Quaternion rot = Quaternion.LookRotation(facing, Vector3.up);
            Vector3 origin = transform.position + rot * def.muzzleOffset;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile_H1";
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col); // transform-driven: no physics collider
            float d = Mathf.Max(0.1f, def.projectileRadius * 2f);
            go.transform.localScale = new Vector3(d, d, d);
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = GreyBoxFx.ProjectileMaterial();
            go.transform.position = origin;

            var projectile = go.AddComponent<Projectile>();
            projectile.Launch(facing, def.projectileSpeed, def.projectileLifetime,
                def.projectileRadius, def.dano, knockback, hitstopMs, rb, combat);
        }

        // ------------------------------------------------------------------
        // H2 — one-shot area
        // ------------------------------------------------------------------

        private void FireArea(AbilityDefinition def)
        {
            float radius = Mathf.Max(0.1f, def.areaRadius);
            Vector3 facing = FacingNow();
            Collider[] cols = Physics.OverlapSphere(
                transform.position, radius, Physics.AllLayers, QueryTriggerInteraction.Ignore);

            var hit = new HashSet<HitReceiver>();
            bool connected = false;
            for (int i = 0; i < cols.Length; i++)
            {
                var col = cols[i];
                if (col == null) continue;
                if (col.attachedRigidbody == rb) continue; // own capsule / Face trigger

                var receiver = col.GetComponentInParent<HitReceiver>();
                if (receiver == null) continue;
                if (!hit.Add(receiver)) continue; // one hit per target (single pass)

                Vector3 to = receiver.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude < 0.0001f) to = facing;
                to.Normalize();

                receiver.TakeHit(def.dano, to * knockback, 0f);
                connected = true;
            }

            GreyBoxFx.SpawnBurst(transform.position + Vector3.up * 0.1f, radius);
            if (connected && combat != null) combat.RequestHitstop(hitstopMs);
        }

        // ------------------------------------------------------------------
        // H2 ring (grey-box radius indicator)
        // ------------------------------------------------------------------

        private void ShowRing(AbilityDefinition def)
        {
            if (ring == null) ring = GreyBoxFx.CreateRing(transform, def.areaRadius);
            GreyBoxFx.ApplyRingSize(ring.transform, def.areaRadius);
            ring.SetActive(true);
        }

        private void HideRing()
        {
            if (ring != null) ring.SetActive(false);
        }

        private Vector3 FacingNow()
        {
            Vector3 facing = visual != null ? visual.Facing : transform.forward;
            facing.y = 0f;
            return facing.sqrMagnitude > 0.0001f ? facing.normalized : Vector3.forward;
        }
    }
}
