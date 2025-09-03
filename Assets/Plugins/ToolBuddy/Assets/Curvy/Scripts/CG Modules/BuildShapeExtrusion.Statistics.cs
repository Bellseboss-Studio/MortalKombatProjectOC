// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using JetBrains.Annotations;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    public partial class BuildShapeExtrusion
    {
        /// <summary>
        /// Statistics about the extrusion
        /// </summary>
        public struct Statistics : IEquatable<Statistics>
        {
            /// <summary>
            /// Number of samples along the path
            /// </summary>
            public int PathSampleCount
            {
                get;
                [UsedImplicitly]
                [Obsolete]
                set;
            }

            /// <summary>
            /// Number of samples along the cross section
            /// </summary>
            public int CrossSampleCount
            {
                get;
                [UsedImplicitly]
                [Obsolete]
                set;
            }

            /// <summary>
            /// Number of material groups
            /// </summary>
            public int MaterialGroupsCount
            {
                get;
                [UsedImplicitly]
                [Obsolete]
                set;
            }

            /// <summary>
            /// Set the statistics
            /// </summary>
            /// <param name="pathSamples">Number of samples along the path</param>
            /// <param name="crossSamples">Number of samples along the cross section</param>
            /// <param name="crossGroups">Number of material groups</param>
            public void Set(int pathSamples, int crossSamples, int crossGroups)
            {
#pragma warning disable 612
                PathSampleCount = pathSamples;
                CrossSampleCount = crossSamples;
                MaterialGroupsCount = crossGroups;
#pragma warning restore 612
            }

            public bool Equals(Statistics other)
                => PathSampleCount == other.PathSampleCount
                   && CrossSampleCount == other.CrossSampleCount
                   && MaterialGroupsCount == other.MaterialGroupsCount;

            public override bool Equals(object obj)
                => obj is Statistics other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = PathSampleCount;
                    hashCode = (hashCode * 397) ^ CrossSampleCount;
                    hashCode = (hashCode * 397) ^ MaterialGroupsCount;
                    return hashCode;
                }
            }

            public static bool operator ==(Statistics left, Statistics right)
                => left.Equals(right);

            public static bool operator !=(Statistics left, Statistics right)
                => !left.Equals(right);
        }

        [UsedImplicitly]
        [Obsolete("Use ExtrusionStatistics instead")]
        public int PathSamples
        {
            get => ExtrusionStatistics.PathSampleCount;
            private set
            {
                Statistics statistics = ExtrusionStatistics;
                statistics.PathSampleCount = value;
                ExtrusionStatistics = statistics;
            }
        }


        [UsedImplicitly]
        [Obsolete("Use ExtrusionStatistics instead")]
        public int CrossSamples
        {
            get => ExtrusionStatistics.CrossSampleCount;
            private set
            {
                Statistics statistics = ExtrusionStatistics;
                statistics.CrossSampleCount = value;
                ExtrusionStatistics = statistics;
            }
        }

        [UsedImplicitly]
        [Obsolete("Use ExtrusionStatistics instead")]
        public int CrossGroups
        {
            get => ExtrusionStatistics.MaterialGroupsCount;
            private set
            {
                Statistics statistics = ExtrusionStatistics;
                statistics.MaterialGroupsCount = value;
                ExtrusionStatistics = statistics;
            }
        }
    }
}