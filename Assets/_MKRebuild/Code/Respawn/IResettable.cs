namespace MK.Rebuild
{
    /// <summary>
    /// m2-respawn slice 2: the future-participant seam for section reset. Anything that
    /// owns runtime state the section must restore on death (hazards, levers, collectibles,
    /// spawned props) implements this and is cached by <see cref="SectionReset"/>, which
    /// calls <see cref="ResetToInitial"/> on every death.
    ///
    /// Runtime-only (MK.Rebuild.Runtime). Nothing implements it yet — the dummies are
    /// restored directly by <see cref="SectionReset"/>, and enemies get their own stub
    /// (<c>ResetEnemies</c>) until M3.
    /// </summary>
    public interface IResettable
    {
        /// <summary>Restores this participant to its initial (scene-load) state.</summary>
        void ResetToInitial();
    }
}
