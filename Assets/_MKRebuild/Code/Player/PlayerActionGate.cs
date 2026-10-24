namespace MK.Rebuild
{
    /// <summary>
    /// Verbs the player can attempt at any moment. The gate below decides which are
    /// LEGAL in the current <see cref="MoveState"/>. Deny by default: a new verb stays
    /// blocked until it is explicitly allowed in <see cref="PlayerActionGate.Allows"/>.
    /// </summary>
    public enum PlayerAction
    {
        Move,
        Jump,
        Light,
        Heavy,
        Dash,
        Skill1,
        Skill2,
        Drop
    }

    /// <summary>
    /// Central action gate (change action-gating): ONE table keyed on the movement
    /// state that every input consumer (<see cref="PlayerCombat"/> / <see cref="PlayerDash"/> /
    /// <see cref="PlayerAbilities"/> and the motor's horizontal input) consults.
    /// DENY BY DEFAULT — an unlisted state, or a verb that is not explicitly allowed,
    /// is blocked, so a new action can never leak through in a state where it has not
    /// been vetted.
    ///
    /// This is DATA-IN-CODE for the prototype (a readable switch). When the matrix
    /// grows, move it to a data-driven table (e.g. a ScriptableObject of
    /// (MoveState, PlayerAction) -> bool) and keep <see cref="Allows"/> as the only
    /// lookup, so consumers never change.
    ///
    /// Capability matrix (frozen, agreed spec):
    /// <code>
    ///   State          Move Jump Light/Heavy Dash Skill1/2 Drop
    ///   Free(ground)    Y    Y       Y        Y      Y       -
    ///   Free(air)       Y    N       N        N      N       -
    ///   WallGrab        N    Y       N        N      N       Y
    ///   WallJump        N    N       N        N      N       N
    ///   LedgeHang       N    Y       N        N      N       Y
    ///   Mantle          N    N       N        N      N       N
    /// </code>
    /// Notes: Move stays allowed in Free regardless of ground/air (air control).
    /// The hang "Jump" is the state's climb verb (wall-jump in WallGrab, mantle/top
    /// in LedgeHang). "Drop" is the intentional let-go, still driven by the motor's
    /// existing Down-release mechanism.
    /// </summary>
    public static class PlayerActionGate
    {
        /// <summary>
        /// True when <paramref name="action"/> is allowed while the character is in
        /// <paramref name="state"/>. <paramref name="grounded"/> subdivides
        /// <see cref="MoveState.Free"/> (combat/mobility on the ground; blocked in the
        /// air). Deny by default.
        /// </summary>
        public static bool Allows(MoveState state, bool grounded, PlayerAction action)
        {
            switch (state)
            {
                case MoveState.Free:
                    switch (action)
                    {
                        // Air control always allowed (walk/run on ground, steer in air).
                        case PlayerAction.Move:
                            return true;

                        // Offensive + mobility verbs require the ground; blocked in the air.
                        case PlayerAction.Jump:
                        case PlayerAction.Light:
                        case PlayerAction.Heavy:
                        case PlayerAction.Dash:
                        case PlayerAction.Skill1:
                        case PlayerAction.Skill2:
                            return grounded;

                        default:
                            return false; // nothing to drop while free
                    }

                // Hangs: only the intentional let-go (Drop) and the state's climb verb
                // (Jump = wall-jump in WallGrab, mantle/top in LedgeHang). Move is denied
                // (no sliding parallel to the lip).
                case MoveState.WallGrab:
                case MoveState.LedgeHang:
                    return action == PlayerAction.Drop || action == PlayerAction.Jump;

                // Scripted flight / animation warp: fully locked.
                case MoveState.WallJump:
                case MoveState.Mantle:
                    return false;

                default:
                    return false; // deny by default
            }
        }
    }
}
