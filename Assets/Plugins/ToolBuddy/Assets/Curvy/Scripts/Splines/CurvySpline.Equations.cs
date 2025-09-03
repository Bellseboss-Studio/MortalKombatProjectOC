// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEngine;

namespace FluffyUnderware.Curvy
{
    public partial class CurvySpline
    {
        /// <summary>
        /// Cubic-Beziere Interpolation
        /// </summary>
        /// <param name="T0">HandleIn</param>
        /// <param name="P0">Pn</param>
        /// <param name="P1">Pn+1</param>
        /// <param name="T1">HandleOut</param>
        /// <param name="f">f in the range 0..1</param>
        /// <returns></returns>
        public static Vector3 Bezier(
            Vector3 T0,
            Vector3 P0,
            Vector3 P1,
            Vector3 T1,
            float f)
        {
            //If you modify this, modify also the inlined version of this method in refreshCurveINTERNAL()

            const double Ft2 = 3;
            const double Ft3 = -3;
            const double Fu1 = 3;
            const double Fu2 = -6;
            const double Fu3 = 3;
            const double Fv1 = -3;
            const double Fv2 = 3;

            double FAX = -P0.x + (Ft2 * T0.x) + (Ft3 * T1.x) + P1.x;
            double FBX = (Fu1 * P0.x) + (Fu2 * T0.x) + (Fu3 * T1.x);
            double FCX = (Fv1 * P0.x) + (Fv2 * T0.x);
            double FDX = P0.x;

            double FAY = -P0.y + (Ft2 * T0.y) + (Ft3 * T1.y) + P1.y;
            double FBY = (Fu1 * P0.y) + (Fu2 * T0.y) + (Fu3 * T1.y);
            double FCY = (Fv1 * P0.y) + (Fv2 * T0.y);
            double FDY = P0.y;

            double FAZ = -P0.z + (Ft2 * T0.z) + (Ft3 * T1.z) + P1.z;
            double FBZ = (Fu1 * P0.z) + (Fu2 * T0.z) + (Fu3 * T1.z);
            double FCZ = (Fv1 * P0.z) + (Fv2 * T0.z);
            double FDZ = P0.z;

            float FX = (float)((((((FAX * f) + FBX) * f) + FCX) * f) + FDX);
            float FY = (float)((((((FAY * f) + FBY) * f) + FCY) * f) + FDY);
            float FZ = (float)((((((FAZ * f) + FBZ) * f) + FCZ) * f) + FDZ);

            Vector3 result;
            result.x = FX;
            result.y = FY;
            result.z = FZ;
            return result;
        }

        /// <summary>
        /// Cubic-Beziere Interpolation
        /// </summary>
        /// <param name="T0">HandleIn</param>
        /// <param name="P0">Pn</param>
        /// <param name="P1">Pn+1</param>
        /// <param name="T1">HandleOut</param>
        /// <param name="t">f in the range 0..1</param>
        /// <returns></returns>
        public static Vector3 BezierTangent(
            Vector3 T0,
            Vector3 P0,
            Vector3 P1,
            Vector3 T1,
            float t)
        {
            //If you modify this, modify also the inlined version of this method in ApproximationsSetter.SetTangentsToBezier()

            Vector3 result;

            if (Mathf.Approximately(
                    t,
                    0f
                )
                && T0 == P0)
            {
                result.x = T1.x - P0.x;
                result.y = T1.y - P0.y;
                result.z = T1.z - P0.z;
            }
            else if (Mathf.Approximately(
                         t,
                         1f
                     )
                     && T1 == P1)
            {
                result.x = P1.x - T0.x;
                result.y = P1.y - T0.y;
                result.z = P1.z - T0.z;
            }
            else
            {
                float mt = 1f - t;
                float mt2 = mt * mt;
                float t2 = t * t;

                result.x = (3f * mt2 * (T0.x - P0.x))
                           + (6f * mt * t * (T1.x - T0.x))
                           + (3f * t2 * (P1.x - T1.x));
                result.y = (3f * mt2 * (T0.y - P0.y))
                           + (6f * mt * t * (T1.y - T0.y))
                           + (3f * t2 * (P1.y - T1.y));
                result.z = (3f * mt2 * (T0.z - P0.z))
                           + (6f * mt * t * (T1.z - T0.z))
                           + (3f * t2 * (P1.z - T1.z));
            }

            return result;
        }

