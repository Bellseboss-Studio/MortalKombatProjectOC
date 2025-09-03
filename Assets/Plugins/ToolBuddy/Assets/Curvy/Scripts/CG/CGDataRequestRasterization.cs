// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEngine;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// Rasterization Request parameters
    /// </summary>
    public class CGDataRequestRasterization : CGDataRequestParameter
    {
#if CONTRACTS_FULL
        [ContractInvariantMethod]
        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Microsoft.Performance",
            "CA1822:MarkMembersAsStatic",
            Justification =
                "Required for code contracts."
        )]
        private void ObjectInvariant()
        {
            Contract.Invariant(Start.IsRatio());
            Contract.Invariant(RasterizedRelativeLength.IsRatio());
            Contract.Invariant(Resolution > 0);
            Contract.Invariant(Resolution <= 100);
            Contract.Invariant(SplineAbsoluteLength.IsPositiveNumber());
            Contract.Invariant(AngleThreshold.IsIn0To180Range());
        }
#endif


        public enum ModeEnum
        {
            /// <summary>
            /// Distribute sample points evenly spread
            /// </summary>
            Even,

            /// <summary>
            /// Use Source' curvation to optimize the result
            /// </summary>
            Optimized
        }

        /// <summary>
        /// Relative Start Position (0..1)
        /// </summary>
        public float Start;

        /// <summary>
        /// Relative Length. A value of 1 means the full spline length
        /// </summary>
        public float RasterizedRelativeLength;

        /// <summary>
        /// Maximum number of samplepoints
        /// </summary>
        public int Resolution;

        /// <summary>
        /// Angle resolution (0..100) for optimized mode
        /// </summary>
        public float AngleThreshold;

        /// <summary>
        /// Rasterization mode
        /// </summary>
        public ModeEnum Mode;

        public CGDataRequestRasterization(float start, float rasterizedRelativeLength, int resolution, float angle,
            ModeEnum mode = ModeEnum.Even)
        {
#if CONTRACTS_FULL
            Contract.Requires(rasterizedRelativeLength.IsRatio());
#endif
            Start = Mathf.Repeat(
                start,
                1
            );
            RasterizedRelativeLength = Mathf.Clamp01(rasterizedRelativeLength);
            Resolution = resolution;
            AngleThreshold = angle;
            Mode = mode;
        }

        public CGDataRequestRasterization(CGDataRequestRasterization source) : this(
            source.Start,
            source.RasterizedRelativeLength,
            source.Resolution,
            source.AngleThreshold,
            source.Mode
        ) { }

        public override bool Equals(object obj)
        {
            CGDataRequestRasterization O = obj as CGDataRequestRasterization;
            if (O == null)
                return false;
            return Start == O.Start
                   && RasterizedRelativeLength == O.RasterizedRelativeLength
                   && Resolution == O.Resolution
                   && AngleThreshold == O.AngleThreshold
                   && Mode == O.Mode;
        }

        public override int GetHashCode()
            => new { A = Start, B = RasterizedRelativeLength, C = Resolution, D = AngleThreshold, E = Mode }
                .GetHashCode(); //OPTIM avoid array creation

        public override string ToString()
            =>
                $"{nameof(Start)}: {Start}, {nameof(RasterizedRelativeLength)}: {RasterizedRelativeLength}, {nameof(Resolution)}: {Resolution}, {nameof(AngleThreshold)}: {AngleThreshold}, {nameof(Mode)}: {Mode}";
    }
}