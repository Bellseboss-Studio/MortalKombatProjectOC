using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Debug overlay that draws a world-anchored label above every enemy: archetype, AI
    /// state and a tell/recovery bar.
    ///
    /// Lives in the scene (or in its prefab) so every value is editable from the Inspector
    /// without entering play mode. IMGUI on purpose: zero assets, zero canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class EnemyStatusOverlay : MonoBehaviour
    {
        [Header("Toggle")]
        [Tooltip("Draw the labels. Also driven by the DebugWindow (F2 · Enemigos).")]
        [SerializeField] private bool visible = true;

        [Header("Layout")]
        [Tooltip("Height above the enemy root (feet) where the label anchors, in metres.")]
        [SerializeField] private float headOffset = 2.1f;

        [SerializeField] private Vector2 labelSize = new Vector2(124f, 18f);
        [SerializeField, Min(6)] private int fontSize = 11;
        [SerializeField] private float barWidth = 68f;
        [SerializeField] private float barHeight = 4f;
        [SerializeField] private float barGap = 5f;

        [Header("Content")]
        [SerializeField] private bool showHp = true;
        [Tooltip("Show a label for enemies that are not attacking (Idle/Chase/Flee).")]
        [SerializeField] private bool showPassiveStates = true;

        [Header("Colors")]
        [SerializeField] private Color tellStartColor = new Color(1f, 0.92f, 0.25f);
        [SerializeField] private Color tellEndColor = new Color(1f, 0.32f, 0.08f);
        [SerializeField] private Color recoveryColor = new Color(0.30f, 0.62f, 1f);
        [SerializeField] private Color staggerColor = new Color(0.35f, 1f, 0.45f);
        [SerializeField] private Color chaseColor = new Color(0.72f, 0.72f, 0.72f);
        [SerializeField] private Color fleeColor = new Color(0.85f, 0.65f, 1f);
        [SerializeField] private Color deadColor = new Color(0.35f, 0.35f, 0.35f);
        [SerializeField] private Color idleColor = new Color(0.62f, 0.62f, 0.62f);

        private const float RefreshSeconds = 0.5f;

        private static readonly List<EnemyController> Enemies = new();
        private static float nextRefresh;
        private GUIStyle style;

        public static EnemyStatusOverlay Instance { get; private set; }

        public bool Visible
        {
            get => visible;
            set => visible = value;
        }

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnGUI()
        {
            if (!visible) return;

            var cam = Camera.main;
            if (cam == null) return;

            if (Time.unscaledTime >= nextRefresh)
            {
                nextRefresh = Time.unscaledTime + RefreshSeconds;
                Enemies.Clear();
                Enemies.AddRange(UnityEngine.Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None));
            }

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = fontSize,
                    alignment = TextAnchor.MiddleCenter,
                    richText = false,
                };
            }

            for (var i = 0; i < Enemies.Count; i++)
            {
                var enemy = Enemies[i];
                if (enemy == null || !enemy.isActiveAndEnabled) continue;

                var world = enemy.transform.position + Vector3.up * headOffset;
                var projected = cam.WorldToScreenPoint(world);
                if (projected.z <= 0f) continue;

                DrawEntry(enemy, new Vector2(projected.x, Screen.height - projected.y));
            }
        }

        private void DrawEntry(EnemyController enemy, Vector2 screen)
        {
            var state = enemy.DebugState ?? "?";
            var tell = Mathf.Max(0.0001f, enemy.DebugTell);
            var recovery = Mathf.Max(0.0001f, enemy.DebugRecovery);
            var timer = enemy.DebugAttackTimer;

            var color = idleColor;
            var label = "IDLE";
            var fill = 0f;
            var passive = true;

            switch (state)
            {
                case "Attack":
                case "Charge":
                case "RangedAttack":
                case "Backstab":
                    passive = false;
                    if (timer < tell)
                    {
                        fill = Mathf.Clamp01(timer / tell);
                        label = "TELL " + Mathf.RoundToInt(fill * 100f) + "%";
                        color = Color.Lerp(tellStartColor, tellEndColor, fill);
                    }
                    else
                    {
                        fill = Mathf.Clamp01((timer - tell) / recovery);
                        label = "RECOV " + Mathf.RoundToInt((1f - fill) * 100f) + "%";
                        color = recoveryColor;
                    }
                    break;

                case "Stagger":
                    passive = false;
                    label = "STAGGER";
                    color = staggerColor;
                    fill = 1f;
                    break;

                case "Chase":
                    label = "CHASE";
                    color = chaseColor;
                    break;

                case "Flee":
                    label = "FLEE";
                    color = fleeColor;
                    break;

                case "Dead":
                    label = "DEAD";
                    color = deadColor;
                    break;
            }

            if (passive && !showPassiveStates) return;

            var receiver = enemy.GetComponent<HitReceiver>();
            var hp = showHp && receiver != null ? "  " + receiver.HP + "/" + receiver.MaxHp : string.Empty;
            var text = ShortArchetype(enemy.DebugArchetype) + " " + label + hp;

            var rect = new Rect(
                screen.x - labelSize.x * 0.5f,
                screen.y - labelSize.y * 0.5f,
                labelSize.x,
                labelSize.y);

            var previous = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
            GUI.color = color;
            GUI.Label(rect, text, style);

            var bar = new Rect(
                screen.x - barWidth * 0.5f,
                screen.y + barGap,
                barWidth,
                barHeight);

            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(bar, Texture2D.whiteTexture);

            if (fill > 0f)
            {
                GUI.color = color;
                GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * fill, bar.height), Texture2D.whiteTexture);
            }

            GUI.color = previous;
        }

        private static string ShortArchetype(string id)
        {
            if (string.IsNullOrEmpty(id)) return "?";

            if (id.EndsWith("esbirro")) return "ESB";
            if (id.EndsWith("tirador")) return "TIR";
            if (id.EndsWith("punalada")) return "PUN";
            if (id.EndsWith("embestidor")) return "EMB";
            if (id.EndsWith("brute")) return "BOSS";

            return id.Length <= 3 ? id.ToUpperInvariant() : id.Substring(0, 3).ToUpperInvariant();
        }
    }
}
