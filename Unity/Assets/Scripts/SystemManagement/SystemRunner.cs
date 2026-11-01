using UnityEngine;
using DependencyInjection;


namespace SystemManagement
{
	[DefaultExecutionOrder(-100)]
	public class SystemsRunner : MonoBehaviour
	{
		private SystemManager systemManager;

		private void Awake()
		{
			systemManager = ServiceLocator.Get<SystemManager>();

			if (systemManager == null)
				Debug.LogError($"{nameof(systemManager)} is null");
		}

		private void Update()
		{
			systemManager.Update();
		}

		private void FixedUpdate()
		{
			systemManager.FixedUpdate();
		}

		private void LateUpdate()
		{
			systemManager.LateUpdate();
		}
	}
}
