using UnityEngine;
using UnityEngine.InputSystem;

namespace MK.Rebuild.Debugging
{
    /// <summary>
    /// DEBUG-ONLY driver for the player hurt path (anim-packs-integration slice 3-uno).
    /// Lives in the isolated <c>MK.Rebuild.Debug</c> assembly (<c>autoReferenced:false</c>)
    /// and ADHERES to the game read-only: it never drives gameplay itself, it only calls
    /// <see cref="PlayerDamage"/> on the player (the same observe-only contract as
    /// <see cref="DebugWindow"/>, whose PLAYER tab owns the hotkeys now).
    ///
    /// Triggers (WU7): <see cref="DebugWindow"/> F9/F10 call
    /// <see cref="ApplyTestDamage"/> / <see cref="KillPlayer"/>. The legacy F6/F7
    /// polling in <see cref="Update"/> is OFF by default (<c>enableHotkeys</c>) so it
    /// cannot collide with the tab dispatch.
    ///   F9  = take 6 damage  -> hit reaction one-shot, returns to locomotion
    ///   F10 = kill           -> death clip spins out once, HOLDS the final pose
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

        [Tooltip("Poll the legacy F6/F7 hotkeys in Update. OFF by default — the DebugWindow PLAYER tab owns F9/F10 now.")]
        [SerializeField] private bool enableHotkeys = false;

        private void Update()
        {
            if (!enableHotkeys) return;

            var keyboard = Keyboard.current;
            if (keyboard == null) return; // no device attached — nothing to drive

            if (damage == null) damage = FindFirstObjectByType<PlayerDamage>();
            if (damage == null) return;

            if (keyboard.f6Key.wasPressedThisFrame) damage.TakeHit(TestDamage, transform.forward);
            if (keyboard.f7Key.wasPressedThisFrame) damage.Kill();
        }

        /// <summary>Debug seam: applies the frozen 6-damage test hit to the player.</summary>
        public void ApplyTestDamage()
        {
            if (damage == null) damage = FindFirstObjectByType<PlayerDamage>();
            if (damage == null) return;
            damage.TakeHit(TestDamage, transform.forward);
        }

        /// <summary>Debug seam: kills the player (death clip + <c>OnDied</c>).</summary>
        public void KillPlayer()
        {
            if (damage == null) damage = FindFirstObjectByType<PlayerDamage>();
            if (damage != null) damage.Kill();
        }
#endif
    }
}
