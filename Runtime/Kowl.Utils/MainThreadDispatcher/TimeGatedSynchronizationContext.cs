using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Packages.de.kowl.utilities.Runtime.Kowl.Utils.MainThreadDispatcher
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

        /// <summary>
        /// Drains queued work on the calling thread until the time budget is spent or the queue is empty.
        /// Processes 0–N callbacks per call depending on remaining budget.
        /// </summary>
        public void Pump()
        {
            var startTicks = DateTime.UtcNow.Ticks;

            while (DateTime.UtcNow.Ticks - startTicks < _timeBudgetTicks)
            {
                if (!_queue.TryDequeue(out var work))
                {
                    break;
                }

                work.callback(work.state);
            }
        }
    }
}
