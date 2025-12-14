using System.Collections.Generic;

namespace Kowl.Utils
{
    public class ConcurrentPriorityQueue<TElement, TPriority>
    {
        private readonly object _lock = new();
        private readonly PriorityQueue<TElement, TPriority> _queue;

        public ConcurrentPriorityQueue()
        {
            _queue = new PriorityQueue<TElement, TPriority>();
        }

        public ConcurrentPriorityQueue(IComparer<TPriority> comparer)
        {
            _queue = new PriorityQueue<TElement, TPriority>(comparer);
        }

        public void Enqueue(TElement element, TPriority priority)
        {
            lock (_lock)
            {
                _queue.Enqueue(element, priority);
            }
        }

        public bool TryDequeue(out TElement element, out TPriority priority)
        {
            lock (_lock)
            {
                if (_queue.Count == 0)
                {
                    element = default;
                    priority = default;
                    return false;
                }

                return _queue.TryDequeue(out element, out priority);
            }
        }

        public bool TryPeek(out TElement element, out TPriority priority)
        {
            lock (_lock)
            {
                if (_queue.Count == 0)
                {
                    element = default;
                    priority = default;
                    return false;
                }

                return _queue.TryPeek(out element, out priority);
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _queue.Count;
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _queue.Clear();
            }
        }
    }
}