        /// <summary>
        /// Catmull-Rom Interpolation
        /// </summary>
        /// <param name="T0">Pn-1 (In Tangent)</param>
        /// <param name="P0">Pn</param>
        /// <param name="P1">Pn+1</param>
        /// <param name="T1">Pn+2 (Out Tangent)</param>
        /// <param name="f">f in the range 0..1</param>
        /// <returns>the interpolated position</returns>
        public static Vector3 CatmullRom(
            Vector3 T0,
            Vector3 P0,
            Vector3 P1,
            Vector3 T1,
            float f)
        {
            //If you modify this, modify also the inlined version of this method in refreshCurveINTERNAL()

            const double Ft1 = -0.5;
            const double Ft2 = 1.5;
            const double Ft3 = -1.5;
            const double Ft4 = 0.5;
            const double Fu2 = -2.5;
            const double Fu3 = 2;
            const double Fu4 = -0.5;
            const double Fv1 = -0.5;
            const double Fv3 = 0.5;

            double FAX = (Ft1 * T0.x) + (Ft2 * P0.x) + (Ft3 * P1.x) + (Ft4 * T1.x);
            double FBX = T0.x + (Fu2 * P0.x) + (Fu3 * P1.x) + (Fu4 * T1.x);
            double FCX = (Fv1 * T0.x) + (Fv3 * P1.x);
            double FDX = P0.x;

            double FAY = (Ft1 * T0.y) + (Ft2 * P0.y) + (Ft3 * P1.y) + (Ft4 * T1.y);
            double FBY = T0.y + (Fu2 * P0.y) + (Fu3 * P1.y) + (Fu4 * T1.y);
            double FCY = (Fv1 * T0.y) + (Fv3 * P1.y);
            double FDY = P0.y;

            double FAZ = (Ft1 * T0.z) + (Ft2 * P0.z) + (Ft3 * P1.z) + (Ft4 * T1.z);
            double FBZ = T0.z + (Fu2 * P0.z) + (Fu3 * P1.z) + (Fu4 * T1.z);
            double FCZ = (Fv1 * T0.z) + (Fv3 * P1.z);
            double FDZ = P0.z;

            float FX = (float)((((((FAX * f) + FBX) * f) + FCX) * f) + FDX);
            float FY = (float)((((((FAY * f) + FBY) * f) + FCY) * f) + FDY);
            float FZ = (float)((((((FAZ * f) + FBZ) * f) + FCZ) * f) + FDZ);

            Vector3 result;
            result.x = FX;
            result.y = FY;
            result.z = FZ;
            return result;
        }

        /// <summary>
        /// Catmull-Rom tangent
        /// </summary>
        /// <param name="T0">Pn-1 (In Tangent)</param>
        /// <param name="P0">Pn</param>
        /// <param name="P1">Pn+1</param>
        /// <param name="T1">Pn+2 (Out Tangent)</param>
        /// <param name="t">f in the range 0..1</param>
        /// <returns></returns>
        public static Vector3 CatmullRomTangent(
            Vector3 T0,
            Vector3 P0,
            Vector3 P1,
            Vector3 T1,
            float t)
        {
            //If you modify this, modify also the inlined version of this method in ApproximationsSetter.SetSegmentPreciseTangents()

            // Unpack vector components for T0, P0, P1, T1
            float T0x = T0.x, T0y = T0.y, T0z = T0.z;
            float P0x = P0.x, P0y = P0.y, P0z = P0.z;
            float P1x = P1.x, P1y = P1.y, P1z = P1.z;
            float T1x = T1.x, T1y = T1.y, T1z = T1.z;

            float t2 = t * t;

            // a0 = 0.5f * (-T0 + P1)
            float a0x = 0.5f * (-T0x + P1x);
            float a0y = 0.5f * (-T0y + P1y);
            float a0z = 0.5f * (-T0z + P1z);

            // a1 = 0.5f * (2f*T0 - 5f*P0 + 4f*P1 - T1)
            float a1x = 0.5f * ((2f * T0x) - (5f * P0x) + (4f * P1x) - T1x);
            float a1y = 0.5f * ((2f * T0y) - (5f * P0y) + (4f * P1y) - T1y);
            float a1z = 0.5f * ((2f * T0z) - (5f * P0z) + (4f * P1z) - T1z);

            // a2 = 0.5f * (-T0 + 3f*P0 - 3f*P1 + T1)
            float a2x = 0.5f * (-T0x + (3f * P0x) - (3f * P1x) + T1x);
            float a2y = 0.5f * (-T0y + (3f * P0y) - (3f * P1y) + T1y);
            float a2z = 0.5f * (-T0z + (3f * P0z) - (3f * P1z) + T1z);

            // Derivative = a0 + (2f * a1 * f) + (3f * a2 * t2)
            Vector3 result;
            result.x = a0x + 2f * a1x * t + 3f * a2x * t2;
            result.y = a0y + 2f * a1y * t + 3f * a2y * t2;
            result.z = a0z + 2f * a1z * t + 3f * a2z * t2;
            return result;
        }

