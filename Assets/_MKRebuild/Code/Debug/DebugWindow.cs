using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace MK.Rebuild.Debugging
{
    /// <summary>
    /// M0 debug window — an ISOLATED dev module (own assembly <c>MK.Rebuild.Debug</c>,
    /// <c>autoReferenced:false</c>). It ADHERES to the game read-only: it never
    /// drives gameplay, it only observes it. The only things it writes are
    /// <see cref="Time.timeScale"/> and the grey dummies it spawns.
    ///
    /// Boot: a manual component on a <c>Debug</c> GameObject in <c>Gym.unity</c>
    /// (NO <c>[RuntimeInitializeOnLoadMethod]</c> — the debug stays in the gym).
    ///
    /// Four topic tabs on F1-F4 — pressing the active tab's key again hides the
    /// overlay; F5-F12 are CONTEXT actions of the active tab:
    ///   F1 MÚSICA  · F5 music · F6 ambience · F7 mute · F8 SFX audition
    ///   F2 ENEMIGOS· F5 spawn · F6 boss · F7 kill all · F8 freeze AI
    ///   F3 PLAYER  · F5 reset tuning · F6 log tuning · F7 states · F8 invuln
    ///              · F9 test damage · F10 kill
    ///   F4 ETC     · F5 dummy · F6 timeScale · F7 clear dummies · F8 teleport
    /// Reserved slots are true no-ops. <c>B</c> stays a global alias for spawn boss.
    /// Input System only — legacy Input is dead, activeInputHandler:1.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebugWindow : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Only the active tab renders; None renders nothing and ignores F5-F12.
        private enum DebugTab { None, Music, Enemies, Player, Etc }

        // timeScale cycle: 0 → 0.25 → 0.5 → 1 → 2. Starts at 1x (index 3).
        // NOTE: timeScale = 0 freezes the Animator + FixedUpdate; this OnGUI overlay
        // keeps rendering because OnGUI is not tied to scaled time. A real PauseManager
        // is deferred debt — this is a direct Time.timeScale write, nothing more.
        private static readonly float[] TimeScales = { 0f, 0.25f, 0.5f, 1f, 2f };
        // F6 (Música) cycles this ring; the ambience clips themselves live in AudioCatalog.asset.
        private static readonly AmbienceState[] AmbienceCycle =
        {
            AmbienceState.Parkour, AmbienceState.Combat, AmbienceState.City,
            AmbienceState.Menu, AmbienceState.None
        };
        // F5 (Música) auditions this ring through MusicStateController.SetDebugState;
        // the controller is the normal owner, so the debug override has no auto-revert.
        private static readonly MusicState[] MusicCycle =
        {
            MusicState.Parkour, MusicState.Combat, MusicState.City,
            MusicState.Menu, MusicState.Boss, MusicState.None
        };
        // F8 (Música) auditions existing cues through AudioDirector.Play2D.
        private static readonly AudioCueId[] SfxAuditionCycle =
        {
            AudioCueId.LightSwing, AudioCueId.HeavyHit, AudioCueId.Land, AudioCueId.Dash
        };
        private const int MaxDummies = 5;          // small count, FIFO recycle past 5
        private const float SpawnAhead = 3f;       // units in front of the player
        private const float CapsuleHalfHeight = 1f;

        [Tooltip("Tab shown when the overlay boots. F1-F4 switch tabs at runtime.")]
        [SerializeField] private DebugTab initialTab = DebugTab.Music;

        [Tooltip("States readout body (MoveState / grounded / facing / speed). F7 on the Player tab toggles.")]
        [SerializeField] private bool showStates = true;

        private DebugTab activeTab;
        private DebugTab pendingTab; // header click, applied next Update (keeps IMGUI passes coherent)
        private int timeScaleIndex = 3; // 1x
        private PlayerMotor motor;
        private AnimatorVisual visual;
        private Rigidbody body;
        private StyleMeter styleMeter;
        private PlayerAbilities abilities;
        private PlayerDamage damage;
        private PlayerCombat combat;
        private Material greyMaterial;
        private readonly List<GameObject> spawned = new List<GameObject>();

        // Música tab state.
        private bool muted;
        private float volumeBeforeMute = 1f;
        private int sfxAuditionIndex = -1;

        // Enemigos tab state.
        private bool aiFrozen;

        // Player tab state.
        private bool playerInvulnerable;

        // Start-of-play tuning snapshot: RESET restores the values the game STARTED with
        // (serialized defaults), not a hardcoded identity block.
        private float defaultAttackSpeedScale = 1f;
        private float defaultAttackReachScale = 1f;
        private float defaultHitstopScale = 1f;
        private float defaultCancelStartScale = 1f;
        private int defaultMaxTargetsOverride;
        private float defaultDirectionFlickWindow = 0.22f;

        // Etc tab state: the spawn transform captured once in Start.
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        private string lastInputPath = "<none>";
        private string lastInputDevice = "<none>";
        private IDisposable anyButtonSubscription;

        private Texture2D background;
        private GUIStyle boxStyle;
        private GUIStyle labelStyle;
        private GUIStyle tabStyle;

        private void Awake()
        {
            ResolveTargets();
            activeTab = initialTab;
        }

        private void Start()
        {
            // F8 (Etc) teleports back here — captured once, before the player moves.
            ResolveTargets();
            if (motor != null)
            {
                initialPosition = motor.transform.position;
                initialRotation = motor.transform.rotation;
            }
            CaptureTuningDefaults();
        }

        private void OnEnable()
        {
            // Observe-only input probe: raw button presses arrive even on a generic
            // <Joystick> that no action binds — that is exactly the device we need to
            // map. This is a readout, never a gameplay input source.
            anyButtonSubscription = InputSystem.onAnyButtonPress.Call(control =>
            {
                if (control == null) return;
                lastInputPath = control.path;
                lastInputDevice = control.device != null ? control.device.displayName : "<unknown>";
            });
            InputSystem.onActionChange += OnActionChange;
        }

        private void OnDisable()
        {
            if (anyButtonSubscription != null)
            {
                anyButtonSubscription.Dispose();
                anyButtonSubscription = null;
            }
            InputSystem.onActionChange -= OnActionChange;
        }

        private void OnActionChange(object obj, InputActionChange change)
        {
            // Stick/axis actions do not fire onAnyButtonPress; this catches them.
            if (change != InputActionChange.ActionPerformed) return;
            var action = obj as InputAction;
            if (action == null) return;
            InputControl control = action.activeControl;
            if (control == null) return;
            lastInputPath = control.path;
            lastInputDevice = control.device != null ? control.device.displayName : "<unknown>";
        }

        private void OnDestroy()
        {
            // The window only ever created its own transient assets — clean them up.
            if (background != null) Destroy(background);
            if (greyMaterial != null) Destroy(greyMaterial);
        }

        private void ResolveTargets()
        {
            if (motor == null) motor = FindFirstObjectByType<PlayerMotor>();
            if (motor == null) return;
            if (visual == null) visual = motor.GetComponent<AnimatorVisual>();
            if (body == null) body = motor.GetComponent<Rigidbody>();
            if (abilities == null) abilities = motor.GetComponent<PlayerAbilities>();
            if (damage == null) damage = motor.GetComponent<PlayerDamage>();
            if (combat == null) combat = motor.GetComponent<PlayerCombat>();
        }

        private void Update()
        {
            // Header clicks are queued so the change lands between IMGUI passes.
            if (pendingTab != DebugTab.None)
            {
                ToggleTab(pendingTab);
                pendingTab = DebugTab.None;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null) return; // no device attached — nothing to toggle

            // Tabs (F1-F4). Re-pressing the active tab hides the overlay.
            if (keyboard.f1Key.wasPressedThisFrame) { ToggleTab(DebugTab.Music); return; }
            if (keyboard.f2Key.wasPressedThisFrame) { ToggleTab(DebugTab.Enemies); return; }
            if (keyboard.f3Key.wasPressedThisFrame) { ToggleTab(DebugTab.Player); return; }
            if (keyboard.f4Key.wasPressedThisFrame) { ToggleTab(DebugTab.Etc); return; }

            // Legacy alias — kept global so the old muscle memory still works.
            if (keyboard.bKey.wasPressedThisFrame) SpawnBoss();

            // No tab active: render nothing and ignore F5-F12.
            if (activeTab == DebugTab.None) return;

            if (keyboard.f5Key.wasPressedThisFrame) { ExecuteSlot(5); return; }
            if (keyboard.f6Key.wasPressedThisFrame) { ExecuteSlot(6); return; }
            if (keyboard.f7Key.wasPressedThisFrame) { ExecuteSlot(7); return; }
            if (keyboard.f8Key.wasPressedThisFrame) { ExecuteSlot(8); return; }
            if (keyboard.f9Key.wasPressedThisFrame) { ExecuteSlot(9); return; }
            if (keyboard.f10Key.wasPressedThisFrame) { ExecuteSlot(10); return; }
            // F11/F12 are reserved: intentionally no-op.
        }

        private void ToggleTab(DebugTab tab)
        {
            activeTab = activeTab == tab ? DebugTab.None : tab;
        }

        /// <summary>
        /// Dispatches one context slot (F5-F12) to the active tab. The caller already
        /// guarantees <see cref="activeTab"/> != None, so reserved slots fall through
        /// every switch and do nothing.
        /// </summary>
        private void ExecuteSlot(int slot)
        {
            switch (activeTab)
            {
                case DebugTab.Music: ExecuteMusicSlot(slot); break;
                case DebugTab.Enemies: ExecuteEnemiesSlot(slot); break;
                case DebugTab.Player: ExecutePlayerSlot(slot); break;
                case DebugTab.Etc: ExecuteEtcSlot(slot); break;
            }
        }

        private void ExecuteMusicSlot(int slot)
        {
            switch (slot)
            {
                case 5: CycleMusic(); break;
                case 6: CycleAmbience(); break;
                case 7: ToggleMute(); break;
                case 8: AuditionSfx(); break;
                // 9-12 reserved.
            }
        }

        private void ExecuteEnemiesSlot(int slot)
        {
            switch (slot)
            {
                case 5: SpawnEnemy(); break;
                case 6: SpawnBoss(); break;
                case 7: KillAllEnemies(); break;
                case 8: ToggleAiFreeze(); break;
                // 9-12 reserved.
            }
        }

        private void ExecutePlayerSlot(int slot)
        {
            switch (slot)
            {
                case 5: ResetCombatTuning(); break;
                case 6: LogCombatTuning(); break;
                case 7: showStates = !showStates; break;
                case 8: TogglePlayerInvulnerability(); break;
                case 9: ApplyTestDamage(); break;
                case 10: KillPlayer(); break;
                // 11-12 reserved.
            }
        }

        private void ExecuteEtcSlot(int slot)
        {
            switch (slot)
            {
                case 5: SpawnDummy(); break;
                case 6: CycleTimeScale(); break;
                case 7: ClearDummies(); break;
                case 8: TeleportPlayerToStart(); break;
                // 9-12 reserved.
            }
        }

        // --- Actions ---------------------------------------------------------

        /// <summary>
        /// Spawns one grey dummy a few units in front of the player, dropped on the
        /// floor. Keeps at most <see cref="MaxDummies"/> alive (oldest is recycled).
        /// </summary>
        private void SpawnDummy()
        {
            ResolveTargets();
            if (motor == null) return;

            if (spawned.Count >= MaxDummies)
            {
                var oldest = spawned[0];
                spawned.RemoveAt(0);
                if (oldest != null) Destroy(oldest);
            }

            Vector3 facing = visual != null ? visual.Facing : motor.transform.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f) facing = Vector3.forward;
            facing.Normalize();

            Vector3 origin = motor.transform.position;
            Vector3 target = origin + facing * SpawnAhead;

            // Drop the dummy onto the floor below the target point.
            Vector3 spawnPos = new Vector3(target.x, origin.y, target.z);
            if (Physics.Raycast(target + Vector3.up * 5f, Vector3.down, out RaycastHit hit,
                    20f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                spawnPos = hit.point + Vector3.up * CapsuleHalfHeight;
            }

            var dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummy.name = "DebugDummy_" + spawned.Count;
            dummy.transform.position = spawnPos;

            // Spawned targets are hittable too (M1 combat): the grey dummy needs
            // the same hurtbox as the scene Dummy, or attacks whiff on it.
            dummy.AddComponent<HitReceiver>();

            var renderer = dummy.GetComponent<Renderer>();
            if (renderer != null)
            {
                if (greyMaterial == null) greyMaterial = CreateGreyMaterial();
                if (greyMaterial != null) renderer.sharedMaterial = greyMaterial;
            }

            spawned.Add(dummy);
        }

        /// <summary>Destroys every dummy this window spawned.</summary>
        private void ClearDummies()
        {
            for (int i = 0; i < spawned.Count; i++)
            {
                if (spawned[i] != null) Destroy(spawned[i]);
            }
            spawned.Clear();
        }

        /// <summary>
        /// Spawns one enemy through the first scene <see cref="EnemySpawner"/> (M3).
        /// No spawner = no-op; the debug window never fabricates encounters itself.
        /// </summary>
        private void SpawnEnemy()
        {
            var spawner = FindFirstObjectByType<EnemySpawner>();
            if (spawner != null) spawner.SpawnOne();
        }

        /// <summary>
        /// Spawns the boss through the first scene <see cref="BossSpawner"/> (M3.4).
        /// No spawner = no-op; the debug window never fabricates encounters itself.
        /// </summary>
        private void SpawnBoss()
        {
            var spawner = FindFirstObjectByType<BossSpawner>();
            if (spawner != null) spawner.SpawnBoss();
        }

        /// <summary>
        /// Kills every <see cref="EnemyController"/> in the scene through its hurtbox.
        /// <see cref="BossController"/> is a separate component, so the boss survives.
        /// </summary>
        private void KillAllEnemies()
        {
            var enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                var enemy = enemies[i];
                if (enemy == null) continue;
                var receiver = enemy.GetComponent<HitReceiver>();
                if (receiver == null || !receiver.IsAlive) continue;
                receiver.TakeHit(receiver.HP + 1, Vector3.zero);
            }
        }

        /// <summary>
        /// Toggles <see cref="EnemyController.Frozen"/> on every enemy. Uses the AI seam
        /// instead of disabling the component so damaged/died subscriptions survive the
        /// unfreeze. New spawns made while frozen start unfrozen — freeze again after.
        /// </summary>
        private void ToggleAiFreeze()
        {
            var enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            aiFrozen = !aiFrozen;

            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null) enemies[i].Frozen = aiFrozen;
            }
        }

        /// <summary>Teleports the player back to the transform captured in Start.</summary>
        private void TeleportPlayerToStart()
        {
            ResolveTargets();
            if (motor == null) return;
            motor.transform.SetPositionAndRotation(initialPosition, initialRotation);
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }

        private static Material CreateGreyMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null; // fall back to the primitive default material
            return new Material(shader) { color = new Color(0.55f, 0.55f, 0.55f, 1f) };
        }

        /// <summary>Cycles Time.timeScale through 0 → 0.25 → 0.5 → 1 → 2.</summary>
        private void CycleTimeScale()
        {
            timeScaleIndex = (timeScaleIndex + 1) % TimeScales.Length;
            Time.timeScale = TimeScales[timeScaleIndex];
        }

        /// <summary>Cycles ambience Parkour → Combat → City → Menu → None → Parkour.</summary>
        private void CycleAmbience()
        {
            int index = Array.IndexOf(AmbienceCycle, AudioDirector.CurrentAmbience);
            int next = index < 0 ? 0 : (index + 1) % AmbienceCycle.Length;
            AudioDirector.SetAmbience(AmbienceCycle[next]);
        }

        /// <summary>
        /// Auditions music Parkour → Combat → City → Menu → Boss → None → Parkour.
        /// Prefers the scene owner; falls back to AudioDirector when the controller
        /// is not in the scene.
        /// </summary>
        private void CycleMusic()
        {
            int index = Array.IndexOf(MusicCycle, AudioDirector.CurrentMusic);
            int next = index < 0 ? 0 : (index + 1) % MusicCycle.Length;
            MusicState state = MusicCycle[next];

            var controller = FindFirstObjectByType<MusicStateController>();
            if (controller != null) controller.SetDebugState(state);
            else AudioDirector.SetMusic(state);
        }

        /// <summary>
        /// Mutes/unmutes the master bus. Mute remembers the volume it replaced so the
        /// unmute restores it; 0 master volume always counts as muted.
        /// </summary>
        private void ToggleMute()
        {
            muted = !muted;
            if (muted)
            {
                if (AudioDirector.MasterVolume > 0f) volumeBeforeMute = AudioDirector.MasterVolume;
                AudioDirector.MasterVolume = 0f;
            }
            else
            {
                AudioDirector.MasterVolume = volumeBeforeMute > 0f ? volumeBeforeMute : 1f;
            }
        }

        /// <summary>Auditions the next existing <see cref="AudioCueId"/> through Play2D.</summary>
        private void AuditionSfx()
        {
            sfxAuditionIndex = (sfxAuditionIndex + 1) % SfxAuditionCycle.Length;
            AudioDirector.Play2D(SfxAuditionCycle[sfxAuditionIndex]);
        }

        /// <summary>Returns the live tuning block to the values the session STARTED with.</summary>
        private void ResetCombatTuning()
        {
            if (!TryGetPlayerParams(out PlayerParams p)) return;
            p.attackSpeedScale = defaultAttackSpeedScale;
            p.attackReachScale = defaultAttackReachScale;
            p.hitstopScale = defaultHitstopScale;
            p.cancelStartScale = defaultCancelStartScale;
            p.maxTargetsOverride = defaultMaxTargetsOverride;
            p.directionFlickWindow = defaultDirectionFlickWindow;
        }

        /// <summary>
        /// Captures the serialized tuning once, at Start, so RESET restores what Unity
        /// launched with (e.g. attackSpeedScale 1.5) instead of an identity block.
        /// </summary>
        private void CaptureTuningDefaults()
        {
            if (!TryGetPlayerParams(out PlayerParams p)) return;
            defaultAttackSpeedScale = p.attackSpeedScale;
            defaultAttackReachScale = p.attackReachScale;
            defaultHitstopScale = p.hitstopScale;
            defaultCancelStartScale = p.cancelStartScale;
            defaultMaxTargetsOverride = p.maxTargetsOverride;
            defaultDirectionFlickWindow = p.directionFlickWindow;
        }

        /// <summary>Prints one copy-paste line with all five live tuning values.</summary>
        private void LogCombatTuning()
        {
            if (!TryGetPlayerParams(out PlayerParams p))
            {
                Debug.Log("[COMBAT TUNING] <no player>");
                return;
            }
            Debug.Log(
                $"[COMBAT TUNING] attackSpeedScale={p.attackSpeedScale:0.00} " +
                $"attackReachScale={p.attackReachScale:0.00} hitstopScale={p.hitstopScale:0.00} " +
                $"cancelStartScale={p.cancelStartScale:0.00} maxTargetsOverride={p.maxTargetsOverride} " +
                $"directionFlickWindow={p.directionFlickWindow:0.000}");
        }

        /// <summary>Toggles debug invulnerability (a 1 h window vs. a 0 s window).</summary>
        private void TogglePlayerInvulnerability()
        {
            ResolveTargets();
            if (damage == null) return;
            playerInvulnerable = !playerInvulnerable;
            damage.GrantInvulnerability(playerInvulnerable ? 3600f : 0f);
        }

        /// <summary>Player tab F9 — delegate to the scene's damage test driver, if any.</summary>
        private void ApplyTestDamage()
        {
            var driver = FindFirstObjectByType<PlayerDamageTestDriver>();
            if (driver != null) driver.ApplyTestDamage();
        }

        /// <summary>Player tab F10 — delegate to the scene's damage test driver, if any.</summary>
        private void KillPlayer()
        {
            var driver = FindFirstObjectByType<PlayerDamageTestDriver>();
            if (driver != null) driver.KillPlayer();
        }

        private bool TryGetPlayerParams(out PlayerParams p)
        {
            // Same lookup the states readout uses: the params asset ON the player's motor.
            if (motor == null) motor = FindFirstObjectByType<PlayerMotor>();
            p = motor != null ? motor.Params : null;
            return p != null;
        }

        // --- IMGUI overlay ---------------------------------------------------

        private void OnGUI()
        {
            // Only the active tab renders. None = the overlay is hidden.
            if (activeTab == DebugTab.None) return;
            EnsureStyles();

            var area = new Rect(10f, 10f, 340f, Mathf.Max(140f, Screen.height - 20f));
            GUILayout.BeginArea(area, boxStyle);

            GUILayout.Label("MK DEBUG  ·  F1-F4 tabs (press again to hide)", labelStyle);
            DrawTabHeader();

            GUILayout.Space(6f);
            switch (activeTab)
            {
                case DebugTab.Music: DrawMusicTab(); break;
                case DebugTab.Enemies: DrawEnemiesTab(); break;
                case DebugTab.Player: DrawPlayerTab(); break;
                case DebugTab.Etc: DrawEtcTab(); break;
            }

            GUILayout.EndArea();
        }

        private void DrawTabHeader()
        {
            GUILayout.BeginHorizontal();
            DrawTabButton(DebugTab.Music, "MÚSICA [F1]");
            DrawTabButton(DebugTab.Enemies, "ENEMIGOS [F2]");
            DrawTabButton(DebugTab.Player, "PLAYER [F3]");
            DrawTabButton(DebugTab.Etc, "ETC [F4]");
            GUILayout.EndHorizontal();
        }

        private void DrawTabButton(DebugTab tab, string text)
        {
            bool isActive = activeTab == tab;
            Color previous = GUI.backgroundColor;
            if (isActive) GUI.backgroundColor = new Color(0.25f, 0.7f, 1f);
            if (GUILayout.Button(text, isActive ? tabStyle : GUI.skin.button)) pendingTab = tab;
            GUI.backgroundColor = previous;
        }

        private void DrawMusicTab()
        {
            GUILayout.Label("MÚSICA · [F5] music  [F6] ambience  [F7] mute  [F8] SFX", labelStyle);
            GUILayout.Label("[F9-F12] reservado", labelStyle);

            GUILayout.Space(4f);
            GUILayout.Label($"master volume: {AudioDirector.MasterVolume:0.00}", labelStyle);
            float master = GUILayout.HorizontalSlider(AudioDirector.MasterVolume, 0f, 1f);
            if (!Mathf.Approximately(master, AudioDirector.MasterVolume))
            {
                AudioDirector.MasterVolume = master;
                muted = master <= 0.0001f;
                if (!muted) volumeBeforeMute = master;
            }

            string audition = sfxAuditionIndex >= 0
                ? SfxAuditionCycle[sfxAuditionIndex].ToString()
                : "<none>";
            GUILayout.Label($"music: {AudioDirector.CurrentMusic}   [F5]", labelStyle);
            GUILayout.Label($"ambience: {AudioDirector.CurrentAmbience}   [F6]", labelStyle);
            GUILayout.Label($"mute: {(muted ? "ON (0.00)" : "OFF")}   [F7]", labelStyle);
            GUILayout.Label($"SFX audition: {audition}   [F8]", labelStyle);
        }

        private void DrawEnemiesTab()
        {
            GUILayout.Label("ENEMIGOS · [F5] spawn  [F6] boss (B)  [F7] kill all  [F8] freeze AI", labelStyle);
            GUILayout.Label("[F9-F12] reservado", labelStyle);

            GUILayout.Space(4f);
            var boss = FindFirstObjectByType<BossController>();
            GUILayout.Label($"enemies: {CountEnemies()}", labelStyle);
            GUILayout.Label($"attackers: {CombatCoordinator.Instance?.AttackerCount ?? 0}", labelStyle);
            GUILayout.Label(
                boss != null ? $"boss: {boss.Phase} {boss.HP}/{boss.MaxHp}" : "boss: none",
                labelStyle);
            GUILayout.Label($"AI freeze: {(aiFrozen ? "ON" : "OFF")}   [F8]", labelStyle);
        }

        private void DrawPlayerTab()
        {
            GUILayout.Label("PLAYER · [F5] reset tuning  [F6] log  [F7] states  [F8] invuln  [F9] test dmg  [F10] kill", labelStyle);

            GUILayout.Space(4f);
            DrawCombatTuning();

            ResolveTargets();
            GUILayout.Label(
                $"invulnerable: {playerInvulnerable || (damage != null && damage.IsInvulnerable)}   [F8]",
                labelStyle);

            if (showStates)
            {
                GUILayout.Space(6f);
                DrawStates();
            }
            else
            {
                GUILayout.Label("estados: OFF   [F7]", labelStyle);
            }
        }

        private void DrawEtcTab()
        {
            GUILayout.Label("ETC · [F5] dummy  [F6] timeScale  [F7] clear dummies  [F8] teleport", labelStyle);
            GUILayout.Label("[F9-F12] reservado", labelStyle);

            GUILayout.Space(4f);
            float fps = 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            GUILayout.Label($"timeScale: {Time.timeScale:0.##}   [F6]", labelStyle);
            GUILayout.Label($"dummies: {spawned.Count}/{MaxDummies}   [F7] clear", labelStyle);
            GUILayout.Label($"scene: {gameObject.scene.name}", labelStyle);
            GUILayout.Label($"FPS: {fps:0}", labelStyle);
            GUILayout.Label($"Unity: {Application.unityVersion}", labelStyle);
        }

        /// <summary>
        /// LIVE COMBAT TUNING (WU6): the five global scales PlayerCombat multiplies the
        /// per-step data by. Writing them on the player's <see cref="PlayerParams"/> asset
        /// retunes combat DURING Play Mode with no recompile and no asset edit on disk.
        /// Identity = 1/1/1/1/0.
        /// </summary>
        private void DrawCombatTuning()
        {
            GUILayout.Space(6f);
            GUILayout.Label("COMBAT TUNING (live)", labelStyle);

            if (!TryGetPlayerParams(out PlayerParams p))
            {
                GUILayout.Label("combat tuning: <none>", labelStyle);
                return;
            }

            p.attackSpeedScale = GUILayout.HorizontalSlider(p.attackSpeedScale, 0.5f, 2f);
            float effectiveMultiplier = 4.75f * Mathf.Max(0.01f, p.attackSpeedScale);
            float stepSeconds = 2.5f / Mathf.Max(0.01f, effectiveMultiplier);
            GUILayout.Label(
                $"ATAQUE x{p.attackSpeedScale:0.00}  ·  {effectiveMultiplier:0.00}x = {stepSeconds:0.000} s", labelStyle);

            p.attackReachScale = GUILayout.HorizontalSlider(p.attackReachScale, 0.5f, 1.5f);
            GUILayout.Label(
                $"ALCANCE x{p.attackReachScale:0.00}  ·  1.9 m -> {1.9f * p.attackReachScale:0.00} m", labelStyle);

            p.hitstopScale = GUILayout.HorizontalSlider(p.hitstopScale, 0f, 2f);
            GUILayout.Label(
                $"HITSTOP x{p.hitstopScale:0.00}  ·  70 ms -> {70f * p.hitstopScale:0} ms", labelStyle);

            p.cancelStartScale = GUILayout.HorizontalSlider(p.cancelStartScale, 0.5f, 1.5f);
            float cancelStart = combat != null ? combat.ActiveCancelStartNormalized : -1f;
            string cancelReadout = cancelStart >= 0f
                ? $"{cancelStart * 100f:0}% -> {cancelStart / Mathf.Max(0.01f, p.cancelStartScale) * 100f:0}%"
                : "sin step activo";
            GUILayout.Label(
                $"CANCEL x{p.cancelStartScale:0.00}  ·  {cancelReadout}", labelStyle);

            // Integer cap: 0 = each step's own data maxTargets.
            p.maxTargetsOverride = Mathf.RoundToInt(
                GUILayout.HorizontalSlider(p.maxTargetsOverride, 0f, 6f));
            GUILayout.Label(
                p.maxTargetsOverride > 0
                    ? $"TARGETS {p.maxTargetsOverride} (override)"
                    : "TARGETS 0 = data",
                labelStyle);

            p.directionFlickWindow = GUILayout.HorizontalSlider(p.directionFlickWindow, 0.05f, 0.5f);
            GUILayout.Label(
                $"FLICK {p.directionFlickWindow * 1000f:0} ms  ·  más tiempo = movimiento (combo neutral), menos = variante direccional",
                labelStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("RESET")) ResetCombatTuning();
            if (GUILayout.Button("LOG")) LogCombatTuning();
            GUILayout.EndHorizontal();
        }

        private void DrawStates()
        {
            if (motor == null)
            {
                GUILayout.Label("player: <none>", labelStyle);
                return;
            }

            var moveState = visual != null ? visual.CurrentMoveState : MoveState.Free;
            GUILayout.Label($"MoveState:     {moveState}", labelStyle);
            GUILayout.Label($"grounded:      {motor.IsGrounded}", labelStyle);
            GUILayout.Label($"facingLocked:  {motor.FacingLocked}", labelStyle);

            Vector3 facing = motor.DesiredFacing;
            GUILayout.Label($"desiredFacing: ({facing.x:0.00}, {facing.z:0.00})", labelStyle);

            if (body != null)
            {
                Vector3 v = body.linearVelocity;
                GUILayout.Label($"speed XZ:      {new Vector2(v.x, v.z).magnitude:0.00} m/s", labelStyle);
                GUILayout.Label($"vertical:      {v.y:0.00} m/s", labelStyle);
            }

            // Style meter (slice style-meter): ONE line — rank + points + charge
            // pips. No real HUD; the observer lives on the Player (Gym wiring).
            if (styleMeter == null) styleMeter = FindFirstObjectByType<StyleMeter>();
            if (styleMeter != null)
            {
                int pts = Mathf.FloorToInt(styleMeter.Points);
                int max = Mathf.Max(0, styleMeter.MaxCharges);
                int n = Mathf.Clamp(styleMeter.Charges, 0, max);
                string pips = new string('●', n) + new string('○', max - n);
                GUILayout.Label($"ESTILO {styleMeter.Rank} {pts} [{pips}]", labelStyle);
            }
            else
            {
                GUILayout.Label("ESTILO -- (sin medidor)", labelStyle);
            }

            // INPUT / GATES (m3-build-feedback): the tester build must SHOW which
            // device/control actually arrives and why an action is denied. Generic
            // <Joystick> devices are the reason the gamepad bindings may never match —
            // this readout is the mapping tool. Observe-only: drives nothing.
            GUILayout.Space(6f);
            GUILayout.Label("INPUT / GATES", labelStyle);
            GUILayout.Label($"last: {lastInputDevice} / {lastInputPath}", labelStyle);
            GUILayout.Label($"devices: {DescribeDevices()}", labelStyle);
            GUILayout.Label($"ambience: {AudioDirector.CurrentAmbience}", labelStyle);

            // Boss readout (M3.4): phase + current-phase HP. Dedicated boss bar is
            // a later step; this keeps the two-phase fight observable right now.
            var boss = FindFirstObjectByType<BossController>();
            GUILayout.Label(
                boss != null ? $"boss: {boss.Phase} {boss.HP}/{boss.MaxHp}" : "boss: none",
                labelStyle);

            bool held = motor.Body != null && motor.Body.IsHeld;
            GUILayout.Label($"bodyOwner: {motor.BodyOwner?.ToString() ?? "none"}  held: {held}", labelStyle);
            GUILayout.Label($"inputLocked: {motor.InputLocked}", labelStyle);

            bool moveGate = motor.CanPerform(PlayerAction.Move) && !motor.InputLocked;
            bool jumpGate = motor.CanPerform(PlayerAction.Jump) && !held;
            bool dashGate = motor.CanPerform(PlayerAction.Dash) && motor.Body != null &&
                            motor.Body.CanAcquire(BodyPriority.Dash);
            bool skillGate = motor.CanPerform(PlayerAction.Skill1) && motor.Body != null &&
                             motor.Body.CanAcquire(BodyPriority.Attack) &&
                             (damage == null || damage.IsAlive);

            GUILayout.Label(
                $"Move {Gate(moveGate)}  Jump {Gate(jumpGate)}  Dash {Gate(dashGate)}  Skill {Gate(skillGate)}",
                labelStyle);
            GUILayout.Label(
                $"alive: {(damage != null ? (damage.IsAlive ? "OK" : "DEAD") : "<none>")}  " +
                $"hp: {(damage != null ? damage.HP + "/" + damage.MaxHp : "--")}",
                labelStyle);
            GUILayout.Label($"abilityBusy: {abilities != null && abilities.IsBusy}", labelStyle);
        }

        private static string Gate(bool allowed) => allowed ? "OK" : "BLOCK";

        private static int CountEnemies()
        {
            var enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null) count++;
            }
            return count;
        }

        private static string DescribeDevices()
        {
            var devices = InputSystem.devices;
            if (devices.Count == 0) return "<none>";
            var sb = new StringBuilder();
            for (int i = 0; i < devices.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(devices[i] != null ? devices[i].displayName : "<null>");
            }
            return sb.ToString();
        }

        private void EnsureStyles()
        {
            if (background == null)
            {
                background = new Texture2D(1, 1);
                background.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.7f));
                background.Apply();
            }
            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    alignment = TextAnchor.UpperLeft
                };
                boxStyle.normal.background = background;
            }
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    wordWrap = true // long generic-joystick display names must not clip
                };
                labelStyle.normal.textColor = Color.white;
            }
            if (tabStyle == null)
            {
                tabStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };
                tabStyle.normal.textColor = Color.white;
                tabStyle.hover.textColor = Color.white;
                tabStyle.active.textColor = Color.white;
            }
        }
#endif
    }
}
