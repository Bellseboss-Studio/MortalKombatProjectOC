namespace MK.Rebuild
{
    /// <summary>
    /// The beats of the unattended demo, in script order. <see cref="GameFlowController"/>
    /// owns the transition table, so this enum stays pure data: an arrow either exists
    /// there or the request is rejected.
    ///
    /// WHY a fixed enum instead of an event graph: the demo is a single linear script with
    /// one branch (an optional mid-demo reveal). A table of nine states is small enough to
    /// audit by eye and impossible to enter out of order without a deliberate debug jump.
    /// </summary>
    public enum FlowState
    {
        /// <summary>Scene loaded, every static/base value reset. Leave immediately for <see cref="Intro"/>; it is the reload anchor.</summary>
        Boot = 0,

        /// <summary>Opening beat: short camera/Timeline intro before the player takes control.</summary>
        Intro,

        /// <summary>Free roam. Player has control, camera zones resolve, HUD visible.</summary>
        Exploration,

        /// <summary>Optional mid-demo reveal fired by a <see cref="FlowTrigger"/> volume; returns to <see cref="Exploration"/>.</summary>
        Beat,

        /// <summary>Encounter entry: the boss spawns and its intro beat plays.</summary>
        BossIntro,

        /// <summary>Phase-1 fight. Boss camera is live; waits for the phase-change signal.</summary>
        BossFight,

        /// <summary>Phase 1 to 2 beat: the boss roars and summons its minions.</summary>
        BossTransition,

        /// <summary>Phase-2 fight. Waits for the boss death signal.</summary>
        BossPhase2,

        /// <summary>Boss defeated: the end beat plays, then the restart-only pause card opens.</summary>
        DemoEnd,
    }
}
