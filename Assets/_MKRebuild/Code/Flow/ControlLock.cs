using System.Collections.Generic;
using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Why control is being held. Only <see cref="Pause"/> and <see cref="Hitstop"/>
    /// stop time; <see cref="Cinematic"/> is recorded so the stack reflects the
    /// scripted state, but a cinematic must keep time running (Timeline + animators).
    /// </summary>
    public enum ControlReason
    {
        Pause,
        Cinematic,
        Hitstop,
    }

    /// <summary>
    /// Single arbiter for time control (change camera-cinematics-flow): holds a reason
    /// stack and is the ONLY place in the rebuild that writes <see cref="Time.timeScale"/>.
    /// One owner removes the old hitstop save/restore race, where two systems could
    /// restore each other's stale scale.
    ///
    /// Acquire returns an opaque handle; release that SAME handle. Release is idempotent
    /// and does NOT require LIFO order, so if a hitstop expires while the game is paused,
    /// Pause keeps time frozen until it releases too.
    /// </summary>
    public static class ControlLock
    {
        /// <summary>Opaque token returned by <see cref="Acquire"/>; release it exactly once.</summary>
        public sealed class Handle
        {
            internal Handle(ControlReason reason, int seq)
            {
                Reason = reason;
                Seq = seq;
            }

            /// <summary>Reason this handle was acquired with (debug/read-only).</summary>
            public ControlReason Reason { get; }

            /// <summary>Acquisition order (lower = earlier); breaks equal-reason ties.</summary>
            public int Seq { get; }
        }

        private struct Entry
        {
            public Handle Handle;
            public ControlReason Reason;
            public int Seq;
        }

        private static readonly List<Entry> entries = new List<Entry>();
        private static int nextSeq;
        private static float baseScale = 1f;
        private static float effectiveScale = 1f;

        /// <summary>
        /// True while a Pause handle is held. Deliberately NOT "EffectiveScale == 0":
        /// hitstop also freezes time, but its unscaled countdown must keep ticking.
        /// </summary>
        public static bool IsPaused
        {
            get
            {
                for (int i = 0; i < entries.Count; i++)
                    if (entries[i].Reason == ControlReason.Pause) return true;
                return false;
            }
        }

        /// <summary>True while ANY reason that must stop time is held (Pause or Hitstop).</summary>
        public static bool FreezesTime
        {
            get
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    ControlReason reason = entries[i].Reason;
                    if (reason == ControlReason.Pause || reason == ControlReason.Hitstop) return true;
                }
                return false;
            }
        }

        /// <summary>Scale actually applied to <see cref="Time.timeScale"/> (read-only seam).</summary>
        public static float EffectiveScale => effectiveScale;

        /// <summary>
        /// Pushes a reason and reapplies. <see cref="ControlReason.Cinematic"/> never
        /// changes time by itself; it is only recorded in the stack.
        /// </summary>
        public static Handle Acquire(ControlReason reason)
        {
            var handle = new Handle(reason, nextSeq++);
            entries.Add(new Entry { Handle = handle, Reason = reason, Seq = handle.Seq });
            Apply();
            return handle;
        }

        /// <summary>
        /// Removes exactly <paramref name="handle"/> (any position, safe to call twice).
        /// Releasing a hitstop under a pause therefore keeps time frozen.
        /// </summary>
        public static void Release(Handle handle)
        {
            if (handle == null) return;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Handle != handle) continue;
                entries.RemoveAt(i);
                Apply();
                return;
            }
        }

        /// <summary>
        /// Sets the scale used while time is NOT frozen (debug F6 / future slow-mo).
        /// Hitstop releases restore THIS value instead of hardcoding 1.
        /// </summary>
        public static void SetBaseTimeScale(float scale)
        {
            baseScale = Mathf.Max(0f, scale);
            Apply();
        }

        /// <summary>Drops every handle, restores base 1 and applies. Used on flow boot/restart.</summary>
        public static void Reset()
        {
            entries.Clear();
            nextSeq = 0;
            baseScale = 1f;
            Apply();
        }

        /// <summary>
        /// Recomputes and writes the scale. The ONLY <c>Time.timeScale</c> write in the
        /// rebuild — every other system acquires/releases or sets the base instead.
        /// </summary>
        private static void Apply()
        {
            effectiveScale = FreezesTime ? 0f : baseScale;
            Time.timeScale = effectiveScale;
        }

        /// <summary>
        /// Safety net for "Enter Play Mode Options" with domain reload disabled: static
        /// state survives, so a session that ended frozen must not boot frozen.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnLoad()
        {
            Reset();
        }
    }
}
