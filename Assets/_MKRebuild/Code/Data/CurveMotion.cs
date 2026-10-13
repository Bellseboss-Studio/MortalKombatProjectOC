using UnityEngine;

namespace MK.Rebuild
{
    /// <summary>
    /// Curved motion helper (m1-combos FEEL iteration, 2026-10-13): converts a
    /// NORMALIZED velocity curve (1 -> 0 over the motion) plus a TOTAL DISTANCE
    /// into an instantaneous speed, so the designer only edits the SHAPE and the
    /// DISTANCE — never the peak speed.
    ///
    /// Mean speed = distance / window; and speed(t) = distance * curve(t/window) /
    /// integral, so integrating speed over [0, window] equals the distance EXACTLY
    /// (the integral is sampled by the trapezoid rule). Robust for a 1 -> 0 curve
    /// (the integral is ~0.5, so the peak is ~2x the mean). A null/empty curve
    /// falls back to a constant speed = distance / window (pre-curve behavior).
    /// </summary>
    public static class CurveMotion
    {
        private const int IntegralSamples = 32;
        private const float MinIntegral = 0.0001f;

        /// <summary>
        /// Default ease-out shape (strong impulse, decaying): 1 at t = 0, fading
        /// to 0 at t = 1. Used whenever a curve is empty (asset/scene default).
        /// </summary>
        public static AnimationCurve DefaultEaseOut()
        {
            var c = new AnimationCurve();
            c.AddKey(0f, 1f);
            c.AddKey(0.5f, 0.45f);
            c.AddKey(1f, 0f);
            for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
            return c;
        }

        /// <summary>True when the curve can be sampled (has at least 2 keys).</summary>
        public static bool IsUsable(AnimationCurve curve) => curve != null && curve.length >= 2;

        /// <summary>The curve when usable, else the default ease-out.</summary>
        public static AnimationCurve OrDefault(AnimationCurve curve)
            => IsUsable(curve) ? curve : DefaultEaseOut();

        /// <summary>
        /// Integral of the curve over the normalized domain [0,1], sampled by the
        /// trapezoid rule. Returns 1 for a null/empty curve (constant fallback).
        /// </summary>
        public static float Integral(AnimationCurve curve)
        {
            if (!IsUsable(curve)) return 1f;
            float sum = 0f;
            float prev = curve.Evaluate(0f);
            for (int i = 1; i <= IntegralSamples; i++)
            {
                float u = (float)i / IntegralSamples;
                float cur = curve.Evaluate(u);
                sum += (prev + cur) * 0.5f; // trapezoid
                prev = cur;
            }
            return sum / IntegralSamples;
        }

        /// <summary>
        /// Instantaneous speed at <paramref name="elapsed"/> seconds into a motion
        /// that must advance <paramref name="distance"/> meters over
        /// <paramref name="window"/> seconds. The integral of this speed over the
        /// window equals the distance. A non-positive window returns 0.
        /// </summary>
        public static float Speed(AnimationCurve curve, float distance, float window, float elapsed)
        {
            if (window <= 0f) return 0f;
            if (!IsUsable(curve)) return distance / window; // constant fallback
            float area = Integral(curve);
            if (area < MinIntegral) area = MinIntegral; // guard: zero/negative-area curve
            float u = Mathf.Clamp01(elapsed / window);
            return distance * curve.Evaluate(u) / (window * area);
        }
    }
}
