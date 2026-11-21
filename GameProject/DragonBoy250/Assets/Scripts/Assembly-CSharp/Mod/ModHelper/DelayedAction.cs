using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Mod.ModHelper
{
	public static class DelayedAction
	{
		struct ScheduledEntry
		{
			public long executeAtTicks;
			public float intervalSeconds;
			public Action action;
			public int id;
		}

		static readonly ConcurrentQueue<ScheduledEntry> _pending = new ConcurrentQueue<ScheduledEntry>();
		static readonly ConcurrentQueue<int> _pendingCancellations = new ConcurrentQueue<int>();
		static readonly List<ScheduledEntry> _scheduled = new List<ScheduledEntry>();
		static readonly HashSet<int> _cancelledIds = new HashSet<int>();
		static int _nextId;

		static long SecondsToTicks(float seconds)
		{
			return seconds > 0f ? (long)(seconds * Stopwatch.Frequency) : 0L;
		}

		public static int Schedule(float delaySeconds, Action action)
		{
			int id = System.Threading.Interlocked.Increment(ref _nextId);
			_pending.Enqueue(new ScheduledEntry
			{
				executeAtTicks = Stopwatch.GetTimestamp() + SecondsToTicks(delaySeconds),
				intervalSeconds = -1f,
				action = action,
				id = id
			});
			return id;
		}

		public static int ScheduleRepeating(float intervalSeconds, Action action)
		{
			int id = System.Threading.Interlocked.Increment(ref _nextId);
			_pending.Enqueue(new ScheduledEntry
			{
				executeAtTicks = Stopwatch.GetTimestamp() + SecondsToTicks(intervalSeconds),
				intervalSeconds = intervalSeconds,
				action = action,
				id = id
			});
			return id;
		}

		public static void Cancel(int id)
		{
			_pendingCancellations.Enqueue(id);
		}

		internal static void Tick()
		{
			while (_pendingCancellations.TryDequeue(out int cancelledId))
			{
				_cancelledIds.Add(cancelledId);
			}

			while (_pending.TryDequeue(out ScheduledEntry pendingEntry))
			{
				_scheduled.Add(pendingEntry);
			}

			if (_scheduled.Count == 0)
				return;

			long nowTicks = Stopwatch.GetTimestamp();

			for (int i = _scheduled.Count - 1; i >= 0; i--)
			{
				ScheduledEntry entry = _scheduled[i];

				if (_cancelledIds.Remove(entry.id))
				{
					_scheduled.RemoveAt(i);
					continue;
				}

				if (nowTicks >= entry.executeAtTicks)
				{
					try
					{
						entry.action?.Invoke();
					}
					catch (Exception ex)
					{
						Debug.LogError("[DelayedAction] Error executing scheduled action: " + ex);
					}

					if (entry.intervalSeconds > 0f)
					{
						// Reschedule for next interval
						entry.executeAtTicks = nowTicks + SecondsToTicks(entry.intervalSeconds);
						_scheduled[i] = entry;
					}
					else
					{
						_scheduled.RemoveAt(i);
					}
				}
			}
		}
	}
}