namespace MK.Rebuild
{
    /// <summary>
    /// Everything an <see cref="ICinematicAction"/> may touch while a beat runs.
    /// Built ONCE per playback by <see cref="CinematicDirector"/> and handed to every
    /// event, so actions never reach for singletons themselves. Any member may be
    /// null when the scene does not provide it; actions must null-check.
    /// </summary>
    public readonly struct CinematicContext
    {
        public CinematicContext(
            CinematicDefinition definition,
            GameFlowController flow,
            PlayerMotor player,
            CameraDirector cameras)
        {
            Definition = definition;
            Flow = flow;
            Player = player;
            Cameras = cameras;
        }

        /// <summary>The definition currently playing.</summary>
        public CinematicDefinition Definition { get; }

        /// <summary>Flow controller, or null when this scene has none.</summary>
        public GameFlowController Flow { get; }

        /// <summary>Player motor (body owner), or null.</summary>
        public PlayerMotor Player { get; }

        /// <summary>Camera arbiter, or null.</summary>
        public CameraDirector Cameras { get; }
    }
}
