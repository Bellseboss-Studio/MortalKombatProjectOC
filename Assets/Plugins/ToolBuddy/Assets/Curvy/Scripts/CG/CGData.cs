// =====================================================================
// Copyright © 2013 ToolBuddy
// All rights reserved
// 
// http://www.toolbuddy.net
// =====================================================================

using System;
using FluffyUnderware.Curvy.Utils;
using FluffyUnderware.DevTools;
using ToolBuddy.Pooling.Collections;
#if CONTRACTS_FULL
using System.Diagnostics.Contracts;
#endif


namespace FluffyUnderware.Curvy.Generator
{
    //TODO replace all the misuse of the F concept here, where it should really be RelativeDistance 

    /// <summary>
    /// Data Base class
    /// </summary>
    public class CGData : IDisposable
    {
        #region Dispose pattern

        private bool disposed;

        protected virtual bool Dispose(bool disposing)
        {
            if (disposed)
            {
                DTLog.LogWarning("[Curvy] Attempt to dispose a CGData twice. Please raise a bug report.");
                return false;
            }

            disposed = true;
            return true;
        }

        /// <summary>
        /// Disposes an instance that is no more used, allowing it to free its resources immediately.
        /// Dispose is called automatically when an instance is <see cref="Finalize"/>d
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CGData() =>
            Dispose(false);

        #endregion

        public string Name;

        public virtual int Count => 0;

        public static implicit operator bool(CGData a)
            => !ReferenceEquals(
                a,
                null
            );

        public virtual T Clone<T>() where T : CGData
            => new CGData() as T;

        /// <summary>
        /// Searches FMapArray and returns the index that covers the fValue as well as the percentage between index and index+1
        /// </summary>
        /// <param name="FMapArray">array of sorted values ranging from 0..1</param>
        /// <param name="fValue">a value 0..1</param>
        /// <param name="frag">fragment between the resulting and the next index (0..1)</param>
        /// <returns>the index where fValue lies in</returns>
        protected int getGenericFIndex(SubArray<float> FMapArray, float fValue, out float frag)
        {
            //WARNING this method is inlined in DeformMesh, if you modify something here modify it there too
            int index = CurvyUtility.InterpolationSearch(
                FMapArray.Array,
                FMapArray.Count,
                fValue
            );

            if (index == FMapArray.Count - 1)
            {
                index -= 1;
                frag = 1;
            }
            else
                frag = (fValue - FMapArray.Array[index]) / (FMapArray.Array[index + 1] - FMapArray.Array[index]);

            return index;
        }
    }
}