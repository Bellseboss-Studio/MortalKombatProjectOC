// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

using UnityEngine;

namespace FluffyUnderware.DevTools
{
    public static class DTTween
    {
        public enum EasingMethod
        {
            Linear              = 0,
            ExponentialIn       = 1,
            ExponentialOut      = 2,
            ExponentialInOut    = 3,
            ExponentialOutIn    = 4,
            CircularIn          = 5,
            CircularOut         = 6,
            CircularInOut       = 7,
            CircularOutIn       = 8,
            QuadraticIn         = 9,
            QuadraticOut        = 10,
            QuadraticInOut      = 11,
            QuadraticOutIn      = 12,
            SinusIn             = 13,
            SinusOut            = 14,
            SinusInOut          = 15,
            SinusOutIn          = 16,
            CubicIn             = 17,
            CubicOut            = 18,
            CubicInOut          = 19,
            CubicOutIn          = 20,
            QuarticIn           = 21,
            QuarticOut          = 22,
            QuarticInOut        = 23,
            QuarticOutIn        = 24,
            QuinticIn           = 25,
            QuinticOut          = 26,
            QuinticInOut        = 27,
            QuinticOutIn        = 28
        }


        /// <summary>
        /// Tween
        /// </summary>
        /// <param name="method">Tweening method</param>
        /// <param name="t">Time (0..1)</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float Ease(EasingMethod method, float t, float b, float c)
        {
            switch (method)
            {
                case EasingMethod.ExponentialIn: return ExpoIn(t, b, c);
                case EasingMethod.ExponentialOut: return ExpoOut(t, b, c);
                case EasingMethod.ExponentialInOut: return ExpoInOut(t, b, c);
                case EasingMethod.ExponentialOutIn: return ExpoOutIn(t, b, c);
                case EasingMethod.CircularIn: return CircIn(t, b, c);
                case EasingMethod.CircularOut: return CircOut(t, b, c);
                case EasingMethod.CircularInOut: return CircInOut(t, b, c);
                case EasingMethod.CircularOutIn: return CircOutIn(t, b, c);
                case EasingMethod.QuadraticIn: return QuadIn(t, b, c);
                case EasingMethod.QuadraticOut: return QuadOut(t, b, c);
                case EasingMethod.QuadraticInOut: return QuadInOut(t, b, c);
                case EasingMethod.QuadraticOutIn: return QuadOutIn(t, b, c);
                case EasingMethod.SinusIn: return SineIn(t, b, c);
                case EasingMethod.SinusOut: return SineOut(t, b, c);
                case EasingMethod.SinusInOut: return SineInOut(t, b, c);
                case EasingMethod.SinusOutIn: return SineOutIn(t, b, c);
                case EasingMethod.CubicIn: return CubicIn(t, b, c);
                case EasingMethod.CubicOut: return CubicOut(t, b, c);
                case EasingMethod.CubicInOut: return CubicInOut(t, b, c);
                case EasingMethod.CubicOutIn: return CubicOutIn(t, b, c);
                case EasingMethod.QuarticIn: return QuartIn(t, b, c);
                case EasingMethod.QuarticOut: return QuartOut(t, b, c);
                case EasingMethod.QuarticInOut: return QuartInOut(t, b, c);
                case EasingMethod.QuarticOutIn: return QuartOutIn(t, b, c);
                case EasingMethod.QuinticIn: return QuintIn(t, b, c);
                case EasingMethod.QuinticOut: return QuintOut(t, b, c);
                case EasingMethod.QuinticInOut: return QuintInOut(t, b, c);
                case EasingMethod.QuinticOutIn: return QuintOutIn(t, b, c);
                default: return Linear(t, b, c);
            }
        }

