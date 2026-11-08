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

        // LEGACY (V1) — continuation matching by live buffer + direction.
        /// <summary>
        /// Resolves the continuation of <paramref name="step"/> matching a LIVE buffered
        /// input AND the buffered (facing-relative) direction: among the scalar
        /// <c>siguiente</c> and the extra <c>branches</c>, picks the link whose input has
        /// a buffer and whose direction is EXACT or <see cref="AttackDirection.Any"/>
        /// (wildcard). Exact beats wildcard; ties keep the first (siguiente, then the
        /// branches in list order). Links with <c>paso &lt; 0</c> (finisher) are skipped,
        /// as is a non-attack input (no press edge in this slice). False when nothing
        /// matches.
        /// </summary>
        public static bool TryResolveContinuation(
            ComboStep step, float lightBuffer, float heavyBuffer, AttackDirection bufferedDirection,
            out ComboLink best)
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
                if (link.paso < 0) return; // finisher: not an in-combo continuation
                bool live = link.input == ComboInput.Ligero ? lightBuffer > 0f
                          : link.input == ComboInput.Pesado ? heavyBuffer > 0f
                          : false; // H1/H2/Launcher/Dash: no press edge in this slice
                if (!live) return;

                bool exact = link.direction != AttackDirection.Any && link.direction == bufferedDirection;
                bool wildcard = link.direction == AttackDirection.Any;
                if (!exact && !wildcard) return;

                if (!found || (exact && !bestExact)) // exact beats wildcard; else first wins
                {
                    result = link;
                    found = true;
                    bestExact = exact;
                }
            }
        }
    }
}
