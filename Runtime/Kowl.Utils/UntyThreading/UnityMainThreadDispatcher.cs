using Packages.de.kowl.utilities.Runtime.Kowl.Utils.MainThreadDispatcher;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Packages.de.kowl.utilities.Runtime.Kowl.Utils.UntyThreading
{
    [DefaultExecutionOrder(-10000)]
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance = null;

        private TimeGatedSynchronizationContext _syncContext;
        private CancellationTokenSource _lifetimeCts;
        private int _shuttingDown;

        private const double TimeBudgetMs = 10;

        private static Thread UnityMainThread { get; set; }

        /// <summary>
        /// Cancelled when Play Mode ends or this dispatcher is torn down.
        /// </summary>
        public CancellationToken CancellationToken =>
            _lifetimeCts?.Token ?? new CancellationToken(canceled: true);

        void Awake()
        {
            UnityMainThread ??= Thread.CurrentThread;
            if (_instance != null) return;

            _instance = this;

            DontDestroyOnLoad(gameObject);

            _lifetimeCts = new CancellationTokenSource();
            _syncContext = new TimeGatedSynchronizationContext(TimeBudgetMs);

            // SynchronizationContext.SetSynchronizationContext(_syncContext);
        }

#if UNITY_EDITOR
        void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Shutdown();
            }
        }
#endif

        public void Update()
        {
            if (_syncContext == null || Volatile.Read(ref _shuttingDown) != 0)
            {
                return;
            }

            _syncContext.Pump(CancellationToken);
        }

        void OnApplicationQuit()
        {
            Shutdown();
        }

        void OnDestroy()
        {
            Shutdown();
        }

        /// <summary>
        /// Cancels the lifetime token and clears the Time-Gate queue so pending Unity work does not run after exit.
        /// </summary>
        public void Shutdown()
        {
            if (Interlocked.Exchange(ref _shuttingDown, 1) != 0)
            {
                return;
            }

            try
            {
                _lifetimeCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            _syncContext?.Clear();

            if (_instance == this)
            {
                _instance = null;
            }

            try
            {
                _lifetimeCts?.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }

            _lifetimeCts = null;
        }

        public static bool AmIOnMainThread()
        {
            return Thread.CurrentThread.ManagedThreadId == UnityMainThread.ManagedThreadId;
        }

        /// <summary>
        /// Runs <paramref name="action"/> immediately when already on the Unity main thread;
        /// otherwise posts it through <see cref="TimeGatedSynchronizationContext"/> so <see cref="Update"/> budgets it.
        /// Completes as canceled when Play Mode ends or <paramref name="cancellationToken"/> fires.
        /// </summary>
        public Task ExecuteOrEnqueueIfNotMainThread(Action action, CancellationToken cancellationToken = default)
        {
            if (action == null)
                return Task.CompletedTask;

            if (IsShuttingDownOrCanceled(cancellationToken))
                return Task.FromCanceled(ResolveToken(cancellationToken));

            if (AmIOnMainThread())
            {
                if (IsShuttingDownOrCanceled(cancellationToken))
                    return Task.FromCanceled(ResolveToken(cancellationToken));

                action();
                return Task.CompletedTask;
            }

            return EnqueueAsync(action, cancellationToken);
        }

        /// <summary>
        /// Runs <paramref name="function"/> immediately when already on the Unity main thread;
        /// otherwise posts it through <see cref="TimeGatedSynchronizationContext"/> so <see cref="Update"/> budgets it.
        /// Completes as canceled when Play Mode ends or <paramref name="cancellationToken"/> fires.
        /// </summary>
        public Task<T> ExecuteOrEnqueueIfNotMainThread<T>(Func<T> function, CancellationToken cancellationToken = default)
        {
            if (function == null)
                return Task.FromResult(default(T)!);

            if (IsShuttingDownOrCanceled(cancellationToken))
                return Task.FromCanceled<T>(ResolveToken(cancellationToken));

            if (AmIOnMainThread())
            {
                if (IsShuttingDownOrCanceled(cancellationToken))
                    return Task.FromCanceled<T>(ResolveToken(cancellationToken));

                return Task.FromResult(function());
            }

            return EnqueueAsync(function, cancellationToken);
        }

        /// <summary>
        /// Posts an action to the main thread. Completes as canceled if Play Mode ends or <paramref name="cancellationToken"/> fires.
        /// </summary>
        public Task EnqueueAsync(Action action, CancellationToken cancellationToken = default)
        {
            if (action == null)
                return Task.CompletedTask;

            if (_syncContext == null || IsShuttingDownOrCanceled(cancellationToken))
                return Task.FromCanceled(ResolveToken(cancellationToken));

            var linked = CreateLinkedToken(cancellationToken, out var token);
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            var registration = token.CanBeCanceled
                ? token.Register(() => tcs.TrySetCanceled(token))
                : default;

            _syncContext.Post(_ =>
            {
                try
                {
                    if (token.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled(token);
                        return;
                    }

                    action();
                    tcs.TrySetResult(null);
                }
                catch (OperationCanceledException oce)
                {
                    tcs.TrySetCanceled(oce.CancellationToken);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                finally
                {
                    registration.Dispose();
                    linked?.Dispose();
                }
            }, null);

            return tcs.Task;
        }

        /// <summary>
        /// Posts a function to the main thread. Completes as canceled if Play Mode ends or <paramref name="token"/> fires.
        /// </summary>
        public Task<T> EnqueueAsync<T>(Func<T> func, CancellationToken token = default)
        {
            if (func == null)
                return Task.FromResult(default(T)!);

            if (_syncContext == null || IsShuttingDownOrCanceled(token))
                return Task.FromCanceled<T>(ResolveToken(token));

            var linked = CreateLinkedToken(token, out var combined);
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            var registration = combined.CanBeCanceled
                ? combined.Register(() => tcs.TrySetCanceled(combined))
                : default;

            _syncContext.Post(_ =>
            {
                try
                {
                    if (combined.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled(combined);
                        return;
                    }

                    var result = func();
                    tcs.TrySetResult(result);
                }
                catch (OperationCanceledException oce)
                {
                    tcs.TrySetCanceled(oce.CancellationToken);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
                finally
                {
                    registration.Dispose();
                    linked?.Dispose();
                }
            }, null);

            return tcs.Task;
        }

        public static bool Exists()
        {
            return _instance != null && Volatile.Read(ref _instance._shuttingDown) == 0;
        }

        public static UnityMainThreadDispatcher Instance()
        {
            if (!Exists())
            {
                throw new Exception("UnityMainThreadDispatcher could not find the UnityMainThreadDispatcher object. Please ensure you have added the MainThreadExecutor Prefab to your scene.");
            }
            return _instance;
        }

        private bool IsShuttingDownOrCanceled(CancellationToken cancellationToken)
        {
            return Volatile.Read(ref _shuttingDown) != 0
                   || _lifetimeCts == null
                   || _lifetimeCts.IsCancellationRequested
                   || cancellationToken.IsCancellationRequested;
        }

        private CancellationToken ResolveToken(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return cancellationToken;
            return _lifetimeCts?.Token ?? new CancellationToken(canceled: true);
        }

        private CancellationTokenSource CreateLinkedToken(CancellationToken cancellationToken, out CancellationToken combined)
        {
            var lifetime = _lifetimeCts;
            if (lifetime == null)
            {
                combined = new CancellationToken(canceled: true);
                return null;
            }

            if (!cancellationToken.CanBeCanceled)
            {
                combined = lifetime.Token;
                return null;
            }

            var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, cancellationToken);
            combined = linked.Token;
            return linked;
        }
    }
}
