// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using System.Collections.Generic;
using FluffyUnderware.Curvy.Pools;
using JetBrains.Annotations;
using ToolBuddy.Pooling.Collections;
using ToolBuddy.Pooling.Pools;

namespace FluffyUnderware.Curvy.Generator
{
    /// <summary>
    /// A collection of <see cref="CGSpot"/>
    /// </summary>
    [CGDataInfo(
        0.96f,
        0.96f,
        0.96f
    )]
    public class CGSpots : CGData
    {
        //DESIGN what is the use of this class? Seems to me like a complicated way to represent an array

        /// <summary>
        /// List of spots
        /// </summary>
        /// <remarks>Setting a new <see cref="SubArray{T}"/> will <see cref="ArrayPool{T}.Free(ToolBuddy.Pooling.Collections.SubArray{T})"/> the current <see cref="SubArray{T}"/>  instance</remarks>

        public SubArray<CGSpot> Spots
        {
            get => spots;
            set
            {
                ArrayPools.CGSpot.Free(spots);
                spots = value;
            }
        }

        /// <summary>
        /// List of spots
        /// </summary>
        /// <remarks>This getter returns a copy of the actual array. For performance reasons, use the equivalent getter returning a <see cref="SubArray{T}"/> instance, which allows you to directly access and modify the underlying array</remarks>
        [UsedImplicitly]
        [Obsolete("Use Spots instead")]
        public CGSpot[] Points
        {
            get => Spots.CopyToArray(ArrayPools.CGSpot);
            set => Spots = new SubArray<CGSpot>(value);
        }

        private SubArray<CGSpot> spots;

        public override int Count => spots.Count;

        public CGSpots() =>
            spots = ArrayPools.CGSpot.Allocate(0);

        public CGSpots(params CGSpot[] points) =>
            spots = new SubArray<CGSpot>(points);

        public CGSpots(SubArray<CGSpot> spots) =>
            this.spots = spots;

        public CGSpots(List<CGSpot> spots)
        {
            this.spots = ArrayPools.CGSpot.Allocate(spots.Count);
            spots.CopyTo(
                0,
                this.spots.Array,
                0,
                spots.Count
            );
        }

        public CGSpots(params List<CGSpot>[] spots)
        {
            int c = 0;
            for (int i = 0; i < spots.Length; i++)
                c += spots[i].Count;
            this.spots = ArrayPools.CGSpot.Allocate(c);
            c = 0;
            for (int i = 0; i < spots.Length; i++)
            {
                List<CGSpot> cgSpots = spots[i];
                cgSpots.CopyTo(
                    0,
                    this.spots.Array,
                    c,
                    cgSpots.Count
                );
                c += cgSpots.Count;
            }
        }

        public CGSpots(CGSpots source) =>
            spots = ArrayPools.CGSpot.Clone(source.spots);

        protected override bool Dispose(bool disposing)
        {
            bool result = base.Dispose(disposing);
            if (result)
                ArrayPools.CGSpot.Free(spots);
            return result;
        }

        public override T Clone<T>()
            => new CGSpots(this) as T;
    }
}