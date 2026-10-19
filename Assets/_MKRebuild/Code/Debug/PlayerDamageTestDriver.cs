using UnityEngine;
using UnityEngine.InputSystem;

namespace MK.Rebuild.Debugging
{
    /// <summary>
    /// DEBUG-ONLY driver for the player hurt path (anim-packs-integration slice 3-uno).
    /// Lives in the isolated <c>MK.Rebuild.Debug</c> assembly (<c>autoReferenced:false</c>)
    /// and ADHERES to the game read-only: it never drives gameplay itself, it only calls
    /// <see cref="PlayerDamage"/> on the player (the same observe-only contract as
    /// <see cref="DebugWindow"/>, which owns F1-F4).
    ///
    /// Frozen test trigger (design open-question Q1): no scene trigger volume, no MCP —
    /// deterministic debug keys.
    ///   F6 = take 6 damage  -> hit reaction one-shot, returns to locomotion
    ///   F7 = kill           -> death clip spins out once, HOLDS the final pose
    ///
    /// Boot: a manual component on the <c>Debug</c> GameObject (same pattern as
    /// <see cref="DebugWindow"/> — NO <c>[RuntimeInitializeOnLoadMethod]</c>).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerDamageTestDriver : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private const int TestDamage = 6;

        [Tooltip("Player hurt path to drive. Null = auto-resolved (first PlayerDamage in the scene).")]
        [SerializeField] private PlayerDamage damage;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return; // no device attached — nothing to drive

            if (damage == null) damage = FindFirstObjectByType<PlayerDamage>();
            if (damage == null) return;

            if (keyboard.f6Key.wasPressedThisFrame) damage.TakeHit(TestDamage, transform.forward);
            if (keyboard.f7Key.wasPressedThisFrame) damage.Kill();
        }
#endif
    }
}
