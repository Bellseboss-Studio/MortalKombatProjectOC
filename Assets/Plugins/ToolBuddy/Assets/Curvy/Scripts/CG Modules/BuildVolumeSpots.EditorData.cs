// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    public partial class BuildVolumeSpots
    {
        private struct EditorData : IEquatable<EditorData>
        {
            public int SpotsCount { get; }
            public bool InputIsAVolume { get; }
            [CanBeNull] [ItemNotNull] public string[] BoundsNames { get; }

            public EditorData([NotNull] IReadOnlyList<CGBounds> bounds, bool inputIsAVolume, int spotsCount)
            {
                SpotsCount = spotsCount;
                InputIsAVolume = inputIsAVolume;
                BoundsNames = GetBoundsNames(bounds);
            }

            [Pure]
            [NotNull]
            private static string[] GetBoundsNames([NotNull] IReadOnlyList<CGBounds> bounds)
            {
#if UNITY_EDITOR
                string[] names = new string[bounds.Count];
                CultureInfo invariantCulture = CultureInfo.InvariantCulture;

                for (int index = 0; index < bounds.Count; index++)
                    names[index] = string.Format(
                        invariantCulture,
                        "{0}:{1}",
                        index.ToString(invariantCulture),
                        bounds[index]
                            .Name
                    );
                return names;

#else
                return Array.Empty<string>();
#endif
            }


            public bool Equals(EditorData other)
                => SpotsCount == other.SpotsCount
                   && InputIsAVolume == other.InputIsAVolume
                   && ((BoundsNames == null && other.BoundsNames == null) 
                       || (BoundsNames != null && other.BoundsNames != null && BoundsNames.SequenceEqual(other.BoundsNames)));


            public override bool Equals(object obj)
                => obj is EditorData other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = SpotsCount;
                    hashCode = (hashCode * 397) ^ InputIsAVolume.GetHashCode();
                    hashCode = (hashCode * 397)
                               ^ (BoundsNames != null
                                   ? BoundsNames.GetHashCode()
                                   : 0);
                    return hashCode;
                }
            }

            public static bool operator ==(EditorData left, EditorData right)
                => left.Equals(right);

            public static bool operator !=(EditorData left, EditorData right)
                => !left.Equals(right);
        }
    }
}