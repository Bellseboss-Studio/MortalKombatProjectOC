// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// List of Material Groups
    /// </summary>
    public class SamplePointsMaterialGroupCollection : List<SamplePointsMaterialGroup>
    {
        public int TriangleCount
        {
            get
            {
                int cnt = 0;
                for (int g = 0; g < Count; g++)
                    cnt += this[g].TriangleCount;
                return cnt;
            }
        }

        public int MaterialID;

        [UsedImplicitly]
        [Obsolete("Use AspectCorrectionV instead")]
        public float AspectCorrection
        {
            get => AspectCorrectionV;
            set => AspectCorrectionV = value;
        }

        /// <summary>
        /// A multiplication applied on the U coordinate as part of the aspect correction
        /// </summary>
        public float AspectCorrectionU = 1;

        /// <summary>
        /// A multiplication applied on the V coordinate as part of the aspect correction
        /// </summary>
        public float AspectCorrectionV = 1;

        public SamplePointsMaterialGroupCollection() { }
        public SamplePointsMaterialGroupCollection(int capacity) : base(capacity) { }
        public SamplePointsMaterialGroupCollection(IEnumerable<SamplePointsMaterialGroup> collection) : base(collection) { }

        public void CalculateAspectCorrection(CGVolume volume, CGMaterialSettingsEx matSettings)
        {
            switch (matSettings.KeepAspect)
            {
                case CGKeepAspectMode.Off:
                    AspectCorrectionV = 1;
                    AspectCorrectionU = 1;
                    break;
                case CGKeepAspectMode.ScaleU:
                case CGKeepAspectMode.ScaleV:
                {
                    float crossLength = 0;
                    float uLength = 0;
                    for (int g = 0; g < Count; g++)
                    {
                        float length, u;
                        this[g].GetLengths(
                            volume,
                            out length,
                            out u
                        );
                        crossLength += length;
                        uLength += u;
                    }

                    if (matSettings.KeepAspect == CGKeepAspectMode.ScaleU)
                    {
                        AspectCorrectionV = 1;
                        AspectCorrectionU = crossLength / volume.Length;
                    }
                    else
                    {
                        AspectCorrectionV = (volume.Length * uLength) / crossLength;
                        AspectCorrectionU = 1;
                    }
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}