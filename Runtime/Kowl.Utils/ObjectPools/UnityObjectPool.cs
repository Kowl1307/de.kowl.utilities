using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Packages.de.kowl.utilities.Runtime.Kowl.Utils.ObjectPools
{
    public class UnityObjectPool<T> : IObjectPool<T> where T : UnityEngine.Object
    {
        private readonly ConcurrentQueue<T> _objects;
        private readonly T _objectPrefab;

        private Task fillTask;

        private int _refillBatchSize;

        public UnityObjectPool(T prefab, int fillBatchSize = 2)
        {
            _objects = new ConcurrentQueue<T>();
            _objectPrefab = prefab;
            _refillBatchSize = fillBatchSize;
        }

        /// <summary>
        /// Prefills the pool. Must run on the Unity main thread (<see cref="GameObject.InstantiateAsync"/> requirement).
        /// </summary>
        public async Task FillToAsync(int capacity, CancellationToken cancellationToken = default)
        {
            while (_objects.Count < capacity && !cancellationToken.IsCancellationRequested)
            {
                var objects = await GameObject.InstantiateAsync(_objectPrefab, _refillBatchSize, null, Vector3.zero, Quaternion.identity, cancellationToken);
                foreach (var o in objects)
                {
                    _objects.Enqueue(o);
                }
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
                cancellationToken.ThrowIfCancellationRequested();

                if (fillTask != null)
                {
                    await fillTask;
                    continue;
                }

                // InstantiateAsync is main-thread-only. Prefer GetOrCreateOnMainThread from a time-gated callback.
                fillTask = FillToAsync(_refillBatchSize, cancellationToken);
                await fillTask;
            }

            return newObj;
        }

        /// <summary>
        /// Dequeues a pooled instance or synchronously Instantiates one.
        /// Must be called on the Unity main thread (e.g. inside <c>TimeGatedSynchronizationContext.Pump</c>).
        /// Sync Instantiate is required because awaiting <see cref="GameObject.InstantiateAsync"/> inside a Pump callback deadlocks the player loop.
        /// </summary>
        public T GetOrCreateOnMainThread()
        {
            if (_objects.TryDequeue(out var obj))
                return obj;

            return UnityEngine.Object.Instantiate(_objectPrefab);
        }

        /// <summary>
        /// Instantiates until the pool reaches <paramref name="capacity"/>. Must run on the Unity main thread.
        /// </summary>
        public void PrefillOnMainThread(int capacity, CancellationToken cancellationToken = default)
        {
            while (_objects.Count < capacity && !cancellationToken.IsCancellationRequested)
            {
                _objects.Enqueue(UnityEngine.Object.Instantiate(_objectPrefab));
            }
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
    }
}
