using System;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// One timed action inside a beat. <see cref="time"/> is seconds from the beat
    /// start (0 = immediately, while the Timeline is still playing).
    ///
    /// <see cref="action"/> is serialized as a MONOBEHAVIOUR with an interface
    /// constraint, using the UnityInterfaceSupport package: the inspector only accepts
    /// a component that implements <see cref="ICinematicAction"/>, so the definition
    /// stays data-driven (no concrete action class in the asset schema). The director
    /// casts at fire time and logs one warning if the reference does not implement it.
    /// </summary>
    [Serializable]
    public struct CinematicEvent
    {
        [Tooltip("Seconds from the beat start. 0 = as soon as the beat begins.")]
        public float time;

        [Tooltip("Component that implements ICinematicAction. Only compatible components are accepted.")]
        [InterfaceType(typeof(ICinematicAction))]
        public MonoBehaviour action;
    }
}
