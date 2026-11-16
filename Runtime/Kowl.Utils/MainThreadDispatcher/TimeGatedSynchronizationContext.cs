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
        /// Drops all pending work without executing it. Awaiters must be completed via cancellation registrations.
        /// </summary>
        public void Clear()
        {
            while (_queue.TryDequeue(out _))
            {
            }
        }

        /// <summary>
        /// Drains queued work on the calling thread until the time budget is spent, the queue is empty, or cancellation is requested.
        /// Processes 0–N callbacks per call depending on remaining budget.
        /// </summary>
        public void Pump(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Clear();
                return;
            }

            var startTicks = DateTime.UtcNow.Ticks;

            while (DateTime.UtcNow.Ticks - startTicks < _timeBudgetTicks)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Clear();
                    break;
                }

                if (!_queue.TryDequeue(out var work))
                {
                    break;
                }

                work.callback(work.state);
            }
        }
    }
}
