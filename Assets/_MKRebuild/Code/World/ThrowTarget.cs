using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Drop-in marker that makes ANY puzzle prop (lever, valve, breakable) targetable by
    /// the throw aim without touching enemy code. Add it to the prop and, if the visual
    /// pivot is not the point the throw should hit, assign an override transform (e.g. the
    /// lever handle).
    ///
    /// Resolution order in <see cref="AimAssist.ResolveThrowable"/>: an ACTIVE
    /// <see cref="IThrowTarget"/> wins over a <see cref="HitReceiver"/>, so a prop that is
    /// also damageable can opt into the throw cone explicitly.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ThrowTarget : MonoBehaviour, IThrowTarget
    {
        [Tooltip("Master switch for this prop. Off = it is ignored by the throw/aim selection.")]
        [SerializeField] private bool isActive = true;

        [Tooltip("Optional aim point (e.g. the lever handle). Null = this transform.")]
        [SerializeField] private Transform overrideTransform;

        /// <summary>World transform the throw aims at (override when set, otherwise this transform).</summary>
        public Transform ThrowTargetTransform => overrideTransform != null ? overrideTransform : transform;

        /// <summary>True only while the authored flag is on AND the component is enabled.</summary>
        public bool IsThrowTargetActive => isActive && isActiveAndEnabled;
    }
}
