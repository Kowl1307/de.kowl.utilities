using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Packages.de.kowl.utilities.Runtime.Kowl.Utils.ObjectPools
{
    public class ObjectPool<T> : IObjectPool<T> where T : class
    {
        private readonly ConcurrentQueue<T> _objects;

        private Task fillTask;
        private readonly Func<T> objectFactory;
        private int _refillBatchSize;

        public ObjectPool(Func<T> objectFactory, int fillBatchSize = 2)
        {
            _objects = new ConcurrentQueue<T>();
            this.objectFactory = objectFactory;
            _refillBatchSize = fillBatchSize;
        }

        public async Task FillToAsync(int capacity, CancellationToken cancellationToken = default)
        {
            while (_objects.Count < capacity && !cancellationToken.IsCancellationRequested)
            {
                _objects.Enqueue(objectFactory());
            }

            fillTask = null;
        }

        public async Task<T> GetObjectAsync(CancellationToken cancellationToken = default)
        {
            if (_objects.TryDequeue(out var obj))
                return obj;

            T newObj;
            while (!_objects.TryDequeue(out newObj))
            {
                if (fillTask != null)
                {
                    await fillTask;
                    continue;
                }

                fillTask = FillToAsync(_refillBatchSize);
                await fillTask;
            }

            return newObj;
        }

        public void ReturnObject(T obj)
        {
            _objects.Enqueue(obj);
        }

        public void SetRefillAmount(int amount)
        {
            _refillBatchSize = amount;
        }

        public int CurrentAmount()
        {
            return _objects.Count;
        }

        public T GetObject()
        {
            return GetOrCreateOnMainThread();
        }

        public T GetOrCreateOnMainThread()
        {
            if (_objects.TryDequeue(out var result))
            {
                return result;
            }

            return objectFactory();
        }

        public void PrefillOnMainThread(int capacity, CancellationToken cancellationToken = default)
        {
            while (_objects.Count < capacity && !cancellationToken.IsCancellationRequested)
            {
                _objects.Enqueue(objectFactory());
            }
        }
    }
}