using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using UnityEngine.Profiling;

namespace Kowl.Utils.MainThreadDispatcher
{
    public sealed class TimeGatedSynchronizationContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<(SendOrPostCallback callback, object state)> _queue = new();
        private readonly long _timeBudgetTicks;

        public TimeGatedSynchronizationContext(double milliseconds)
        {
            _timeBudgetTicks = (long)(milliseconds * TimeSpan.TicksPerMillisecond);
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            _queue.Enqueue((d, state));
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            var mre = new ManualResetEventSlim(false);
            Exception ex = null;

            Post(s =>
            {
                try { d(s); }
                catch (Exception e) { ex = e; }
                finally { mre.Set(); }
            }, state);

            mre.Wait();
            mre.Dispose();
            if (ex != null) throw ex;
        }

        public void Pump()
        {
            if (_queue.Count == 0)
            {
                return;
            }
            
            var startTicks = DateTime.UtcNow.Ticks;

            do
            {
                if (!_queue.TryDequeue(out var work))
                {
                    break;
                }
                work.callback(work.state);
                
            } while (DateTime.UtcNow.Ticks - startTicks >= _timeBudgetTicks);
        }
    }
}