using System.Threading;
using System.Threading.Tasks;

namespace Packages.de.kowl.utilities.Runtime.Kowl.Utils.ObjectPools
{
    public interface IObjectPool<T>
    {
        public Task FillToAsync(int capacity, CancellationToken cancellationToken = default);
        public T GetObject();
        /// <summary>Dequeue or sync-Instantiate on the Unity main thread (safe inside a time-gated Pump callback).</summary>
        public T GetOrCreateOnMainThread();
        /// <summary>Instantiate until the pool reaches capacity. Must run on the Unity main thread.</summary>
        public void PrefillOnMainThread(int capacity, CancellationToken cancellationToken = default);
        public Task<T> GetObjectAsync(CancellationToken cancellationToken = default);
        public void ReturnObject(T obj);
        public void SetRefillAmount(int amount);
        public int CurrentAmount();
    }
}
