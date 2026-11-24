using System;
using System.Linq;
using Mod.ModHelper;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Mod
{
	public class ModScript : MonoBehaviour
	{
		void Awake()
		{
			GameEvents.OnAwake();
		}

		void Start()
		{
			GameEvents.OnGameStart();
		}
		
		private static bool _bootstrapped;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void Init()
		{
			if (_bootstrapped)
				return;
			_bootstrapped = true;
			var modHost = new GameObject(nameof(ModScript));
			modHost.AddComponent<ModScript>();
			Object.DontDestroyOnLoad(modHost);

			var types = AppDomain.CurrentDomain
				.GetAssemblies()
				.SelectMany(a => a.GetTypes())
				.Where(t =>
					!t.IsAbstract &&
					t.BaseType != null &&
					t.BaseType.IsGenericType &&
					t.BaseType.GetGenericTypeDefinition() == typeof(CoroutineMainThreadAction<>));

			foreach (var type in types)
			{
				var obj = new GameObject(type.Name);
				obj.AddComponent(type);
				Object.DontDestroyOnLoad(obj);
			}
		}
	}
}