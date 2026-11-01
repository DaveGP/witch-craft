using System;
using Character;
using Data.ScriptableObjects;
using DependencyInjection;
using Items.Interfaces;
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

		private IInventory inventory;

		private Collider[] colliders;

		private bool available = true;

		public string Prompt => $"{verb} {item.DisplayName}";

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
		}

		public void Interact(GameObject interactor)
		{
			Collect();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (!collectOnTouch || !available || other.GetComponentInParent<PlayerCharacter>() == null)
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
				Invoke(nameof(Regrow), respawnSeconds);
		}

		public void Regrow()
		{
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
