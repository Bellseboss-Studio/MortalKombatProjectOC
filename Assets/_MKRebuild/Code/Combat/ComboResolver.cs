namespace MK.Rebuild
{
    /// <summary>
    /// COMBO RESOLVER — LEGACY (V1) path (T0.3 extraction).
    ///
    /// Root selection and continuation matching moved 1:1 out of
    /// <see cref="PlayerCombat"/> (no semantic signature change, no behavior
    /// change). This is the pre-grammar chain resolution: index/`siguiente`/
    /// `branches` based. It stays frozen as the V1 fallback — the
    /// `combo-grammar` change never deletes it (design: "Qué se borra: nada").
    ///
    /// Pure functions: the controller passes the data it owns (<c>combos</c>)
    /// and the live buffers, so no state is duplicated and inspector edits
    /// during Play Mode keep being read exactly as before.
    /// </summary>
    public static class ComboResolver
    {
        // LEGACY (V1) — root resolution by input + direction + longest chain.
        /// <summary>
        /// Root resolution (input-prefix + directional variant rule, multi-combo):
        /// among <paramref name="combos"/> whose first step input matches
        /// <paramref name="pressed"/> AND whose step-0 direction is
        /// <paramref name="direction"/> (exact) OR <see cref="AttackDirection.Any"/>
        /// (wildcard), pick the LONGEST chain so a lone press still ends cleanly at
        /// step 0 while a continuation chains. An EXACT direction always beats a
        /// wildcard; ties (same exactness, same length) keep the first in the array.
        /// Null when nothing matches.
        /// </summary>
        public static ComboDefinition SelectRoot(
            ComboDefinition[] combos, ComboInput pressed, AttackDirection direction)
        {
            ComboDefinition best = null;
            int bestLen = -1;
            bool bestExact = false;
            if (combos == null) return null;
            for (int i = 0; i < combos.Length; i++)
            {
                ComboDefinition c = combos[i];
                if (c == null || !c.TryGetStep(0, out ComboStep s0)) continue;
                if (s0.input != pressed) continue;

                bool exact = s0.direction == direction;
                bool wildcard = s0.direction == AttackDirection.Any;
                if (!exact && !wildcard) continue;

                bool better = best == null
                    || (exact && !bestExact)                       // exact beats wildcard
                    || (exact == bestExact && c.Length > bestLen); // then longest chain
                if (better) { best = c; bestLen = c.Length; bestExact = exact; }
            }
            return best;
        }

        // GRAMMAR (combo-grammar-neutral) — continuation matching by the PRESS itself.
        /// <summary>
        /// Continuation matching keyed by the PRESS instead of the legacy buffers
        /// (combo-grammar-neutral): among the scalar <c>siguiente</c> and the extra
        /// <c>branches</c>, returns the link whose <c>input</c> IS
        /// <paramref name="input"/> and whose <c>direction</c> is EXACT or
        /// <see cref="AttackDirection.Any"/> (wildcard). Exact beats wildcard; ties keep
        /// the first (<c>siguiente</c>, then the branches in list order). Links with
        /// <c>paso &lt; 0</c> are skipped, so a leaf is never a destination — neither an
        /// advance to -1 nor a forced close. Neutral is <c>1</c>, so a link authored with
        /// <c>Any</c> still matches a neutral press (that is why the wildcard matters
        /// here and not <c>Any</c> being "no stick").
        ///
        /// PURE MATCHER: it knows nothing about the chain memory, the slot budget or the
        /// running step (<c>Equals(current)</c>, <c>HasUsed</c>, <c>TryGetStep</c>). All
        /// of that policy lives in the CALLER's guard, so the resolver can never mutate
        /// state nor decide that a self-link/used target is acceptable. False when no
        /// live link matches.
        /// </summary>
        public static bool TryResolveContinuation(
            in ComboStep step, ComboInput input, AttackDirection direction, out ComboLink best)
        {
            ComboLink result = default;
            bool found = false;
            bool bestExact = false;

            Consider(step.siguiente);
            if (step.branches != null)
            {
                for (int i = 0; i < step.branches.Count; i++) Consider(step.branches[i]);
            }

            best = result;
            return found;

            void Consider(ComboLink link)
            {
                if (link.paso < 0) return; // hoja / finisher: nunca es destino (R3)
                if (link.input != input) return; // el press trae SU input: un input por intento

                bool exact = link.direction != AttackDirection.Any && link.direction == direction;
                bool wildcard = link.direction == AttackDirection.Any;
                if (!exact && !wildcard) return;

                if (!found || (exact && !bestExact)) // exacta gana a wildcard; si no, gana la primera
                {
                    result = link;
                    found = true;
                    bestExact = exact;
                }
            }
        }

        // LEGACY (V1) — continuation matching by live buffer + direction.
        /// <summary>
        /// Resolves the continuation of <paramref name="step"/> matching a LIVE buffered
        /// input AND the buffered (facing-relative) direction. Delegates the matching to
        /// the press overload (one implementation, one precedence), using the buffers
        /// only as "is this input pressed" flags: Ligero probes with
        /// <paramref name="lightBuffer"/>, Pesado with <paramref name="heavyBuffer"/>,
        /// any other input has no press edge in this slice. The probe order cannot change
        /// V1: every press zeroes the other buffer, so the two are never live at once.
        /// </summary>
        public static bool TryResolveContinuation(
            ComboStep step, float lightBuffer, float heavyBuffer, AttackDirection bufferedDirection,
            out ComboLink best)
        {
            best = default;
            if (lightBuffer > 0f
                && TryResolveContinuation(in step, ComboInput.Ligero, bufferedDirection, out best)) return true;
            if (heavyBuffer > 0f
                && TryResolveContinuation(in step, ComboInput.Pesado, bufferedDirection, out best)) return true;
            return false;
        }
    }
}
