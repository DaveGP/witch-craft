using Companions.Interfaces;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using Home.Interfaces;
using UnityEngine;
using Utilities;


namespace Companions
{
	// Brings resident companions to life on the home plot, including any who move in mid-session.
	public class CompanionSpawner : MonoBehaviour
	{
		private ICompanionRoster roster;

		private IEventBus eventBus;

		private void Start()
		{
			roster = ServiceLocator.Get<ICompanionRoster>();
			eventBus = ServiceLocator.Get<IEventBus>();

			if (roster == null)
				Debug.LogError($"{nameof(roster)} is null");

			foreach (var companion in roster.Residents)
				Spawn(companion, false);

			eventBus.CompanionRecruited += SpawnNew;
		}

		private void OnDestroy()
		{
			if (eventBus != null)
				eventBus.CompanionRecruited -= SpawnNew;
		}

		private void SpawnNew(CompanionDefinition companion) => Spawn(companion, true);

		private void Spawn(CompanionDefinition companion, bool announce)
		{
			var home = ServiceLocator.Get<IHomeService>();

			if (companion.Prefab == null || home == null)
				return;

			var position = home.Plot.RandomPoint(2f);

			if (Physics.Raycast(position + Vector3.up * 10f, Vector3.down, out var hit, 20f, LayerMask.GetMask("Ground"), QueryTriggerInteraction.Ignore))
				position = hit.point;

			var instance = Instantiate(companion.Prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);

			instance.name = $"{companion.DisplayName} (Resident)";

			// Prefabs are shared with the world recruit; swap its behaviour for the resident one
			foreach (var recruit in instance.GetComponentsInChildren<CompanionRecruit>())
				DestroyImmediate(recruit);

			instance.AddComponent<CompanionResident>().Initialise(companion);

			if (announce)
				FxUtility.Burst(position + Vector3.up * 0.5f, companion.Color, 40, 3f);
		}
	}
}
