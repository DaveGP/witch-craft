using System;
using Character;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using Items.Interfaces;
using Sky.Interfaces;
using UnityEngine;
using Utilities;


namespace Interaction
{
	using Interfaces;

	// A herb, mushroom, log or stone the witch can pick up. Regrows after a while; water sources never run out.
	public class Gatherable : MonoBehaviour, IInteractable
	{
		public event Action Collected;

		[SerializeField] private ItemDefinition item;

		[SerializeField] private int amount = 1;

		[Tooltip("Seconds until it grows back. 0 never runs out, negative never comes back on its own")]
		[SerializeField] private float respawnSeconds = 120f;

		[SerializeField] private bool collectOnTouch;

		[SerializeField] private string verb = "Gather";

		[SerializeField] private GameObject visual;

		[Tooltip("Only opens, and can only be gathered, at night")]
		[SerializeField] private bool nightOnly;

		private IInventory inventory;

		private Collider[] colliders;

		private bool available = true;

		private float regrowRemaining;

		private Vector3 visualScale;

		private IDayCycle dayCycle;

		private IWeather weather;

		private IDayCycle DayCycle => dayCycle ??= ServiceLocator.Get<IDayCycle>();

		private IWeather Weather => weather ??= ServiceLocator.Get<IWeather>();

		private bool InSeason => !nightOnly || DayCycle == null || DayCycle.IsNight;

		public string Prompt => InSeason ? $"{verb} {item.DisplayName}" : $"{item.DisplayName} (opens at night)";

		public bool CanInteract => available && !collectOnTouch;

		public Vector3 Position => transform.position;

		private void Awake()
		{
			inventory = ServiceLocator.Get<IInventory>();

			if (inventory == null)
				Debug.LogError($"{nameof(inventory)} is null");

			if (item == null)
				Debug.LogError($"{name}: {nameof(item)} is null");

			colliders = GetComponents<Collider>();

			if (visual != null)
				visualScale = visual.transform.localScale;
		}

		public void Interact(GameObject interactor)
		{
			if (!InSeason)
			{
				ServiceLocator.Get<IEventBusSender>()?.Notify($"The {item.DisplayName} is closed up tight. Come back after dusk.");
				return;
			}

			Collect();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!collectOnTouch || !available || !InSeason || other.GetComponentInParent<PlayerCharacter>() == null)
				return;

			Collect();
		}

		private void Collect()
		{
			if (!available)
				return;

			inventory.Add(item, amount);

			FxUtility.Burst(transform.position + Vector3.up * 0.4f, item.Color, 14, 2f);

			Collected?.Invoke();

			if (respawnSeconds == 0f)
				return;

			SetAvailable(false);

			if (respawnSeconds > 0f)
				regrowRemaining = respawnSeconds;
		}

		private void Update()
		{
			UpdateRegrowth();

			UpdateBloom();
		}

		// Rain makes things grow back twice as fast
		private void UpdateRegrowth()
		{
			if (regrowRemaining <= 0f)
				return;

			regrowRemaining -= Time.deltaTime * (Weather != null && Weather.IsRaining ? 2f : 1f);

			if (regrowRemaining <= 0f)
				Regrow();
		}

		// Night flowers close up into small buds by day
		private void UpdateBloom()
		{
			if (!nightOnly || visual == null || !available)
				return;

			var target = InSeason ? 1f : 0.35f;

			var scale = visual.transform.localScale.x / Mathf.Max(0.0001f, visualScale.x);

			visual.transform.localScale = visualScale * Mathf.MoveTowards(scale, target, Time.deltaTime * 0.5f);
		}

		public void Regrow()
		{
			regrowRemaining = 0f;

			SetAvailable(true);
		}

		private void SetAvailable(bool value)
		{
			available = value;

			if (visual != null)
				visual.SetActive(value);

			foreach (var collider in colliders)
				collider.enabled = value;
		}
	}
}
