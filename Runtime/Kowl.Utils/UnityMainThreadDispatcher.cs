using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Kowl.Utils.MainThreadDispatcher;
using UnityEngine;

namespace Kowl.Utils
{
	public class UnityMainThreadDispatcher : MonoBehaviour {

		private static UnityMainThreadDispatcher _instance = null;

		private TimeGatedSynchronizationContext _syncContext;

		private const double TimeBudgetMs = 10;

		/// <summary>
		/// Posts an action to the main thread
		/// </summary>
		/// <param name="action">function that will be executed from the main thread.</param>
		/// <returns>A Task that can be awaited until the action completes</returns>
		public Task EnqueueAsync(Action action)
		{
			if (action == null)
				return Task.CompletedTask;

			var tcs = new TaskCompletionSource<object>();

			_syncContext.Post(_ =>
			{
				try
				{
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
		public Task<T> EnqueueAsync<T>(Func<T> func)
		{
			if (func == null)
				return Task.FromResult(default(T)!);

			var tcs = new TaskCompletionSource<T>();

			_syncContext.Post(_ =>
			{
				try
				{
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
		
		public static bool Exists() {
			return _instance != null;
		}

		public static UnityMainThreadDispatcher Instance() {
			if (!Exists ()) {
				throw new Exception ("UnityMainThreadDispatcher could not find the UnityMainThreadDispatcher object. Please ensure you have added the MainThreadExecutor Prefab to your scene.");
			}
			return _instance;
		}

		void Awake()
		{
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

		void OnDestroy() {
			_instance = null;
		}
	}
}