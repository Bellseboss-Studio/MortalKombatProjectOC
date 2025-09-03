// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Extension methods of <see cref="ConnectionHeadingEnum"/>
    /// </summary>
    public static class ConnectionHeadingEnumMethods
    {
        /// <summary>
        /// If heading is Auto, this method will translate it to a Plus, Minus or Sharp value depending on the Follow-Up control point.
        /// </summary>
        /// <param name="heading">the value to resolve</param>
        /// <param name="followUp">the related followUp control point</param>
        /// <returns></returns>
        public static ConnectionHeadingEnum ResolveAuto(this ConnectionHeadingEnum heading, CurvySplineSegment followUp)
        {
            if (heading == ConnectionHeadingEnum.Auto)
            {
                if (CurvySplineSegment.CanFollowUpHeadToEnd(followUp))
                    heading = ConnectionHeadingEnum.Plus;
                else if (CurvySplineSegment.CanFollowUpHeadToStart(followUp))
                    heading = ConnectionHeadingEnum.Minus;
                else
                    heading = ConnectionHeadingEnum.Sharp;
            }

            return heading;
        }
    }
}