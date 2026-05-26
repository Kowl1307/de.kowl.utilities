using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Packages.de.kowl.utilities.Runtime.Kowl.Utils
{
    public class ObjectPool<T> : Object where T : UnityEngine.Object
    {
        private readonly ConcurrentQueue<T> _objects;
        private readonly T _objectPrefab;

        private Task fillTask;

        private int _refillBatchSize;

        public ObjectPool(T prefab, int fillBatchSize = 2)
        {
            _objects = new ConcurrentQueue<T>();
            _objectPrefab = prefab;
            _refillBatchSize = fillBatchSize;
        }

        public async Task FillToAsync(int capacity, CancellationToken cancellationToken = default)
        {
            while (_objects.Count < capacity && !cancellationToken.IsCancellationRequested)
            {
                var objects = await InstantiateAsync(_objectPrefab, _refillBatchSize, null, Vector3.zero, Quaternion.identity, cancellationToken);
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

        public void Clear()
        {
            _objects.Clear();
        }

        public void SetRefillAmount(int amount)
        {
            _refillBatchSize = amount;
        }

        public int CurrentAmount()
        {
            return _objects.Count;
        }
    }
}