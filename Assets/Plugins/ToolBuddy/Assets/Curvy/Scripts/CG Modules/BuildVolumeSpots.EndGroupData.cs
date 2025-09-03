// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy.Pools;
using FluffyUnderware.DevTools;
using ToolBuddy.Pooling.Collections;

namespace FluffyUnderware.Curvy.Generator.Modules
{
    public partial class BuildVolumeSpots
    {
        /// <summary>
        /// holds data that will be used to place groups at a later time
        /// </summary>
        private sealed class EndGroupData : IDisposable
        {
            internal CGBoundsGroup BoundsGroup { get; }
            internal SubArray<int> ItemIndices { get; }
            internal float GroupDepth { get; }
            internal CGBounds[] ItemBounds { get; }
            internal float SpaceBefore { get; }
            internal float SpaceAfter { get; }

            internal EndGroupData(CGBoundsGroup boundsGroup, SubArray<int> itemIndices, float groupDepth, CGBounds[] itemBounds,
                float spaceBefore, float spaceAfter)
            {
                BoundsGroup = boundsGroup;
                ItemIndices = itemIndices;
                GroupDepth = groupDepth;
                ItemBounds = itemBounds;
                SpaceBefore = spaceBefore;
                SpaceAfter = spaceAfter;
            }

            #region Dispose pattern

            private bool disposed;

            private bool Dispose(bool disposing)
            {
                if (disposed)
                {
                    DTLog.LogWarning("[Curvy] Attempt to dispose an EndGroupData twice. Please raise a bug report.");
                    return false;
                }

                ArrayPools.Int32.Free(ItemIndices);

                disposed = true;
                return true;
            }

            /// <summary>
            /// Disposes an instance that is no more used, allowing it to free its resources immediately.
            /// Dispose is called automatically when an instance is <see cref="Finalize()"/>d
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            ~EndGroupData() =>
                Dispose(false);

            #endregion
        }
    }
}