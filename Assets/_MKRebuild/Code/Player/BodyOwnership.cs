using System.Collections.Generic;

namespace MK.Rebuild
{
    /// <summary>
    /// Body-control priority. Higher wins; the lever rank exists for the future lever
    /// but is unused in M2. The OR of every held token replaces the old ownerless
    /// <c>PlayerMotor.InputLocked</c> bool.
    /// </summary>
    public enum BodyPriority
    {
        Lever = 0,
        Attack = 1,
        Dash = 2,
        HitRecovery = 3,
        Death = 4,
        Knockback = 5,
    }

    /// <summary>
    /// Minimal prioritized arbiter for body control (m2-hazards S2).
    /// Plain class (NOT a MonoBehaviour): <see cref="PlayerMotor"/> creates one in
    /// <c>Awake</c> and exposes it as <see cref="PlayerMotor.Body"/>, so no component,
    /// prefab or scene change is needed.
    ///
    /// Every system acquires its OWN token and releases that SAME token. The body is
    /// locked while ANY token is held; the owner is the highest-priority held token
    /// (ties resolve to the earliest acquirer). Releasing a lower-priority token can
    /// never clobber a higher-priority lock — that is the W1 fix.
    /// </summary>
    public sealed class BodyOwnership
    {
        /// <summary>Opaque handle returned by <see cref="Acquire"/>; release it exactly once.</summary>
        public sealed class Token
        {
            internal Token(BodyPriority priority, int seq)
            {
                Priority = priority;
                Seq = seq;
            }

            /// <summary>Priority the holder acquired at.</summary>
            public BodyPriority Priority { get; }

            /// <summary>Acquisition order (lower = earlier); breaks equal-priority ties.</summary>
            public int Seq { get; }
        }

        private readonly List<Token> held = new List<Token>();
        private Token owner;
        private int nextSeq;

        /// <summary>Highest-priority held token's priority, or null when the body is free.</summary>
        public BodyPriority? Owner => owner?.Priority;

        /// <summary>True while ANY token is held (the derived <c>InputLocked</c>).</summary>
        public bool IsHeld => owner != null;

        /// <summary>
        /// True when <paramref name="priority"/> may take control now: the body is free,
        /// or the current owner does not outrank it (equal priority is allowed).
        /// </summary>
        public bool CanAcquire(BodyPriority priority)
        {
            return owner == null || owner.Priority <= priority;
        }

        /// <summary>
        /// Takes a token at <paramref name="priority"/>. Always succeeds: a lower-priority
        /// token may be held while a higher one owns, and it becomes owner when the higher
        /// releases. The caller gates entry with <see cref="CanAcquire"/>.
        /// </summary>
        public Token Acquire(BodyPriority priority)
        {
            var token = new Token(priority, nextSeq++);
            held.Add(token);
            Recompute();
            return token;
        }

        /// <summary>Releases <paramref name="token"/>. Null-safe and idempotent.</summary>
        public void Release(Token token)
        {
            if (token == null) return;
            if (!held.Remove(token)) return;
            Recompute();
        }

        /// <summary>Highest priority wins; on a tie the earliest acquisition wins.</summary>
        private void Recompute()
        {
            Token best = null;
            for (int i = 0; i < held.Count; i++)
            {
                Token candidate = held[i];
                if (best == null
                    || candidate.Priority > best.Priority
                    || (candidate.Priority == best.Priority && candidate.Seq < best.Seq))
                {
                    best = candidate;
                }
            }
            owner = best;
        }
    }
}
