using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Kowl.Utils
{
	public class UnityMainThreadDispatcher : MonoBehaviour {

		private static UnityMainThreadDispatcher _instance = null;
		private static readonly ConcurrentQueue<Action> ExecutionQueue = new();

		private const int MaxProcessMilliseconds = 4;

		public void Update() {
			var startTime = Time.realtimeSinceStartup;
			while (true) {
				Action action = null;
				
				if (ExecutionQueue.Count > 0)
					 ExecutionQueue.TryDequeue(out action);
				
				if (action == null)
					break;
				action.Invoke();
				if ((Time.realtimeSinceStartup - startTime) * 1000f >= MaxProcessMilliseconds)
					break;
			}
		}

		/// <summary>
		/// Locks the queue and adds the Action to the queue
		/// </summary>
		/// <param name="action">function that will be executed from the main thread.</param>
		public void Enqueue(Action action)
		{
			if (action == null)
				return;
			
			ExecutionQueue.Enqueue(action);
		}

		/// <summary>
		/// Locks the queue and adds the Action to the queue, returning a Task which is completed when the action completes
		/// </summary>
		/// <param name="action">function that will be executed from the main thread.</param>
		/// <returns>A Task that can be awaited until the action completes</returns>
		public Task EnqueueAsync(Action action)
		{
			if (action == null)
				return null;
			
			var tcs = new TaskCompletionSource<bool>();

			void WrappedAction() {
				try 
				{
					action();
					tcs.TrySetResult(true);
				} catch (Exception ex) 
				{
					tcs.TrySetException(ex);
					Debug.LogError(ex);
				}
			}

			Enqueue(WrappedAction);
			return tcs.Task;
		}
		
		public Task<T> EnqueueAsync<T>(Func<T> func)
		{
			var tcs = new TaskCompletionSource<T>();

			void WrappedAction()
			{
				try
				{
					T result = func();
					tcs.TrySetResult(result);
				}
				catch (Exception ex)
				{
					tcs.TrySetException(ex);
				}
			}

			Enqueue(WrappedAction);
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


		void Awake() {
			if (_instance == null) {
				_instance = this;
				DontDestroyOnLoad(this.gameObject);
			}
		}

		void OnDestroy() {
			_instance = null;
		}
	}
}