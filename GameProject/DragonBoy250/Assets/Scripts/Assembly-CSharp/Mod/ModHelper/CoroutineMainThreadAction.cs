using System.Collections;
using UnityEngine;

namespace Mod.ModHelper
{
	public abstract class CoroutineMainThreadAction<T> : MonoBehaviour
		where T : CoroutineMainThreadAction<T>
	{
		Coroutine runningCoroutine;
		Coroutine activeUpdateCoroutine;

		public static T gI { get; private set; }

		protected abstract float Interval { get; }

		public bool IsActing { get; private set; }

		protected virtual void Awake()
		{
			if (gI == null)
			{
				gI = (T)this;
				DontDestroyOnLoad(gameObject);
			}
			else
			{
				Destroy(gameObject);
			}
		}

		protected abstract IEnumerator OnUpdate();
		
		protected virtual void OnStart() { }
		
		protected virtual void OnStop() { }
		
		public void Toggle(bool value) => Toggle((bool?)value);

		protected void Toggle(bool? value = null)
		{
			if (value == null && IsActing || value == false)
			{
				StopAction();
			}
			else if (value == null && !IsActing || value == true)
			{
				StartAction();
			}
		}
		

		void StartAction()
		{
			if (IsActing) return;

			IsActing = true;

			OnStart();

			runningCoroutine = StartCoroutine(Run());
		}

		void StopAction()
		{
			if (!IsActing) return;

			IsActing = false;

			if (activeUpdateCoroutine != null)
			{
				StopCoroutine(activeUpdateCoroutine);
				activeUpdateCoroutine = null;
			}

			if (runningCoroutine != null)
			{
				StopCoroutine(runningCoroutine);
				runningCoroutine = null;
			}

			OnStop();
		}

		IEnumerator Run()
		{
			while (IsActing)
			{
				activeUpdateCoroutine = StartCoroutine(OnUpdate());
				yield return activeUpdateCoroutine;
				activeUpdateCoroutine = null;

				if (Interval <= 0f)
				{
					yield return null;
				}
				else
				{
					yield return new WaitForSecondsRealtime(Interval);
				}
			}

			runningCoroutine = null;
		}
	}
}
