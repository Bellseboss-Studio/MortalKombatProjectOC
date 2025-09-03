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
    /// Bounds data class
    /// </summary>
    [CGDataInfo(
        1,
        0.8f,
        0.5f
    )]
    public class CGBounds : CGData
    {
        protected Bounds? mBounds;

        public Bounds Bounds
        {
            get
            {
                if (!mBounds.HasValue)
                    RecalculateBounds();
                return mBounds.Value;
            }
            set => mBounds = value;
        }

        public float Depth =>
            //OPTIM just do the delta between max z and min z, and get rid of bounds
            Bounds.size.z;

        public CGBounds() { }

        public CGBounds(Bounds bounds) =>
            Bounds = bounds;

        public CGBounds(CGBounds source)
        {
            Name = source.Name;
            if (source.mBounds.HasValue) //Do not copy bounds if they are not computed yet
                Bounds = source.Bounds;
        }


        public virtual void RecalculateBounds() =>
            Bounds = new Bounds();

        public override T Clone<T>()
            => new CGBounds(this) as T;

        public static void Copy(CGBounds dest, CGBounds source)
        {
            if (source.mBounds.HasValue) //Do not copy bounds if they are not computed yet
                dest.Bounds = source.Bounds;
        }
    }
}