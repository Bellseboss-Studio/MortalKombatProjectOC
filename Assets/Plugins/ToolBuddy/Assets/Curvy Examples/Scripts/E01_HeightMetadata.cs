// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using FluffyUnderware.DevTools;
using UnityEngine;

namespace FluffyUnderware.Curvy.Examples
{
    public class E01_HeightMetadata : CurvyInterpolatableMetadataBase<float>
    {
        [SerializeField]
        [RangeEx(
            0,
            1,
            Slider = true
        )]
#pragma warning disable 649
        private float m_Height;
#pragma warning restore 649

        public override float MetaDataValue => m_Height;

        public override float Interpolate(CurvyInterpolatableMetadataBase<float> nextMetadata, float interpolationTime)
            => nextMetadata != null
                ? Mathf.Lerp(
                    MetaDataValue,
                    nextMetadata.MetaDataValue,
                    interpolationTime
                )
                : MetaDataValue;
    }
}