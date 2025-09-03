// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;

namespace FluffyUnderware.Curvy.Controllers
{
    /// <summary>
    /// Extension methods for <see cref="MovementDirection"/>
    /// </summary>
    public static class MovementDirectionMethods
    {
        /// <summary>
        /// Converts the int to a direction. Positive int means Forward, negative means backward.
        /// </summary>
        public static MovementDirection FromInt(int value)
            => value >= 0
                ? MovementDirection.Forward
                : MovementDirection.Backward;

        /// <summary>
        /// Returns the opposite value of the given direction value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static MovementDirection GetOpposite(this MovementDirection value)
        {
            MovementDirection result;
            switch (value)
            {
                case MovementDirection.Forward:
                    result = MovementDirection.Backward;
                    break;
                case MovementDirection.Backward:
                    result = MovementDirection.Forward;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result;
        }

        /// <summary>
        /// Converts the direction to an int. Positive int means Forward, negative means backward.
        /// </summary>
        public static int ToInt(this MovementDirection direction)
        {
            int result;
            switch (direction)
            {
                case MovementDirection.Forward:
                    result = 1;
                    break;
                case MovementDirection.Backward:
                    result = -1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return result;
        }
    }
}