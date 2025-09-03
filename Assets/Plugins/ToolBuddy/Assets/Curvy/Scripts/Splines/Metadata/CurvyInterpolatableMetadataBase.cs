// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using UnityEngine;

namespace FluffyUnderware.Curvy
{
    /// <summary>
    /// Base class for Metadata classes that support interpolation.
    /// </summary>
    /// <typeparam name="T">The Type of the Metadata's value</typeparam>
    [ExecuteAlways]
    public abstract class CurvyInterpolatableMetadataBase<T> : CurvyMetadataBase
    {
        /// <summary>
        /// The value stored within this Metadata instance
        /// </summary>
        public abstract T MetaDataValue { get; }

        /// <summary>
        /// Interpolates between the current Metadata's value and the one from the next Control Point's Metadata.
        /// </summary>
        /// <param name="nextMetadata">The Metadata from the Control Point next to the current one</param>
        /// <param name="interpolationTime">The local F value on the segment defined by the current Control Point and the next one</param>
        /// <returns></returns>
        public abstract T Interpolate(CurvyInterpolatableMetadataBase<T> nextMetadata, float interpolationTime);
    }
}