using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// The Executioner: verdugo de dos fases cuyo combate es una alternancia de INICIATIVA.
    ///
    /// El host (<see cref="BossController"/>) sigue dueño de la salud, las fases, el leash, la
    /// cámara y la muerte. Este cerebro sólo decide el TURNO:
    ///
    ///   PlayerWindow → (vence la ventana) → Guard | Windup | CloseIn
    ///   Guard        → PlayerWindow
    ///   Windup       → Strike → Recover → PlayerWindow
    ///   CloseIn      → Windup | PlayerWindow (timeout)
    ///   Punish       → PlayerWindow (lo abre una exposición; S2)
    ///
    /// Regla que lo hace iniciativa y no distancia: la ventana del jugador es la ÚNICA entrada
    /// a Guard/Windup/CloseIn. La distancia sólo pesa en la selección y en si el ataque elegido
    /// puede empezar; si no llega, el boss camina en vez de atacar al aire.
    ///
    /// Ningún número de balance vive acá: todo sale de <see cref="BossDefinition"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ExecutionerController : BossController
    {
        private enum Beat { PlayerWindow, Guard, Windup, Strike, Recover, Punish, CloseIn }

        private Beat beat = Beat.PlayerWindow;
        private float beatTimer;
        private float windowTimer;
        private float sinceLastWindup = float.MaxValue;
        private BossAttackDefinition chosen;
        private string lastAttackId = string.Empty;
        private int streak;
        private bool guardOn;
        private bool strikeApplied;

        /// <summary>Nombre del beat actual, para el overlay de debug y la luz de estado.</summary>
        public string CurrentBeat => beat.ToString();

        /// <summary>Id del ataque en curso (vacío fuera de Windup/Strike/Recover).</summary>
        public string CurrentAttackId => chosen != null ? chosen.id : string.Empty;

        // --- ciclo de vida ---------------------------------------------------

        protected override void Start()
        {
            base.Start();
            EnterPlayerWindow(true);
        }

        protected override void TickDecision(float dt, float mult)
        {
            sinceLastWindup += dt;

            switch (beat)
            {
                case Beat.PlayerWindow:
                    TickPlayerWindow(dt, mult);
                    break;
                case Beat.Guard:
                    TickGuard(dt);
                    break;
                case Beat.Windup:
                    TickWindup(dt, mult);
                    break;
                case Beat.Strike:
                    TickStrike(dt, mult);
                    break;
                case Beat.Recover:
                    TickRecover(dt, mult);
                    break;
                case Beat.Punish:
                    TickPunish(dt);
                    break;
                case Beat.CloseIn:
                    TickCloseIn(dt, mult);
                    break;
            }

            AssertGuardInvariant();
        }

        /// <summary>
        /// El leash no se come el frame en los beats comprometidos: interrumpir un ataque ya
        /// lanzado por "me alejé del centro" rompería la lectura del tell.
        /// </summary>
        protected override bool TickLeash(float dt, float mult)
        {
            if (beat == Beat.Windup || beat == Beat.Strike || beat == Beat.Punish) return false;
            return base.TickLeash(dt, mult);
        }

        protected override void OnPhaseRoarFinished()
        {
            EnterPlayerWindow(true);
        }

        protected override void OnResetForRespawn()
        {
            SetGuard(false);
            chosen = null;
            lastAttackId = string.Empty;
            streak = 0;
            sinceLastWindup = float.MaxValue;
            EnterPlayerWindow(true);
        }

        /// <summary>
        /// Reacción al daño por tabla de beat. En los beats comprometidos sólo queda el flash
        /// del HitReceiver: el daño nunca cambia de estado (INIT-4).
        /// </summary>
        protected override void PlayDamageReaction()
        {
            if (beat == Beat.Windup || beat == Beat.Strike) return;

            CharacterVisual visual = Visual;
            if (visual == null) return;

            if (beat == Beat.Guard) visual.PlayGuardHit();
            else visual.PlayHit();
        }

        // --- beats -----------------------------------------------------------

        private void EnterPlayerWindow(bool full)
        {
            beat = Beat.PlayerWindow;
            SetGuard(false);
            chosen = null;
            windowTimer = full ? CurrentPhaseProfile().playerWindow : 0f;
            beatTimer = 0f;
        }

        private void TickPlayerWindow(float dt, float mult)
        {
            FacePlayer(dt);
            SteerTowardsPlayer(mult);

            windowTimer -= dt;
            if (windowTimer > 0f) return;

            // La ventana venció: recién acá el boss toma la iniciativa.
            if (sinceLastWindup < Definition.attackCooldown / mult)
            {
                windowTimer = 0.25f;
                return;
            }

            if (Random.value < CurrentPhaseProfile().guardChance)
            {
                EnterGuard();
                return;
            }

            BossAttackDefinition attack = PickAttack();
            if (attack == null)
            {
                EnterCloseIn();
                return;
            }

            if (DistanceToPlayer() <= attack.range) EnterWindup(attack);
            else EnterCloseIn();
        }

        private void EnterGuard()
        {
            beat = Beat.Guard;
            SetGuard(true);
            beatTimer = Definition.guardDuration;
        }

        private void TickGuard(float dt)
        {
            FacePlayer(dt);
            StopHorizontal();

            beatTimer -= dt;
            if (beatTimer > 0f) return;

            // Guardia cumplida: vuelve a ceder la iniciativa.
            EnterPlayerWindow(false);
        }

        private void EnterWindup(BossAttackDefinition attack)
        {
            beat = Beat.Windup;
            chosen = attack;
            strikeApplied = false;
            beatTimer = Mathf.Max(Definition.minTell, attack.tell) / PhaseSpeedMultiplier();
            sinceLastWindup = 0f;

            TrackStreak(attack.id);
            PlayClip(attack, 0);
            FacePlayer(Time.fixedDeltaTime);
        }

        private void TickWindup(float dt, float mult)
        {
            FacePlayer(dt);
            StopHorizontal();

            beatTimer -= dt;
            if (beatTimer > 0f) return;

            beat = Beat.Strike;
            beatTimer = 0.12f;
            PlayClip(chosen, 1);
        }

        private void TickStrike(float dt, float mult)
        {
            if (!strikeApplied)
            {
                strikeApplied = true;
                ResolveStrike();
            }

            beatTimer -= dt;
            if (beatTimer > 0f) return;

            beat = Beat.Recover;
            beatTimer = Mathf.Max(0.01f, chosen != null ? chosen.punish : 1f) / mult;
            if (chosen != null && !string.IsNullOrEmpty(chosen.punishClip)) PlayClipName(chosen.punishClip);
        }

        private void TickRecover(float dt, float mult)
        {
            FacePlayer(dt);
            StopHorizontal();

            beatTimer -= dt;
            if (beatTimer > 0f) return;

            EnterPlayerWindow(false);
        }

        private void TickPunish(float dt)
        {
            StopHorizontal();

            beatTimer -= dt;
            if (beatTimer > 0f) return;

            EnterPlayerWindow(false);
        }

        private void EnterCloseIn()
        {
            beat = Beat.CloseIn;
            beatTimer = Definition.closeInTimeout;
        }

        private void TickCloseIn(float dt, float mult)
        {
            FacePlayer(dt);
            SteerTowardsPlayer(mult);

            beatTimer -= dt;

            BossAttackDefinition attack = chosen != null ? chosen : PickAttack();
            if (attack != null && DistanceToPlayer() <= attack.range)
            {
                EnterWindup(attack);
                return;
            }

            if (beatTimer <= 0f) EnterPlayerWindow(false);
        }

        // --- selección -------------------------------------------------------

        /// <summary>
        /// Peso = weight × (en rango ? 1 : (cierra distancia ? 1 : 0.25)). El pool sale de la
        /// fase y la racha descarta al ataque que ya se repitió <c>maxStreak</c> veces.
        /// </summary>
        private BossAttackDefinition PickAttack()
        {
            BossAttackDefinition[] attacks = Definition.attacks;
            if (attacks == null || attacks.Length == 0) return null;

            int phase = Phase;
            float distance = DistanceToPlayer();
            BossAttackDefinition best = null;
            float bestScore = 0f;
            float total = 0f;

            for (int i = 0; i < attacks.Length; i++)
            {
                BossAttackDefinition a = attacks[i];
                if (a == null || !InPhase(a, phase)) continue;
                if (a.id == lastAttackId && streak >= Definition.maxStreak) continue;

                float score = a.weight;
                if (distance > a.range && (a.flags & BossAttackFlags.ClosesDistance) == 0) score *= 0.25f;

                total += score;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = a;
                }
            }

            if (best == null || total <= 0f) return best;

            // Ruleta ponderada: el "mejor" no siempre gana, o el boss sería predecible.
            float roll = Random.value * total;
            for (int i = 0; i < attacks.Length; i++)
            {
                BossAttackDefinition a = attacks[i];
                if (a == null || !InPhase(a, phase)) continue;
                if (a.id == lastAttackId && streak >= Definition.maxStreak) continue;

                float score = a.weight;
                if (distance > a.range && (a.flags & BossAttackFlags.ClosesDistance) == 0) score *= 0.25f;

                roll -= score;
                if (roll <= 0f) return a;
            }

            return best;
        }

        private static bool InPhase(BossAttackDefinition attack, int phase)
        {
            if (attack.phases == null || attack.phases.Length == 0) return true;
            for (int i = 0; i < attack.phases.Length; i++)
            {
                if (attack.phases[i] == phase) return true;
            }

            return false;
        }

        private void TrackStreak(string id)
        {
            if (id == lastAttackId) streak++;
            else
            {
                lastAttackId = id;
                streak = 1;
            }
        }

        // --- guardia ---------------------------------------------------------

        /// <summary>
        /// Único escritor de <see cref="HitReceiver.DamageMultiplier"/>. Todo lo que enciende o
        /// apaga la guardia pasa por acá: guardia, Recover, Punish, muerte, reset y fin de rugido.
        /// </summary>
        private void SetGuard(bool on)
        {
            guardOn = on;
            HitReceiver receiver = Receiver;
            if (receiver != null)
            {
                receiver.DamageMultiplier = on ? Definition.guardDamageMultiplier : 1f;
            }

            CharacterVisual visual = Visual;
            if (visual != null && on) visual.PlayGuard();
        }

        /// <summary>
        /// El bug más caro de este change: un boss que nunca más recibe daño entero porque la
        /// guardia quedó encendida fuera de su beat.
        /// </summary>
        private void AssertGuardInvariant()
        {
            if (beat == Beat.Guard || !Application.isEditor) return;

            HitReceiver receiver = Receiver;
            if (receiver == null || guardOn) return;

            if (receiver.DamageMultiplier < 1f)
            {
                Debug.LogError("[Executioner] DamageMultiplier quedó reducido fuera de Guard.", this);
                receiver.DamageMultiplier = 1f;
            }
        }

        // --- golpe -----------------------------------------------------------

        private void ResolveStrike()
        {
            if (chosen == null || chosen.damage <= 0) return;

            PlayerDamage player = Player;
            if (player == null || !player.IsAlive) return;

            float range = chosen.motion != null && chosen.motion.radius > 0f
                ? chosen.motion.radius
                : chosen.range;

            if (DistanceToPlayer() > range) return;

            player.TakeHit(chosen.damage, FlatDirectionTo(player.transform.position));
        }

        // --- helpers ---------------------------------------------------------

        private void PlayClip(BossAttackDefinition attack, int index)
        {
            if (attack == null || attack.clips == null || attack.clips.Length == 0) return;
            PlayClipName(attack.clips[Mathf.Clamp(index, 0, attack.clips.Length - 1)]);
        }

        private void PlayClipName(string state)
        {
            CharacterVisual visual = Visual;
            if (visual != null && !string.IsNullOrEmpty(state)) visual.CrossFadeSafe(state, 0.1f);
        }

        private BossPhaseProfile CurrentPhaseProfile()
        {
            BossPhaseProfile[] profiles = Definition.phaseProfiles;
            if (profiles == null || profiles.Length == 0) return new BossPhaseProfile();

            int index = Mathf.Clamp(Phase - 1, 0, profiles.Length - 1);
            return profiles[index] ?? new BossPhaseProfile();
        }

        private float DistanceToPlayer()
        {
            PlayerDamage player = Player;
            return player != null ? FlatDistanceTo(player.transform.position) : float.MaxValue;
        }

        private void FacePlayer(float dt)
        {
            PlayerDamage player = Player;
            if (player != null) Face(FlatDirectionTo(player.transform.position), dt);
        }

        private void SteerTowardsPlayer(float mult)
        {
            PlayerDamage player = Player;
            if (player == null) return;

            float speed = Definition.moveSpeed * mult;
            if (Visual != null) Visual.SetMoving(true, speed);
            Steer(FlatDirectionTo(player.transform.position), speed);
        }
    }
}
