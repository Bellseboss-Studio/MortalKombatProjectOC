using UnityEngine;
using UnityEngine.UI;

namespace MK.Rebuild
{
    /// <summary>
    /// M3.5b HUD: the two-segment boss health bar. The bar is bound by
    /// <see cref="BossController"/> when the fight starts and hidden on death or
    /// when the boss is unbound. No numbers, no events — just two Filled images.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossHealthBar : MonoBehaviour
    {
        [Tooltip("Root object toggled by Bind/Hide (the BossBar container).")]
        [SerializeField] private GameObject root;

        [Tooltip("Phase-1 fill (left half).")]
        [SerializeField] private Image phase1Fill;

        [Tooltip("Phase-2 fill (right half).")]
        [SerializeField] private Image phase2Fill;

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

        /// <summary>Hides the bar (boss dead, unbound or scene leaving).</summary>
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

            Refresh();
        }

        private void Refresh()
        {
            if (boss == null) return;
            if (phase1Fill != null) phase1Fill.fillAmount = boss.Phase1Normalized;
            if (phase2Fill != null) phase2Fill.fillAmount = boss.Phase2Normalized;
        }

        /// <summary>
        /// uGUI ignores <c>fillAmount</c> when the sprite is null, so give both fills
        /// the built-in white texture as a sprite (same trick as PlayerHealthBar).
        /// </summary>
        private void EnsureFillSprites()
        {
            if (phase1Fill != null) EnsureFillSprite(phase1Fill);
            if (phase2Fill != null) EnsureFillSprite(phase2Fill);
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
