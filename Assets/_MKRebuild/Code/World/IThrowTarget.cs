using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Marker seam for anything a thrown body may target: enemies AND world props
    /// (puzzle levers, breakables). Query users resolve a collider to this interface and
    /// read the aim point; they never need to know what the prop actually is.
    ///
    /// WHY an interface instead of another HitReceiver check: the throw/aim systems must
    /// stay agnostic of enemy controllers and puzzle scripts, and a prop that is not a
    /// damageable is not a <see cref="HitReceiver"/>.
    /// </summary>
    public interface IThrowTarget
    {
        /// <summary>World transform the throw aims at (may be an override point, e.g. a lever handle).</summary>
        Transform ThrowTargetTransform { get; }

        /// <summary>True while this target may be picked (authored flag AND the component enabled).</summary>
        bool IsThrowTargetActive { get; }
    }
}
