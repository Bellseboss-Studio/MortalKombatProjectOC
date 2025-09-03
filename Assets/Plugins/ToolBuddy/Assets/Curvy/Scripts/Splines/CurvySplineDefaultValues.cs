// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Here you can find all the default values for CurvySpline's serialized fields. If you don't find a field here, this means that it's type's default value is the same than the field's default value, except for <see cref="CurvySpline.Interpolation"/> which default value is user defined, see <see cref="CurvyGlobalManager.DefaultInterpolation"/>
    /// </summary>
    public static class CurvySplineDefaultValues
    {
        public const bool AutoEndTangents = true;
        public const CurvyOrientation Orientation = CurvyOrientation.Dynamic;
        public const float AutoHandleDistance = 0.39f;
        public const int CacheDensity = 50;
        public const float MaxPointsPerUnit = 8;
        public const bool UsePooling = true;
        public const CurvyUpdateMethod UpdateIn = CurvyUpdateMethod.Update;
        public const bool CheckTransform = true;
        public const int BSplineDegree = 2;
        public const bool IsBSplineClamped = true;
    }
}