        /// <summary>
        /// Tween
        /// </summary>
        /// <param name="method">Tweening method</param>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float Ease(EasingMethod method, float t, float b, float c, float d)
        {
            switch (method)
            {
                case EasingMethod.ExponentialIn: return ExpoIn(t, b, c, d);
                case EasingMethod.ExponentialOut: return ExpoOut(t, b, c, d);
                case EasingMethod.ExponentialInOut : return ExpoInOut(t, b, c, d);
                case EasingMethod.ExponentialOutIn: return ExpoOutIn(t, b, c, d);
                case EasingMethod.CircularIn: return CircIn(t, b, c, d);
                case EasingMethod.CircularOut: return CircOut(t, b, c, d);
                case EasingMethod.CircularInOut: return CircInOut(t, b, c, d);
                case EasingMethod.CircularOutIn: return CircOutIn(t, b, c, d);
                case EasingMethod.QuadraticIn : return QuadIn(t, b, c, d);
                case EasingMethod.QuadraticOut : return QuadOut(t, b, c, d);
                case EasingMethod.QuadraticInOut : return QuadInOut(t, b, c, d);
                case EasingMethod.QuadraticOutIn : return QuadOutIn(t, b, c, d);
                case EasingMethod.SinusIn: return SineIn(t, b, c, d);
                case EasingMethod.SinusOut: return SineOut(t, b, c, d);
                case EasingMethod.SinusInOut : return SineInOut(t, b, c, d);
                case EasingMethod.SinusOutIn  : return SineOutIn(t, b, c, d);
                case EasingMethod.CubicIn: return CubicIn(t, b, c, d);
                case EasingMethod.CubicOut: return CubicOut(t, b, c, d);
                case EasingMethod.CubicInOut: return CubicInOut(t, b, c, d);
                case EasingMethod.CubicOutIn: return CubicOutIn(t, b, c, d);
                case EasingMethod.QuarticIn: return QuartIn(t, b, c, d);
                case EasingMethod.QuarticOut: return QuartOut(t, b, c, d);
                case EasingMethod.QuarticInOut: return QuartInOut(t, b, c, d);
                case EasingMethod.QuarticOutIn: return QuartOutIn(t, b, c, d);
                case EasingMethod.QuinticIn: return QuintIn(t, b, c, d);
                case EasingMethod.QuinticOut: return QuintOut(t, b, c, d);
                case EasingMethod.QuinticInOut: return QuintInOut(t, b, c, d);
                case EasingMethod.QuinticOutIn: return QuintOutIn(t, b, c, d);
                default: return Linear(t, b, c, d);
            }
        }

        #region ### Linear ###
        /// <summary>
        /// Linear tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float Linear(float t, float b, float c)
            => c * Mathf.Clamp01(t) + b;

        /// <summary>
        /// Linear tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float Linear(float t, float b, float c, float d)
            => c * t / d + b;

        #endregion

        #region ### Exponential ###
        /// <summary>
        /// Exponential tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float ExpoOut(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            return (t == 1) ? b + c : c * (-Mathf.Pow(2, -10 * t) + 1) + b;
        }
        /// <summary>
        /// Exponential tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float ExpoOut(float t, float b, float c, float d)
            => (t == d) ? b + c : c * (-Mathf.Pow(2, -10 * t / d) + 1) + b;

