using Packages.de.kowl.utilities.Runtime.Kowl.Utils.MainThreadDispatcher;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Packages.de.kowl.utilities.Runtime.Kowl.Utils.UntyThreading
{
    public class UnityMainThreadDispatcher : MonoBehaviour
    {

        private static UnityMainThreadDispatcher _instance = null;

        private TimeGatedSynchronizationContext _syncContext;

        private const double TimeBudgetMs = 10;

        private static Thread UnityMainThread { get; set; }

        void Awake()
        {
            UnityMainThread ??= Thread.CurrentThread;
            if (_instance != null) return;

            _instance = this;

            DontDestroyOnLoad(gameObject);

            _syncContext = new TimeGatedSynchronizationContext(TimeBudgetMs);

            // SynchronizationContext.SetSynchronizationContext(_syncContext);
        }

        public void Update()
        {
            _syncContext.Pump();
        }

        void OnDestroy()
        {
            _instance = null;
        }

        public static bool AmIOnMainThread()
        {
            return Thread.CurrentThread.ManagedThreadId == UnityMainThread.ManagedThreadId;
        }

        public async Task ExecuteOrEnqueueIfNotMainThread(Action action)
        {
            if (!AmIOnMainThread())
            {
                await Awaitable.MainThreadAsync();
            }

            action();
        }

        public async Task<T> ExecuteOrEnqueueIfNotMainThread<T>(Func<T> function)
        {
            if (!AmIOnMainThread())
            {
                await Awaitable.MainThreadAsync();
            }
            return function();
        }

        /// <summary>
        /// Posts an action to the main thread
        /// </summary>
        /// <param name="action">function that will be executed from the main thread.</param>
        /// <returns>A Task that can be awaited until the action completes</returns>
        public Task EnqueueAsync(Action action, CancellationToken cancellationToken = default)
        {
            if (action == null)
                return Task.CompletedTask;

            var tcs = new TaskCompletionSource<object>();

            _syncContext.Post(_ =>
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        tcs.SetResult(null);
                        return;
                    }
                    action();
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);

            return tcs.Task;
        }

        /// <summary>
        /// Posts a function to the main thread. The result can be awaited.
        /// </summary>
        /// <param name="func"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public Task<T> EnqueueAsync<T>(Func<T> func, CancellationToken token = default)
        {
            if (func == null)
                return Task.FromResult(default(T)!);

            var tcs = new TaskCompletionSource<T>();

            _syncContext.Post(_ =>
            {
                try
                {
                    if (token.IsCancellationRequested)
                    {
                        tcs.SetResult(default);
                        return;
                    }

                    var result = func();
                    tcs.SetResult(result);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, null);

            return tcs.Task;
        }

        public static bool Exists()
        {
            return _instance != null;
        }

        public static UnityMainThreadDispatcher Instance()
        {
            if (!Exists())
            {
                throw new Exception("UnityMainThreadDispatcher could not find the UnityMainThreadDispatcher object. Please ensure you have added the MainThreadExecutor Prefab to your scene.");
            }
            return _instance;
        }

    }
}