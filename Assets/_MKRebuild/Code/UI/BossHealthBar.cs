using UnityEngine;
using UnityEngine.UI;

namespace MK.Rebuild
{
    /// <summary>
    /// M3.5b HUD: the two-segment boss health bar. Arena-scoped presentation: the bar
    /// is visible only while the boss is alive AND the player is inside the boss arena,
    /// so a respawn outside the zone never leaves the HUD behind. The bar is bound by
    /// <see cref="BossController"/> when the fight starts and hidden on death, on unbound,
    /// or when the player leaves the arena. No numbers, no events — just two Filled images.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossHealthBar : MonoBehaviour
    {
        [Tooltip("Boss arena whose volume scopes the bar. Resolved from BossArena.Instance " +
                  "lazily when unassigned.")]
        [SerializeField] private BossArena arena;

        [Tooltip("Root object toggled by Bind/Hide (the BossBar container).")]
        [SerializeField] private GameObject root;

        [Tooltip("Phase-1 fill (left half).")]
        [SerializeField] private Image phase1Fill;

        [Tooltip("Phase-2 fill (middle / right half).")]
        [SerializeField] private Image phase2Fill;

        [Tooltip("Phase-3 fill (third segment). Hidden on two-phase fights.")]
        [SerializeField] private Image phase3Fill;

        public static BossHealthBar Instance { get; private set; }

        private BossController boss;
        private static Sprite solidFillSprite;

        private void Awake()
        {
            Instance = this;
            EnsureFillSprites();
            Hide();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Shows the bar and binds it to the live boss.</summary>
        public void Bind(BossController bossController)
        {
            boss = bossController;
            if (root != null) root.SetActive(true);
            Refresh();
        }

        /// <summary>Hides the bar (boss dead, unbound, player outside the arena or scene leaving).</summary>
        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        private void Update()
        {
            if (boss == null || boss.IsDead)
            {
                Hide();
                return;
            }

            // Lazy resolve: Awake order between the HUD and the arena is not guaranteed.
            if (arena == null) arena = BossArena.Instance;

            // Arena-scoped: an alive boss outside the zone must not keep the bar on
            // screen (e.g. the player respawns at the S10 checkpoint after a death).
            if (arena != null && !arena.PlayerInside)
            {
                Hide();
                return;
            }

            // Re-entry: Hide() left the root off, so restore it once the player is
            // back in the zone (Bind only fires on spawn/reset).
            if (root != null && !root.activeSelf) root.SetActive(true);

            Refresh();
        }

        private void Refresh()
        {
            if (boss == null) return;
            ApplySegmentLayout(boss.PhaseCount);
            if (phase1Fill != null) phase1Fill.fillAmount = boss.Phase1Normalized;
            if (phase2Fill != null) phase2Fill.fillAmount = boss.Phase2Normalized;
            if (phase3Fill != null) phase3Fill.fillAmount = boss.Phase3Normalized;
        }

        /// <summary>
        /// Splits the bar into as many segments as the fight has phases. Two phases keep the
        /// authored halves; three phases split it into thirds and show the extra fill.
        /// </summary>
        private void ApplySegmentLayout(int phaseCount)
        {
            bool three = phaseCount >= 3;

            SetSegment(phase1Fill, 0f, three ? 1f / 3f : 0.5f);
            SetSegment(phase2Fill, three ? 1f / 3f : 0.5f, three ? 2f / 3f : 1f);

            if (phase3Fill == null) return;
            if (phase3Fill.gameObject.activeSelf != three) phase3Fill.gameObject.SetActive(three);
            if (three) SetSegment(phase3Fill, 2f / 3f, 1f);
        }

        private static void SetSegment(Image fill, float min, float max)
        {
            if (fill == null) return;
            var rect = fill.rectTransform;
            rect.anchorMin = new Vector2(min, rect.anchorMin.y);
            rect.anchorMax = new Vector2(max, rect.anchorMax.y);
        }

        /// <summary>
        /// uGUI ignores <c>fillAmount</c> when the sprite is null, so give both fills
        /// the built-in white texture as a sprite (same trick as PlayerHealthBar).
        /// </summary>
        private void EnsureFillSprites()
        {
            if (phase1Fill != null) EnsureFillSprite(phase1Fill);
            if (phase2Fill != null) EnsureFillSprite(phase2Fill);
            if (phase3Fill != null) EnsureFillSprite(phase3Fill);
        }

        private static void EnsureFillSprite(Image fill)
        {
            if (fill == null || fill.sprite != null) return;

            if (solidFillSprite == null)
            {
                Texture2D white = Texture2D.whiteTexture;
                solidFillSprite = Sprite.Create(
                    white,
                    new Rect(0f, 0f, white.width, white.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                solidFillSprite.name = "BossHUD_SolidFill";
            }

            fill.sprite = solidFillSprite;
        }
    }
}
