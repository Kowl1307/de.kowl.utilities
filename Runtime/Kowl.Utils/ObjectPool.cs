using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace Kowl.Utils
{
    public class ObjectPool<T> : Object where T : UnityEngine.Object
    {
        private readonly ConcurrentQueue<T> _objects;
        private readonly T _objectPrefab;

        private int _refillAmount = 4;

        private Task fillTask;

        private const int _refillBatchSize = 2;

        public ObjectPool(T prefab, int fillBatchSize = 2)
        {
            _objects = new ConcurrentQueue<T>();
            _objectPrefab = prefab;
            _refillAmount = fillBatchSize;
        }

        public async Task FillToAsync(int capacity)
        {
            while (_objects.Count < capacity)
            {
                var objects = await InstantiateAsync(_objectPrefab, _refillBatchSize, Vector3.zero, Quaternion.identity);
                foreach (var o in objects)
                {
                    _objects.Enqueue(o);
                }
            }

            fillTask = null;
        }

        public async Task<T> GetObjectAsync()
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
            _refillAmount = amount;
        }

        public int CurrentAmount()
        {
            return _objects.Count;
        }
    }
}