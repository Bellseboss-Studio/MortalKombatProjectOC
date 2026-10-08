using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// M0 marker: the collider on this object (or a child) can be grabbed and
    /// wall-jumped from. A near-vertical collider WITHOUT this component is NOT
    /// grabbable (e.g. the Dummy, hazards, generic level geometry).
    /// Intentionally a marker component (not a layer) to keep the wall-motion
    /// feature isolated from ProjectSettings.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Grabbable : MonoBehaviour
    {
    }
}
