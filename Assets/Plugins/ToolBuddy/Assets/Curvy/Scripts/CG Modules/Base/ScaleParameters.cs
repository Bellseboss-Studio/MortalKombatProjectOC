// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEngine;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    /// <summary>
    /// A class used as a container of Scaling parameters found in <see cref="ScalingModule"/>
    /// </summary>
    public class ScaleParameters
    {
        /// <summary>
        /// <see cref="ScalingModule.ScaleMode"/>
        /// </summary>
        public readonly ScaleMode ScaleMode;

        /// <summary>
        /// <see cref="ScalingModule.ScaleReference"/>
        /// </summary>
        public readonly CGReferenceMode ScaleReference;

        /// <summary>
        /// <see cref="ScalingModule.ScaleUniform"/>
        /// </summary>
        public readonly bool ScaleUniform;

        /// <summary>
        /// <see cref="ScalingModule.ScaleOffset"/>
        /// </summary>
        public readonly float ScaleOffset;

        /// <summary>
        /// <see cref="ScalingModule.ScaleX"/>
        /// </summary>
        public readonly float ScaleX;

        /// <summary>
        /// <see cref="ScalingModule.ScaleY"/>
        /// </summary>
        public readonly float ScaleY;

        /// <summary>
        /// <see cref="ScalingModule.ScaleMultiplierX"/>
        /// </summary>
        public readonly AnimationCurve ScaleMultiplierX;

        /// <summary>
        /// <see cref="ScalingModule.ScaleMultiplierY"/>
        /// </summary>
        public readonly AnimationCurve ScaleMultiplierY;

        public ScaleParameters(ScaleMode scaleMode, CGReferenceMode scaleReference, bool scaleUniform, float scaleOffset,
            float scaleX, float scaleY, AnimationCurve scaleMultiplierX, AnimationCurve scaleMultiplierY)
        {
            ScaleMode = scaleMode;
            ScaleReference = scaleReference;
            ScaleUniform = scaleUniform;
            ScaleOffset = scaleOffset;
            ScaleX = scaleX;
            ScaleY = scaleY;
            ScaleMultiplierX = scaleMultiplierX;
            ScaleMultiplierY = scaleMultiplierY;
        }
    }
}