namespace MK.Rebuild
{
    /// <summary>
    /// A thing a cinematic beat can DO at a moment in time. The definition stores
    /// these as interface-typed references (see <see cref="CinematicEvent"/>), so the
    /// data layer never hard-codes a concrete action class.
    /// </summary>
    public interface ICinematicAction
    {
        /// <summary>
        /// Runs once when the owning event's time is reached. Every field on
        /// <paramref name="context"/> may be null: an action must be defensive.
        /// </summary>
        void Execute(CinematicContext context);
    }
}