        /// <summary>
        /// Kochanek-Bartels/TCB-Interpolation
        /// </summary>
        /// <param name="T0">Pn-1 (In Tangent)</param>
        /// <param name="P0">Pn</param>
        /// <param name="P1">Pn+1</param>
        /// <param name="T1">Pn+2 (Out Tangent)</param>
        /// <param name="f">f in the range 0..1</param>
        /// <param name="FT0">Start Tension</param>
        /// <param name="FC0">Start Continuity</param>
        /// <param name="FB0">Start Bias</param>
        /// <param name="FT1">End Tension</param>
        /// <param name="FC1">End Continuity</param>
        /// <param name="FB1">End Bias</param>
        /// <returns>the interpolated position</returns>
        public static Vector3 TCB(
            Vector3 T0,
            Vector3 P0,
            Vector3 P1,
            Vector3 T1,
            float f,
            float FT0,
            float FC0,
            float FB0,
            float FT1,
            float FC1,
            float FB1)
        {
            //If you modify this, modify also the inlined version of this method in refreshCurveINTERNAL()

            double FFA = (1 - FT0) * (1 + FC0) * (1 + FB0);
            double FFB = (1 - FT0) * (1 - FC0) * (1 - FB0);
            double FFC = (1 - FT1) * (1 - FC1) * (1 + FB1);
            double FFD = (1 - FT1) * (1 + FC1) * (1 - FB1);

            double DD = 2;
            double Ft1 = -FFA / DD;
            double Ft2 = ((+4 + FFA) - FFB - FFC) / DD;
            double Ft3 = ((-4 + FFB + FFC) - FFD) / DD;
            double Ft4 = FFD / DD;
            double Fu1 = (+2 * FFA) / DD;
            double Fu2 = ((-6 - (2 * FFA)) + (2 * FFB) + FFC) / DD;
            double Fu3 = ((+6 - (2 * FFB) - FFC) + FFD) / DD;
            double Fu4 = -FFD / DD;
            double Fv1 = -FFA / DD;
            double Fv2 = (FFA - FFB) / DD;
            double Fv3 = FFB / DD;
            double Fw2 = +2 / DD;

            double FAX = (Ft1 * T0.x) + (Ft2 * P0.x) + (Ft3 * P1.x) + (Ft4 * T1.x);
            double FBX = (Fu1 * T0.x) + (Fu2 * P0.x) + (Fu3 * P1.x) + (Fu4 * T1.x);
            double FCX = (Fv1 * T0.x) + (Fv2 * P0.x) + (Fv3 * P1.x);
            double FDX = Fw2 * P0.x;

            double FAY = (Ft1 * T0.y) + (Ft2 * P0.y) + (Ft3 * P1.y) + (Ft4 * T1.y);
            double FBY = (Fu1 * T0.y) + (Fu2 * P0.y) + (Fu3 * P1.y) + (Fu4 * T1.y);
            double FCY = (Fv1 * T0.y) + (Fv2 * P0.y) + (Fv3 * P1.y);
            double FDY = Fw2 * P0.y;

            double FAZ = (Ft1 * T0.z) + (Ft2 * P0.z) + (Ft3 * P1.z) + (Ft4 * T1.z);
            double FBZ = (Fu1 * T0.z) + (Fu2 * P0.z) + (Fu3 * P1.z) + (Fu4 * T1.z);
            double FCZ = (Fv1 * T0.z) + (Fv2 * P0.z) + (Fv3 * P1.z);
            double FDZ = Fw2 * P0.z;

            float FX = (float)((((((FAX * f) + FBX) * f) + FCX) * f) + FDX);
            float FY = (float)((((((FAY * f) + FBY) * f) + FCY) * f) + FDY);
            float FZ = (float)((((((FAZ * f) + FBZ) * f) + FCZ) * f) + FDZ);

            Vector3 result;
            result.x = FX;
            result.y = FY;
            result.z = FZ;
            return result;
        }

