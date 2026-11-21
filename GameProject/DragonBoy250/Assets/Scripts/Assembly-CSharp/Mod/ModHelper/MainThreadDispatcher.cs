using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Mod.ModHelper
{
	public class MainThreadDispatcher : MonoBehaviour
	{
		static readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

		void Update()
		{
			while (queue.TryDequeue(out Action action))
			{
				if (action == null)
					continue;
				try
				{
					action();
				}
				catch (Exception ex)
				{
					UnityEngine.Debug.LogException(ex);
				}
			}
		}

		public static void Dispatch(Action action)
		{
			if (action == null)
				return;
			queue.Enqueue(action);
		}
	}
}