        /// <summary>
        /// Exponential tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float ExpoIn(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            return (t == 0) ? b : c * Mathf.Pow(2, 10 * (t-1)) + b;
        }
        /// <summary>
        /// Exponential tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float ExpoIn(float t, float b, float c, float d)
            => (t == 0) ? b : c * Mathf.Pow(2, 10 * (t / d - 1)) + b;

        /// <summary>
        /// Exponential tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float ExpoInOut(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            if (t == 0)
                return b;

            if (t == 1)
                return b + c;

            if ((t /= .5f) < 1)
                return c / 2 * Mathf.Pow(2, 10 * (t - 1)) + b;

            return c / 2 * (-Mathf.Pow(2, -10 * --t) + 2) + b;
        }
        /// <summary>
        /// Exponential tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float ExpoInOut(float t, float b, float c, float d)
        {
            if (t == 0)
                return b;

            if (t == d)
                return b + c;

            if ((t /= d / 2) < 1)
                return c / 2 * Mathf.Pow(2, 10 * (t - 1)) + b;

            return c / 2 * (-Mathf.Pow(2, -10 * --t) + 2) + b;
        }

        /// <summary>
        /// Exponential tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float ExpoOutIn(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            if (t < .5f)
                return ExpoOut(t * 2, b, c / 2);

            return ExpoIn((t * 2) - 1, b + c / 2, c / 2);
        }
        /// <summary>
        /// Exponential tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float ExpoOutIn(float t, float b, float c, float d)
        {
            if (t < d / 2)
                return ExpoOut(t * 2, b, c / 2, d);

            return ExpoIn((t * 2) - d, b + c / 2, c / 2, d);
        }

        #endregion

        #region ### Circular ###
        /// <summary>
        /// Circular tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float CircOut(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            return c * Mathf.Sqrt(1 - (t = t - 1) * t) + b;
        }
        /// <summary>
        /// Circular tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float CircOut(float t, float b, float c, float d)
            => c * Mathf.Sqrt(1 - (t = t / d - 1) * t) + b;

        /// <summary>
        /// Circular tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float CircIn(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            return -c * (Mathf.Sqrt(1 - t * t) - 1) + b;
        }
        /// <summary>
        /// Circular tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float CircIn(float t, float b, float c, float d)
            => -c * (Mathf.Sqrt(1 - (t /= d) * t) - 1) + b;

        /// <summary>
        /// Circular tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float CircInOut(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            if ((t /= .5f) < 1)
                return -c / 2 * (Mathf.Sqrt(1 - t * t) - 1) + b;

            return c / 2 * (Mathf.Sqrt(1 - (t -= 2) * t) + 1) + b;
        }
        /// <summary>
        /// Circular tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float CircInOut(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1)
                return -c / 2 * (Mathf.Sqrt(1 - t * t) - 1) + b;

            return c / 2 * (Mathf.Sqrt(1 - (t -= 2) * t) + 1) + b;
        }

        /// <summary>
        /// Circular tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float CircOutIn(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            if (t < .5f)
                return CircOut(t * 2, b, c / 2);

            return CircIn((t * 2) - 1, b + c / 2, c / 2);
        }
        /// <summary>
        /// Circular tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float CircOutIn(float t, float b, float c, float d)
        {
            if (t < d / 2)
                return CircOut(t * 2, b, c / 2, d);

            return CircIn((t * 2) - d, b + c / 2, c / 2, d);
        }

        #endregion

        #region ### Quadratic ###
        /// <summary>
        /// Quadratic tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float QuadOut(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            return -c * t * (t - 2) + b;
        }
        /// <summary>
        /// Quadratic tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float QuadOut(float t, float b, float c, float d)
            => -c * (t /= d) * (t - 2) + b;

        /// <summary>
        /// Quadratic tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float QuadIn(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            return c * t * t + b;
        }
        /// <summary>
        /// Quadratic tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float QuadIn(float t, float b, float c, float d)
            => c * (t /= d) * t + b;

        /// <summary>
        /// Quadratic tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float QuadInOut(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            if ((t /= .5f) < 1)
                return c / 2 * t * t + b;

            return -c / 2 * ((--t) * (t - 2) - 1) + b;
        }
        /// <summary>
        /// Quadratic tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float QuadInOut(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1)
                return c / 2 * t * t + b;

            return -c / 2 * ((--t) * (t - 2) - 1) + b;
        }

        /// <summary>
        /// Quadratic tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float QuadOutIn(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            if (t < .5f)
                return QuadOut(t * 2, b, c / 2);

            return QuadIn((t * 2) - 1, b + c / 2, c / 2);
        }

        /// <summary>
        /// Quadratic tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float QuadOutIn(float t, float b, float c, float d)
        {
            if (t < d / 2)
                return QuadOut(t * 2, b, c / 2, d);

            return QuadIn((t * 2) - d, b + c / 2, c / 2, d);
        }

        #endregion

        #region ### Sinus ###
        /// <summary>
        /// Sinusoidal tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float SineOut(float t, float b, float c)
            => c * Mathf.Sin(Mathf.Clamp01(t) * (Mathf.PI / 2)) + b;

        /// <summary>
        /// Sinusoidal tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float SineOut(float t, float b, float c, float d)
            => c * Mathf.Sin(t / d * (Mathf.PI / 2)) + b;

        /// <summary>
        /// Sinusoidal tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float SineIn(float t, float b, float c)
            => -c * Mathf.Cos(Mathf.Clamp01(t) * (Mathf.PI / 2)) + c + b;

        /// <summary>
        /// Sinusoidal tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float SineIn(float t, float b, float c, float d)
            => -c * Mathf.Cos(t / d * (Mathf.PI / 2)) + c + b;

        /// <summary>
        /// Sinusoidal tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float SineInOut(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            if ((t /= .5f) < 1)
                return c / 2 * (Mathf.Sin(Mathf.PI * t / 2)) + b;

            return -c / 2 * (Mathf.Cos(Mathf.PI * --t / 2) - 2) + b;

        }
        /// <summary>
        /// Sinusoidal tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float SineInOut(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1)
                return c / 2 * (Mathf.Sin(Mathf.PI * t / 2)) + b;

            return -c / 2 * (Mathf.Cos(Mathf.PI * --t / 2) - 2) + b;
        }

        /// <summary>
        /// Sinusoidal tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float SineOutIn(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            if (t < .5f)
                return SineOut(t * 2, b, c / 2);

            return SineIn((t * 2) - 1, b + c / 2, c / 2);
        }

        /// <summary>
        /// Sinusoidal tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float SineOutIn(float t, float b, float c, float d)
        {
            if (t < d / 2)
                return SineOut(t * 2, b, c / 2, d);

            return SineIn((t * 2) - d, b + c / 2, c / 2, d);
        }

        #endregion

        #region ### Cubic ###

        /// <summary>
        /// Cubic tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float CubicOut(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            return c * ((t = t - 1) * t * t + 1) + b;
        }
        /// <summary>
        /// Cubic tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float CubicOut(float t, float b, float c, float d)
            => c * ((t = t / d - 1) * t * t + 1) + b;

        /// <summary>
        /// Cubic tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float CubicIn(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            return c * t * t * t + b;
        }
        /// <summary>
        /// Cubic tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float CubicIn(float t, float b, float c, float d)
            => c * (t /= d) * t * t + b;

        /// <summary>
        /// Cubic tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float CubicInOut(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            if ((t /= .5f) < 1)
                return c / 2 * t * t * t + b;

            return c / 2 * ((t -= 2) * t * t + 2) + b;

        }
        /// <summary>
        /// Cubic tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float CubicInOut(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1)
                return c / 2 * t * t * t + b;

            return c / 2 * ((t -= 2) * t * t + 2) + b;
        }

        /// <summary>
        /// Cubic tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float CubicOutIn(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            if (t < .5f)
                return CubicOut(t * 2, b, c / 2);

            return CubicIn((t * 2) - 1, b + c / 2, c / 2);
        }

        /// <summary>
        /// Cubic tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float CubicOutIn(float t, float b, float c, float d)
        {
            if (t < d / 2)
                return CubicOut(t * 2, b, c / 2, d);

            return CubicIn((t * 2) - d, b + c / 2, c / 2, d);
        }

        #endregion

        #region ### Quartic ###

        /// <summary>
        /// Quartic tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float QuartOut(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            return -c * ((t = t - 1) * t * t * t - 1) + b;
        }
        /// <summary>
        /// Quartic tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float QuartOut(float t, float b, float c, float d)
            => -c * ((t = t / d - 1) * t * t * t - 1) + b;

        /// <summary>
        /// Quartic tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float QuartIn(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            return c * t * t * t * t + b;
        }
        /// <summary>
        /// Quartic tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float QuartIn(float t, float b, float c, float d)
            => c * (t /= d) * t * t * t + b;

        /// <summary>
        /// Quartic tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float QuartInOut(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            if ((t /= .5f) < 1)
                return c / 2 * t * t * t * t + b;

            return -c / 2 * ((t -= 2) * t * t * t - 2) + b;

        }
        /// <summary>
        /// Quartic tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float QuartInOut(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1)
                return c / 2 * t * t * t * t + b;

            return -c / 2 * ((t -= 2) * t * t * t - 2) + b;
        }

        /// <summary>
        /// Quartic tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float QuartOutIn(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            if (t < .5f)
                return QuartOut(t * 2, b, c / 2);

            return QuartIn((t * 2) - 1, b + c / 2, c / 2);
        }

        /// <summary>
        /// Quartic tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float QuartOutIn(float t, float b, float c, float d)
        {
            if ( t < d / 2 )
                return QuartOut( t * 2, b, c / 2, d );

            return QuartIn( ( t * 2 ) - d, b + c / 2, c / 2, d );
        }

        #endregion

        #region ### Quintic ###

        /// <summary>
        /// Quintic tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float QuintOut(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            return c * ((t = t - 1) * t * t * t * t + 1) + b;
        }
        /// <summary>
        /// Quintic tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float QuintOut(float t, float b, float c, float d)
            => c * ((t = t / d - 1) * t * t * t * t + 1) + b;

        /// <summary>
        /// Quintic tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float QuintIn(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            return c * t * t * t * t * t + b;
        }
        /// <summary>
        /// Quintic tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float QuintIn(float t, float b, float c, float d)
            => c * (t /= d) * t * t * t * t + b;

        /// <summary>
        /// Quintic tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float QuintInOut(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            if ((t /= .5f) < 1)
                return c / 2 * t * t * t * t * t + b;
            return c / 2 * ((t -= 2) * t * t * t * t + 2) + b;

        }
        /// <summary>
        /// Quintic tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float QuintInOut(float t, float b, float c, float d)
        {
            if ((t /= d / 2) < 1)
                return c / 2 * t * t * t * t * t + b;
            return c / 2 * ((t -= 2) * t * t * t * t + 2) + b;
        }

        /// <summary>
        /// Quintic tween with t clamped to 0..1
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <returns>tweened value</returns>
        public static float QuintOutIn(float t, float b, float c)
        {
            t = Mathf.Clamp01(t);
            if (t < .5f)
                return QuintOut(t * 2, b, c / 2);
            return QuintIn((t * 2) - 1, b + c / 2, c / 2);
        }

        /// <summary>
        /// Quintic tween
        /// </summary>
        /// <param name="t">Time</param>
        /// <param name="b">Start value</param>
        /// <param name="c">Delta value</param>
        /// <param name="d">Duration</param>
        /// <returns>tweened value</returns>
        public static float QuintOutIn(float t, float b, float c, float d)
        {
            if (t < d / 2)
                return QuintOut(t * 2, b, c / 2, d);
            return QuintIn((t * 2) - d, b + c / 2, c / 2, d);
        }

        #endregion

        // TODO: Add missing formulas (Elastic etc.) from here: http://wpf-animation.googlecode.com/svn/trunk/src/WPF/Animation/PennerDoubleAnimation.cs
    }
}