        /// <summary>
        /// Kochanek–Bartels (TCB) spline tangent (first derivative),
        /// with separate T,C,B at the start point (P1) and end point (P2).
        /// Returns the tangent at parameter t ∈ [0,1].
        /// </summary>
        /// <param name="T0">Pn-1 (In Tangent)</param>
        /// <param name="P0">Pn</param>
        /// <param name="P1">Pn+1</param>
        /// <param name="T1">Pn+2 (Out Tangent)</param>
        /// <param name="t">f in the range 0..1</param>
        /// <param name="FT0">Start Tension</param>
        /// <param name="FC0">Start Continuity</param>
        /// <param name="FB0">Start Bias</param>
        /// <param name="FT1">End Tension</param>
        /// <param name="FC1">End Continuity</param>
        /// <param name="FB1">End Bias</param>
        public static Vector3 TCBTangent(
            Vector3 T0,
            Vector3 P0,
            Vector3 P1,
            Vector3 T1,
            float t,
            float FT0,
            float FC0,
            float FB0,
            float FT1,
            float FC1,
            float FB1)
        {
            //If you modify this, modify also the inlined version of this method in ApproximationsSetter.SetSegmentPreciseTangents()

            // Unpack vector components
            float P0x = P0.x, P0y = P0.y, P0z = P0.z;
            float P1x = P1.x, P1y = P1.y, P1z = P1.z;

            // (P1 - P0)
            float diffP1P0x = P1x - P0x;
            float diffP1P0y = P1y - P0y;
            float diffP1P0z = P1z - P0z;

            // Factors for M1
            float factorM1 = (1f - FT0);
            float s1 = (1f + FC0) * (1f + FB0);
            float s2 = (1f - FC0) * (1f - FB0);

            // M1 = 0.5f * factorM1 * s1*(P0 - T0) + s2*(P1 - P0)
            float M1x = 0.5f * factorM1 * (((P0x - T0.x) * s1) + (diffP1P0x * s2));
            float M1y = 0.5f * factorM1 * (((P0y - T0.y) * s1) + (diffP1P0y * s2));
            float M1z = 0.5f * factorM1 * (((P0z - T0.z) * s1) + (diffP1P0z * s2));

            // Factors for M2
            float factorM2 = (1f - FT1);
            float s3 = (1f - FC1) * (1f + FB1);
            float s4 = (1f + FC1) * (1f - FB1);

            // M2 = 0.5f * factorM2 * s3*(P1 - P0) + s4*(T1 - P1)
            float M2x = 0.5f * factorM2 * ((diffP1P0x * s3) + ((T1.x - P1x) * s4));
            float M2y = 0.5f * factorM2 * ((diffP1P0y * s3) + ((T1.y - P1y) * s4));
            float M2z = 0.5f * factorM2 * ((diffP1P0z * s3) + ((T1.z - P1z) * s4));

            // Hermite derivative basis
            float t2 = t * t;
            float d1 = (6f * t2) - (6f * t); // coefficient for P0
            float d2 = ((3f * t2) - (4f * t)) + 1f; // coefficient for M1
            float d3 = (-6f * t2) + (6f * t); // coefficient for P1
            float d4 = (3f * t2) - (2f * t); // coefficient for M2

            Vector3 result;
            result.x = (P0x * d1) + (M1x * d2) + (P1x * d3) + (M2x * d4);
            result.y = (P0y * d1) + (M1y * d2) + (P1y * d3) + (M2y * d4);
            result.z = (P0z * d1) + (M1z * d2) + (P1z * d3) + (M2z * d4);
            return result;
        }
    }
}