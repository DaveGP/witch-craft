using Audio;
using Audio.Interfaces;
using Companions.Interfaces;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using Items.Interfaces;
using UnityEngine;
using Utilities;


namespace Interaction
{
	using Interfaces;

	// Something a companion has asked the witch to find. It only turns up while that step is under way.
	public class QuestPickup : MonoBehaviour, IInteractable
	{
		[SerializeField] private CompanionDefinition companion;

		[SerializeField] private int step;

		[SerializeField] private GameObject visual;

		private IQuestLog quests;

		private IInventory inventory;

		private ItemDefinition Item => companion.Quests[step].Needs.Item;

		private bool Wanted => quests.HasStarted(companion) && quests.StepIndex(companion) == step && inventory.Count(Item) == 0;

		public string Prompt => $"Pick up {Item.DisplayName}";

		public bool CanInteract => Wanted;

		public Vector3 Position => transform.position;

		private void Awake()
		{
			quests = ServiceLocator.Get<IQuestLog>();
			inventory = ServiceLocator.Get<IInventory>();

			if (quests == null)
				Debug.LogError($"{nameof(quests)} is null");

			if (companion == null)
				Debug.LogError($"{name}: {nameof(companion)} is null");
		}

		private void Update()
		{
			if (visual != null && visual.activeSelf != Wanted)
				visual.SetActive(Wanted);
		}

		public void Interact(GameObject interactor)
		{
			if (!Wanted)
				return;

			inventory.Add(Item, 1);

			FxUtility.Burst(transform.position + Vector3.up * 0.4f, Item.Color, 30, 2.5f, 0.15f);

			ServiceLocator.Get<IAudioService>()?.Play(Sound.Learn, 0.6f, 0f);

			ServiceLocator.Get<IEventBusSender>()?.Notify($"You found {Item.DisplayName}. Take it back to {companion.DisplayName}.");
		}
	}
}
