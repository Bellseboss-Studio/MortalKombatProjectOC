using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
    /// Keys (Input System only — legacy Input is dead, activeInputHandler:1):
    ///   F1 toggle window · F2 spawn dummy · F3 toggle states readout · F4 cycle timeScale.
    /// F5..F12 and a PauseManager are DEFERRED to M1+ (see 03-sistemas.md).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebugWindow : MonoBehaviour
    {
        // timeScale cycle: 0 → 0.25 → 0.5 → 1 → 2. Starts at 1x (index 3).
        // NOTE: timeScale = 0 freezes the Animator + FixedUpdate; this OnGUI overlay
        // keeps rendering because OnGUI is not tied to scaled time. A real PauseManager
        // is deferred debt — this is a direct Time.timeScale write, nothing more.
        private static readonly float[] TimeScales = { 0f, 0.25f, 0.5f, 1f, 2f };
        private const int MaxDummies = 5;          // small count, FIFO recycle past 5
        private const float SpawnAhead = 3f;       // units in front of the player
        private const float CapsuleHalfHeight = 1f;

        [Tooltip("Window visibility. F1 toggles this at runtime.")]
        [SerializeField] private bool windowVisible = true;

        [Tooltip("States readout body (MoveState / grounded / facing / speed). F3 toggles.")]
        [SerializeField] private bool showStates = true;

        private int timeScaleIndex = 3; // 1x
        private PlayerMotor motor;
        private AnimatorVisual visual;
        private Rigidbody body;
        private Material greyMaterial;
        private readonly List<GameObject> spawned = new List<GameObject>();

        private Texture2D background;
        private GUIStyle boxStyle;
        private GUIStyle labelStyle;

        private void Awake()
        {
            ResolveTargets();
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
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return; // no device attached — nothing to toggle
            if (keyboard.f1Key.wasPressedThisFrame) windowVisible = !windowVisible;
            if (keyboard.f2Key.wasPressedThisFrame) SpawnDummy();
            if (keyboard.f3Key.wasPressedThisFrame) showStates = !showStates;
            if (keyboard.f4Key.wasPressedThisFrame) CycleTimeScale();
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

        // --- IMGUI overlay ---------------------------------------------------

        private void OnGUI()
        {
            if (!windowVisible) return;
            EnsureStyles();

            var area = new Rect(10f, 10f, 340f, Mathf.Max(140f, Screen.height - 20f));
            GUILayout.BeginArea(area, boxStyle);

            GUILayout.Label("MK DEBUG  ·  [F1] toggle", labelStyle);
            GUILayout.Label($"timeScale: {Time.timeScale:0.##}   [F4] cycle", labelStyle);
            GUILayout.Label("[F2] spawn dummy", labelStyle);
            GUILayout.Label($"[F3] estados: {(showStates ? "ON" : "OFF")}", labelStyle);

            if (showStates)
            {
                GUILayout.Space(6f);
                DrawStates();
            }

            GUILayout.EndArea();
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
                    fontStyle = FontStyle.Bold
                };
                labelStyle.normal.textColor = Color.white;
            }
        }
    }
}
