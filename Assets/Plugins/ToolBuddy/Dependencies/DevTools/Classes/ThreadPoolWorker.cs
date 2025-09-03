// =====================================================================
// Copyright 2013-2017 Fluffy Underware
// All rights reserved
// 
// http://www.fluffyunderware.com
// =====================================================================

#if !UNITY_WSA && !UNITY_WEBGL
#define THREADING_SUPPORTED
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FluffyUnderware.DevTools
{
    /// <summary>
    /// A class to execute actions in a multi-threaded way
    /// </summary>
    /// <typeparam name="T">The type of the action input</typeparam>
    public class ThreadPoolWorker<T> : IDisposable
    {
        //TODO OPTIM Is ThreadPoolWorker still needed. Aren't all unity version handling .Net's parallel fors now?

        public ThreadPoolWorker()
        {
#if THREADING_SUPPORTED
            handleWorkItemCallBack = o =>
            {
                QueuedCallback queuedCallback = (QueuedCallback)o;
                try
                {
                    queuedCallback.Callback(queuedCallback.State);
                }
                finally
                {
                    lock (queuedCallbackPool)
                        queuedCallbackPool.ReleaseItem(queuedCallback);
                    DoneWorkItem();
                }
            };

            handleLoopCallBack = state =>
            {
                LoopState<T> loopS = (LoopState<T>)state;
                for (int i = loopS.StartIndex; i <= loopS.EndIndex; i++)
                {
                    loopS.Action(loopS.Items.ElementAt(i), i, loopS.ItemsCount);
                }
                lock (loopStatePool)
                    loopStatePool.ReleaseItem(loopS);
            };
#endif
        }

        [JetBrains.Annotations.UsedImplicitly] [Obsolete("Use ParallelFor(Action<T,int,int> action, IEnumerable<T> list) instead")]
        public void ParralelFor(Action<T> action, List<T> list)
        {
            ParallelFor((item, itemIndex, itemsCount) => action(item),
                list,
                list.Count());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ParallelFor(Action<T, int, int> action, IEnumerable<T> list)
        {
            ParallelFor(action,
                list,
                list.Count());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ParallelFor(Action<T, int, int> action, IEnumerable<T> list, int elementsCount)
        {
            if (Environment.IsThreadingSupported)
                DoParallelFor(action,
                    list,
                    elementsCount);
            else
                for (int i = 0; i < elementsCount; i++)
                    action(list.ElementAt(i),
                        i,
                        elementsCount);
        }

        public void Dispose()
        {
#if THREADING_SUPPORTED

            if (_done != null)
            {
                ((IDisposable)_done).Dispose();
                _done = null;
            }
#endif
        }

        [System.Diagnostics.Conditional("THREADING_SUPPORTED")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DoParallelFor(Action<T, int, int> action, IEnumerable<T> list, int elementsCount)
        {
#if THREADING_SUPPORTED
            int threadsToUseCount = ThreadsToUseCount;

            //BUG a bug in iterationPerThread leads to sometimes not using all available threads. For example, if you have 5 items and 4 cores, only 3 cores will be used
            int iterationPerThread = threadsToUseCount == 1
                ? elementsCount
                : (int)Math.Ceiling((float)elementsCount / threadsToUseCount);
#if CURVY_SANITY_CHECKS
            UnityEngine.Assertions.Assert.IsTrue(iterationPerThread * threadsToUseCount >= elementsCount);
#endif
            int currentIndex = 0;
            while (currentIndex < elementsCount)
            {
                int endEndex = Math.Min(currentIndex + iterationPerThread - 1,
                    elementsCount - 1);

                if (endEndex == elementsCount - 1)
                    for (int i = currentIndex; i <= endEndex; i++)
                        action(list.ElementAt(i),
                            i,
                            elementsCount);
                else
                {
                    QueuedCallback queuedCallback;
                    {
                        lock (queuedCallbackPool)
                            queuedCallback = queuedCallbackPool.GetItem();
                    }


                    LoopState<T> loopState;
                    {
                        lock (loopStatePool)
                            loopState = loopStatePool.GetItem();
                    }

                    loopState.Set((short)currentIndex,
                        (short)endEndex,
                        list,
                        elementsCount,
                        action);

                    queuedCallback.State = loopState;
                    queuedCallback.Callback = handleLoopCallBack;

                    ThrowIfDisposed();
                    //Debug.LogWarning("New thread " + " from "+ loopState.StartIndex +  " to " + loopState.EndIndex);
                    lock (_done)
                        _remainingWorkItems++;
                    System.Threading.ThreadPool.QueueUserWorkItem(handleWorkItemCallBack,
                        queuedCallback);
                }

                currentIndex = endEndex + 1;
            }

            WaitAll(-1,
                false);
#else
            throw new NotSupportedException("Current environment does not support multi threading");
#endif
        }

#if THREADING_SUPPORTED


        private readonly SimplePool<QueuedCallback> queuedCallbackPool = new SimplePool<QueuedCallback>(4);
        private readonly SimplePool<LoopState<T>> loopStatePool = new SimplePool<LoopState<T>>(4);

        private int _remainingWorkItems = 1;
        private System.Threading.ManualResetEvent _done = new System.Threading.ManualResetEvent(false);
        private readonly System.Threading.WaitCallback handleWorkItemCallBack;
        private readonly System.Threading.WaitCallback handleLoopCallBack;

        private static int ThreadsToUseCount
        {
            get
            {
                int threadsToUseCount;
                {
                    int availableThreads;
#if NET_4_6
                    int temp;
                    System.Threading.ThreadPool.GetAvailableThreads(out availableThreads, out temp);
#else
                    availableThreads = System.Environment.ProcessorCount - 1;
#endif
                    threadsToUseCount = 1 /*main thread*/ +
                                        Math.Min(availableThreads,
                                            System.Environment.ProcessorCount - 1 /*keep one processor for the main thead*/);
                }
                return threadsToUseCount;
            }
        }

        private bool WaitAll(int millisecondsTimeout, bool exitContext)
        {
            ThrowIfDisposed();
            DoneWorkItem();
            bool rv = _done.WaitOne(millisecondsTimeout, exitContext);
            lock (_done)
            {
                if (rv)
                {
                    _remainingWorkItems = 1;
                    _done.Reset();
                }
                else
                    _remainingWorkItems++;
            }
            return rv;
        }

        private void ThrowIfDisposed()
        {
            if (_done == null)
                throw new ObjectDisposedException(GetType().Name);
        }

        private void DoneWorkItem()
        {
            lock (_done)
            {
                --_remainingWorkItems;
                if (_remainingWorkItems == 0)
                    _done.Set();
            }
        }
#endif
    }
}
