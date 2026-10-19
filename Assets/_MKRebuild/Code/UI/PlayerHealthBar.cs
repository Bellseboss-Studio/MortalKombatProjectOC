using UnityEngine;
using UnityEngine.UI;

namespace MK.Rebuild
{
    /// <summary>
    /// M2 gate HUD: the minimal health bar the GDD asks for (no numbers, no events).
    /// Polls <see cref="PlayerDamage"/> in <see cref="Update"/> and writes the fill only
    /// when the fraction changes, so respawn's <see cref="PlayerDamage.Heal"/> is
    /// reflected automatically. Tints by fraction so danger is readable at a glance.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHealthBar : MonoBehaviour
    {
        [Tooltip("Hurt source. Resolved from the scene in Awake when left unset (serialized-ref + runtime-fallback convention).")]
        [SerializeField] private PlayerDamage damage;

        [Tooltip("Filled Image driven by the HP fraction.")]
        [SerializeField] private Image fill;

        [Header("Tint by fraction")]
        [SerializeField] private Color healthyColor = new Color(0.24f, 0.78f, 0.29f, 1f);
        [SerializeField] private Color warningColor = new Color(0.95f, 0.78f, 0.16f, 1f);
        [SerializeField] private Color dangerColor = new Color(0.88f, 0.16f, 0.16f, 1f);

        private float lastFraction = -1f;
        private static Sprite solidFillSprite;

        private void Awake()
        {
            if (damage == null) damage = FindFirstObjectByType<PlayerDamage>();
            EnsureFillSprite();
        }

        private void Update()
        {
            if (damage == null || fill == null) return;

            float fraction = Mathf.Clamp01((float)damage.HP / Mathf.Max(1, damage.MaxHp));
            if (Mathf.Approximately(fraction, lastFraction)) return;

            lastFraction = fraction;
            fill.fillAmount = fraction;
            fill.color = fraction > 0.5f ? healthyColor : fraction > 0.25f ? warningColor : dangerColor;
        }

        /// <summary>
        /// A null-sprite Image renders a solid rect, but uGUI ignores <c>fillAmount</c> when
        /// the sprite is null. Give the fill the built-in white texture as a sprite so the
        /// Filled type actually works without adding an art asset.
        /// </summary>
        private void EnsureFillSprite()
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
                solidFillSprite.name = "HUD_SolidFill";
            }

            fill.sprite = solidFillSprite;
        }
    }
}
