using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Owns the player's fighting stance (<see cref="Stance.Mantis"/> / <see cref="Stance.Tarkatana"/>)
    /// and the guard state that depends on it.
    ///
    /// WHY this component exists: the design says Mantis defends and Tarkatana attacks. Until the
    /// real animations land, the stance has to be *felt* anyway, so this slice implements the two
    /// rules that need no clips — blocking slows the player down and cuts incoming damage — and
    /// tints the character so the state is visible. The tint is a DEBUG cue, not the final VFX.
    ///
    /// GUARD METER: blocking is a RESOURCE, not a free stance. Blocked damage drains the guard;
    /// when it empties the guard BREAKS for a short window (no block, full damage), then refills
    /// partially. There is no HUD: the character tint itself is the readout (block colour ramps
    /// to <see cref="guardWarnTint"/> as guard drains). Regeneration only starts after a delay
    /// without blocked hits, so the guard recovers OUT OF PRESSURE.
    ///
    /// WHY it owns its own <see cref="PlayerInput"/>: the generated wrapper is instantiated
    /// separately by PlayerMotor and PlayerCombat, so this follows the existing pattern instead of
    /// refactoring the input seam in the same change. Consolidating the three instances into one
    /// shared service is tracked as debt, not solved here.
    ///
    /// Mantis = can block. Tarkatana = cannot block (its defensive verb is the grab/disarm).
    ///
    /// WHY the feature gates exist: the v1 player (demo v0.2.0) must stay playable and unchanged
    /// while the current player keeps evolving. Instead of forking the player code, the v1 prefab
    /// simply has NO <see cref="PlayerStance"/> component, and every seam that reads it is
    /// null-safe. CONTRACT: any new mechanic that would change how v1 feels must be null-safe or
    /// live behind a gate here. Breaking that contract silently changes v1.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerStance : MonoBehaviour
    {
        public enum Stance
        {
            Mantis = 0,
            Tarkatana = 1
        }

        [Header("State")]
        [Tooltip("Stance the player starts the scene in.")]
        [SerializeField] private Stance initialStance = Stance.Mantis;

        [Header("Guard")]
        [Tooltip("Move speed multiplier while blocking. The design calls for a slow strafe (~35% of run speed).")]
        [SerializeField, Range(0f, 1f)] private float blockSpeedMultiplier = 0.35f;

        [Tooltip("Damage multiplier while blocking. 0.2 = an 80% reduction. The guard meter below decides how long the guard lasts; this is the per-hit reduction while it holds.")]
        [SerializeField, Range(0f, 1f)] private float blockDamageMultiplier = 0.2f;

        [Header("Guard meter")]
        [Tooltip("Maximum guard points. Reaching 0 BREAKS the guard for guardBreakDuration.")]
        [SerializeField] private float guardMax = 100f;

        [Tooltip("Guard points refilled per second after the guard has not been hit for guardRegenDelay seconds.")]
        [SerializeField] private float guardRegenPerSecond = 35f;

        [Tooltip("Seconds WITHOUT blocked damage before guard starts regenerating.")]
        [SerializeField] private float guardRegenDelay = 0.8f;

        [Tooltip("Guard points spent per point of BLOCKED damage (2 = a 50 damage hit eats 100 guard).")]
        [SerializeField] private float guardDamageRatio = 2f;

        [Tooltip("Seconds the guard stays broken: blocking is impossible and every hit lands at full damage.")]
        [SerializeField] private float guardBreakDuration = 0.9f;

        [Tooltip("Guard points restored when the break ends, so it cannot break again on the next hit.")]
        [SerializeField] private float guardBrokenRefill = 30f;

        [Tooltip("Warning colour. The block tint lerps toward this as guard drains, and it is the fixed tint while broken.")]
        [SerializeField] private Color guardWarnTint = new Color(1f, 0.12f, 0.08f, 1f);

        [Header("Debug tint (placeholder for real VFX)")]
        [Tooltip("Tint applied while in Mantis.")]
        [SerializeField] private Color mantisTint = new Color(0.62f, 0.84f, 1f, 1f);

        [Tooltip("Tint applied while in Tarkatana.")]
        [SerializeField] private Color tarkatanaTint = new Color(1f, 0.55f, 0.38f, 1f);

        [Tooltip("Tint applied while blocking (takes priority over the stance tint).")]
        [SerializeField] private Color blockTint = new Color(0.85f, 0.98f, 1f, 1f);

        [Header("Feature gates")]
        [Tooltip("When off, StanceSwitch is ignored and the player keeps the initial stance. This is the explicit gate that keeps the v1 player behaviour intact.")]
        [SerializeField] private bool allowStanceSwitch = true;

        [Tooltip("When off, the Block action is ignored entirely (no slow strafe, no damage reduction).")]
        [SerializeField] private bool allowBlock = true;

        [Header("Dash per stance")]
        [Tooltip("Dash distance multiplier while in Tarkatana. The lunge is the offensive verb: it covers more ground than the Mantis parry-dash.")]
        [SerializeField, Min(1f)] private float tarkatanaDashDistanceMultiplier = 1.35f;

        [Tooltip("When off, the dash keeps its iframes but they do NOT consume incoming hits. Only turn this off to compare against the v1 player.")]
        [SerializeField] private bool allowDashParry = true;

        [Tooltip("Seconds the Mantis parry stays open. The player plants in place for exactly this long. Shorter = tighter timing, harder parry.")]
        [SerializeField, Min(0.02f)] private float parryWindowSeconds = 0.15f;

        [Header("Parry feedback")]
        [Tooltip("Tint used for the short flash when a dash parries a hit.")]
        [SerializeField] private Color parryTint = Color.white;

        [Tooltip("Seconds the parry flash lasts.")]
        [SerializeField, Min(0.01f)] private float parryFlashSeconds = 0.18f;

        private const float TintBlend = 0.65f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private PlayerInput input;
        private Renderer[] renderers;
        private Color[] baseColors;
        private MaterialPropertyBlock tint;
        private Stance stance;
        private bool blocking;
        private float parryFlashUntil;

        // Guard meter state (runtime only: never serialized, rebuilt in Awake on load).
        private float guard;
        private float lastGuardHitTime;
        private bool guardBroken;
        private float guardBreakTimer;

        /// <summary>Current stance (read-only seam).</summary>
        public Stance CurrentStance => stance;

        /// <summary>True while the player is in Tarkatana (read-only seam).</summary>
        public bool IsTarkatana => stance == Stance.Tarkatana;

        /// <summary>
        /// True while the guard is up. FALSE while <see cref="IsGuardBroken"/>: a broken guard
        /// takes full damage and loses the slow-strafe, even if Block is still held.
        /// </summary>
        public bool IsBlocking => blocking && !guardBroken;

        /// <summary>True while the guard is broken: no blocking at all for a short window.</summary>
        public bool IsGuardBroken => guardBroken;

        /// <summary>Guard remaining, normalized 0..1 (read-only seam for debug/tint).</summary>
        public float GuardNormalized => guardMax > 0f ? Mathf.Clamp01(guard / guardMax) : 0f;

        /// <summary>
        /// Spends guard for a hit that was BLOCKED. The caller invokes this before reducing the
        /// damage, so the hit that empties the meter is still blocked; every hit after the break
        /// lands in full because <see cref="IsBlocking"/> is false. No-op while already broken.
        /// </summary>
        /// <param name="blockedDamage">Incoming damage of the hit entering the guard (pre-reduction).</param>
        public void ConsumeGuard(float blockedDamage)
        {
            if (guardBroken) return;

            float cost = Mathf.Max(0f, blockedDamage) * Mathf.Max(0f, guardDamageRatio);
            guard = Mathf.Max(0f, guard - cost);
            lastGuardHitTime = Time.time;

            if (guard <= 0f)
            {
                guardBroken = true;
                guardBreakTimer = Mathf.Max(0f, guardBreakDuration);
                ApplyTint();
                Debug.Log($"[{nameof(PlayerStance)}] GUARD BREAK", this);
                return;
            }

            ApplyTint();
        }

        /// <summary>Move speed multiplier the motor must apply (1 when not blocking).</summary>
        public float SpeedMultiplier => blocking ? blockSpeedMultiplier : 1f;

        /// <summary>Incoming damage multiplier the hurt path must apply (1 when not blocking).</summary>
        public float DamageMultiplier => blocking ? blockDamageMultiplier : 1f;

        /// <summary>Dash distance multiplier the dash must apply (1 in Mantis, lunge in Tarkatana).</summary>
        public float DashDistanceMultiplier =>
            stance == Stance.Tarkatana ? tarkatanaDashDistanceMultiplier : 1f;

        /// <summary>
        /// True when the dash may consume an incoming hit as a parry. Mantis only: Tarkatana's
        /// dash is the offensive lunge, so it has no defensive frames.
        /// </summary>
        public bool AllowsDashParry => allowDashParry && stance == Stance.Mantis;

        /// <summary>Seconds the Mantis parry stays open (the player stays planted for exactly this long).</summary>
        public float ParryWindow => parryWindowSeconds;

        /// <summary>
        /// Called by the hurt path when a dash consumed a hit. Feedback only: the damage was
        /// already negated by the caller. The counter-attack reward is not implemented yet.
        /// </summary>
        public void NotifyParry()
        {
            parryFlashUntil = Time.unscaledTime + parryFlashSeconds;
            ApplyTint();
            Debug.Log($"[{nameof(PlayerStance)}] PARRY", this);
        }

        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            baseColors = new Color[renderers.Length];
            tint = new MaterialPropertyBlock();

            // Snapshot the authored colour so the debug tint blends into it instead of
            // flattening the character into a single flat colour.
            for (int i = 0; i < renderers.Length; i++)
            {
                Material material = renderers[i] != null ? renderers[i].sharedMaterial : null;
                baseColors[i] = ReadBaseColor(material);
            }

            stance = initialStance;
            guard = Mathf.Max(0f, guardMax);
            lastGuardHitTime = Time.time - Mathf.Max(0f, guardRegenDelay);
            ApplyTint();
        }

        private static Color ReadBaseColor(Material material)
        {
            if (material == null) return Color.white;
            if (material.HasProperty(BaseColorId)) return material.GetColor(BaseColorId);
            if (material.HasProperty(ColorId)) return material.GetColor(ColorId);
            return Color.white;
        }

        private void OnEnable()
        {
            if (input == null) input = new PlayerInput();
            input.Player.Enable();
        }

        private void OnDisable()
        {
            if (input != null) input.Player.Disable();
            blocking = false;
        }

        private void Update()
        {
            UpdateParryFlash();
            UpdateGuard();
            if (input == null) return;

            if (allowStanceSwitch && input.Player.StanceSwitch.WasPressedThisFrame())
            {
                ToggleStance();
            }

            // Tarkatana has no guard: Block there is the ROAR (PlayerRoar), not a block, so this
            // path simply keeps the guard down. Its other defensive verb is the grab/disarm.
            bool nextBlocking = allowBlock &&
                                stance == Stance.Mantis &&
                                input.Player.Block.IsPressed();
            if (nextBlocking == blocking) return;

            blocking = nextBlocking;
            ApplyTint();
            Debug.Log(
                $"[{nameof(PlayerStance)}] Block {(blocking ? "UP" : "down")} " +
                $"(speed x{SpeedMultiplier:0.##}, damage x{DamageMultiplier:0.##})",
                this);
        }

        private void UpdateParryFlash()
        {
            if (parryFlashUntil <= 0f) return;
            if (Time.unscaledTime < parryFlashUntil) return;

            parryFlashUntil = 0f;
            ApplyTint();
        }

        /// <summary>
        /// Runs the guard meter: the break countdown, then the delayed regeneration. The tint
        /// is refreshed while regenerating because the tint IS the meter (no HUD).
        /// </summary>
        private void UpdateGuard()
        {
            if (guardBroken)
            {
                guardBreakTimer -= Time.deltaTime;
                if (guardBreakTimer > 0f) return;

                guardBroken = false;
                guard = Mathf.Clamp(guardBrokenRefill, 0f, Mathf.Max(0f, guardMax));
                ApplyTint();
                Debug.Log(
                    $"[{nameof(PlayerStance)}] Guard recovered ({guard:0.#}/{guardMax:0.#})",
                    this);
                return;
            }

            if (guard >= guardMax) return;
            if (Time.time - lastGuardHitTime < guardRegenDelay) return;

            guard = Mathf.Min(guardMax, guard + Mathf.Max(0f, guardRegenPerSecond) * Time.deltaTime);

            // Only repaint while the meter is visible (guard up or broken): the block tint
            // ramps back from red as guard returns.
            if (IsBlocking) ApplyTint();
        }

        private void ToggleStance()
        {
            stance = stance == Stance.Mantis ? Stance.Tarkatana : Stance.Mantis;
            blocking = false;
            ApplyTint();
            Debug.Log($"[{nameof(PlayerStance)}] Stance -> {stance}", this);
        }

        /// <summary>
        /// Writes the debug colour through a <see cref="MaterialPropertyBlock"/> so no material
        /// asset is ever modified: the tint is per-renderer and disappears with Play Mode.
        /// Both URP (<c>_BaseColor</c>) and built-in (<c>_Color</c>) names are written; a property
        /// the shader does not have is simply ignored.
        /// </summary>
        private void ApplyTint()
        {
            if (renderers == null || tint == null) return;

            Color target;
            if (parryFlashUntil > 0f)
            {
                target = parryTint;
            }
            else if (guardBroken)
            {
                // Broken: fixed warning colour, no meter left to show.
                target = guardWarnTint;
            }
            else if (IsBlocking)
            {
                // The guard meter IS the tint: block colour -> warning red as guard drains.
                target = Color.Lerp(blockTint, guardWarnTint, 1f - GuardNormalized);
            }
            else
            {
                target = stance == Stance.Mantis ? mantisTint : tarkatanaTint;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null) continue;

                Color color = Color.Lerp(baseColors[i], target, TintBlend);

                renderer.GetPropertyBlock(tint);
                tint.SetColor(BaseColorId, color);
                tint.SetColor(ColorId, color);
                renderer.SetPropertyBlock(tint);
            }
        }
    }
}
