// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System.Runtime.CompilerServices;
using FluffyUnderware.Curvy.Utils;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Static methods used in the implementation of B-Splines
    /// </summary>
    public static class BSplineHelper
    {
        /// <summary>
        /// De Boor algorithm for clamped B-Splines. parameter names taken from https://pages.mtu.edu/~shene/COURSES/cs3621/NOTES/spline/B-spline/de-Boor.html.
        /// This is a variant of that implementation, explained in the english De Boor's page in Wikipedia 
        /// </summary>
        public static Vector3 DeBoorClamped(int p, int k, float u, int nPlus1, [NotNull] Vector3[] pArray)
        {
            //OPTIM make a per degree non recursive implementation for degree 2 and maybe other lower degrees
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(pArray.Length >= p + 1);
#endif
            int kMinusP = k - p;
            int maxClampedKnotValue = nPlus1 - p; // n + 1 - p  is the max knot value

            for (int r = 1; r <= p; r++)
            {
                int kPlusOneMinusR = (k + 1) - r;

                for (int j = p; j >= r; j--)
                {
                    int i1 = j + kMinusP;
                    int i2 = j + kPlusOneMinusR;

                    int u1;
                    int u2;
                    /*The following code is equivalent to the commented code underneath, but using the fact that i1 is smaller than i2
                    u1 = i1 <= p
                        ? 0
                        : i1 >= nPlus1 // n+1 is m-p 
                            ? maxClampedKnotValue // n - p + 1 is the max knot value
                            : i1 - p;
                    u2 = i2 <= p
                        ? 0
                        : i2 >= nPlus1 // n+1 is m-p 
                            ? maxClampedKnotValue // n - p + 1 is the max knot value
                            : i2 - p;
                    */
                    if (i1 <= p)
                    {
                        u1 = 0;

                        if (i2 <= p)
                            u2 = 0;
                        else
                            u2 = i2 >= nPlus1 // n+1 is m-p 
                                ? maxClampedKnotValue
                                : i2 - p;
                    }
                    else if (i1 >= nPlus1)
                        u1 = u2 = maxClampedKnotValue;
                    else
                    {
                        u1 = i1 - p;
                        u2 = i2 >= nPlus1 // n+1 is m-p 
                            ? maxClampedKnotValue
                            : i2 - p;
                    }

                    float a = (u - u1) / (u2 - u1);
                    //equivalent to psArray[j] = (1 - a) * psArray[j - 1] + a * psArray[j];
                    pArray[j] = pArray[j - 1].Multiply(1 - a)
                        .Addition(pArray[j].Multiply(a));
                }
            }

            return pArray[p];
        }

        /// <summary>
        /// De Boor algorithm for unclamped B-Splines. parameter names taken from https://pages.mtu.edu/~shene/COURSES/cs3621/NOTES/spline/B-spline/de-Boor.html.
        /// This is a variant of that implementation, explained in the english De Boor's page in Wikipedia 
        /// </summary>
        public static Vector3 DeBoorUnclamped(int p, int k, float u, [NotNull] Vector3[] pArray)
        {
            //OPTIM make a per degree non recursive implementation for degree 2 and maybe other lower degrees

#if CURVY_SANITY_CHECKS
            Assert.IsTrue(pArray.Length >= p + 1);
#endif
            int kMinusP = k - p;

            for (int r = 1; r <= p; r++)
            {
                int kPlusOneMinusR = (k + 1) - r;

                for (int j = p; j >= r; j--)
                {
                    float a = (u - (j + kMinusP)) / ((j + kPlusOneMinusR) - (j + kMinusP));
                    //equivalent to psArray[j] = (1 - a) * psArray[j - 1] + a * psArray[j];
                    pArray[j] = pArray[j - 1].Multiply(1 - a)
                        .Addition(pArray[j].Multiply(a));
                }
            }

            return pArray[p];
        }

        /// <summary>
        /// Get the N number as defined in the B-Spline section here: https://pages.mtu.edu/~shene/COURSES/cs3621/NOTES/
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBSplineN(int controlPointsCount, int degree, bool closed)
            => (controlPointsCount
                - 1)
               + (closed
                   ? degree
                   : 0);

        /// <summary>
        /// Get the the U and K numbers as defined in the B-Spline section, De Boor's algorithm, here: https://pages.mtu.edu/~shene/COURSES/cs3621/NOTES/
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetBSplineUAndK(float tf, bool isClamped, int p, int n, out float u, out int k)
        {
#if CURVY_SANITY_CHECKS
            Assert.IsTrue(tf.IsBetween0And1());
#endif
            if (isClamped)
            {
                u = ((n - p) + 1) * tf; // n - p + 1 is the max knot value
                int intU = (int)u;
                //case of u equal to 1
                if (intU == (n - p) + 1) // n - p + 1 is the max knot value
                    intU--;
                k = intU + p;
            }
            else
            {
                u = p + (((n + 1) - p) * tf); //inlined version of DTMath.MapValue(p, m - p, tf, 0, 1), knowing that n+1 is m-p
                int intU = (int)u;
                //case of u equal to 1
                if (intU == n + 1) // n+1 is m-p
                    intU--;
                k = intU;
            }
        }
    }
}