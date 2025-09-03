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
#if THREADING_SUPPORTED
using System.Collections.Generic;
using System.Linq;
using System.Threading;
#endif

namespace FluffyUnderware.DevTools
{
    /// <summary>
    /// This class is not very optimized. For better performance, use the generic version of ThreadPoolWorker instead
    /// </summary>
    [Obsolete("Use ThreadPoolWorker<T> instead")]
#if THREADING_SUPPORTED
    public class ThreadPoolWorker : IDisposable
    {
        private int _remainingWorkItems = 1;
        private ManualResetEvent _done = new ManualResetEvent(false);

        public void QueueWorkItem(WaitCallback callback)
        {
            QueueWorkItem(callback, null);
        }

        public void QueueWorkItem(Action act)
        {
            QueueWorkItem(act, null);
        }

        public void ParralelFor<T>(Action<T> action, List<T> list)
        {
            int threadsToUseCount;
            {
                int availableThreads;
#if NET_4_6
                int temp;
                ThreadPool.GetAvailableThreads(out availableThreads, out temp);
#else
                availableThreads = System.Environment.ProcessorCount - 1;
#endif
                threadsToUseCount = 1 /*main thread*/ + Math.Min(availableThreads, System.Environment.ProcessorCount - 1 /*keep one processor for the main thead*/);
            }
            int iterationsCount = list.Count;
            if (threadsToUseCount == 1 || iterationsCount == 1)
            {
                for (int i = 0; i < iterationsCount; i++)
                {
                    action(list[i]);
                }
            }
            else
            {
                int iterationPerThread = (int)Math.Ceiling((float)iterationsCount / threadsToUseCount);
                int currentIndex = 0;
                while (currentIndex < iterationsCount)
                {
                    QueuedCallback queuedCallback = new QueuedCallback();

                    int endEndex = Math.Min(currentIndex + iterationPerThread, iterationsCount - 1);

                    LoopState<T> loopState = new LoopState<T>((short)currentIndex,
                        (short)endEndex,
                        list,
                        iterationsCount,
                        (item,
                            itemIndex,
                            itemsCount) => action(item));
                    queuedCallback.State = loopState;

                    queuedCallback.Callback = state =>
                    {
                        LoopState<T> loopS = (LoopState<T>)state;
                        for (int i = loopS.StartIndex; i <= loopS.EndIndex; i++)
                        {
                            loopS.Action(loopS.Items.ElementAt(i), i, iterationsCount);
                        }
                    };


                    QueueWorkItem(queuedCallback);

                    currentIndex = endEndex + 1;
                }
            }
        }

        private void QueueWorkItem(QueuedCallback callback)
        {
            ThrowIfDisposed();
            lock (_done)
                _remainingWorkItems++;
            ThreadPool.QueueUserWorkItem(new WaitCallback(HandleWorkItem), callback);
        }

        public void QueueWorkItem(WaitCallback callback, object state)
        {
            QueuedCallback qc = new QueuedCallback();
            qc.Callback = callback;
            qc.State = state;
            QueueWorkItem(qc);
        }

        public void QueueWorkItem(Action act, object state)
        {
            QueuedCallback qc = new QueuedCallback();
            qc.Callback = (x => act());
            qc.State = state;
            QueueWorkItem(qc);
        }

        public bool WaitAll()
            => WaitAll(-1, false);

        public bool WaitAll(TimeSpan timeout, bool exitContext)
            => WaitAll((int)timeout.TotalMilliseconds, exitContext);

        public bool WaitAll(int millisecondsTimeout, bool exitContext)
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

        private void HandleWorkItem(object state)
        {
            QueuedCallback qc = (QueuedCallback)state;
            try
            {
                qc.Callback(qc.State);
            }
            finally
            {
                DoneWorkItem();
            }
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

        private void ThrowIfDisposed()
        {
            if (_done == null)
                throw new ObjectDisposedException(GetType().Name);
        }

        public void Dispose()
        {
            if (_done != null)
            {
                ((IDisposable)_done).Dispose();
                _done = null;
            }
        }
    }
#else
    public class ThreadPoolWorker{}
#endif

}