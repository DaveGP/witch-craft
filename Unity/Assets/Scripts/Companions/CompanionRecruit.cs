using System.Collections.Generic;
using Companions.Interfaces;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using Interaction.Interfaces;
using Items.Interfaces;
using UnityEngine;
using UserInterface;
using Utilities;


namespace Companions
{
	// A magical creature met out in the world. Bring them what they ask for and they'll move into the witch's home.
	public class CompanionRecruit : MonoBehaviour, IInteractable
	{
		[SerializeField] private CompanionDefinition companion;

		private ICompanionRoster roster;

		private IInventory inventory;

		private IEventBusSender eventBus;

		public string Prompt => $"Talk to {companion.DisplayName}";

		public bool CanInteract => !roster.IsResident(companion);

		public Vector3 Position => transform.position;

		private void Awake()
		{
			roster = ServiceLocator.Get<ICompanionRoster>();
			inventory = ServiceLocator.Get<IInventory>();
			eventBus = ServiceLocator.Get<IEventBusSender>();

			if (roster == null)
				Debug.LogError($"{nameof(roster)} is null");

			if (companion == null)
				Debug.LogError($"{name}: {nameof(companion)} is null");
		}

		private void Start()
		{
			if (roster.IsResident(companion))
				gameObject.SetActive(false);
		}

		public void Interact(GameObject interactor)
		{
			FaceWitch(interactor.transform);

			var gift = companion.RecruitGift;

			var have = inventory.Count(gift.Item);

			var options = new List<DialogueOption>();

			if (have >= gift.Amount)
				options.Add(new DialogueOption($"Offer {gift} (you have {have})", MoveIn));

			options.Add(new DialogueOption("Chat", Chat, true));
			options.Add(new DialogueOption("Goodbye", null));

			var body = $"{companion.Greeting}\n\n{companion.Plea}";

			if (have < gift.Amount)
				body += $"\n\n<i>They would like {gift}. You have {have}.</i>";

			eventBus.RequestDialogue(new DialogueRequest($"{companion.DisplayName} the {companion.Species}", body, companion.Color, options));
		}

		private void Chat()
		{
			if (companion.ChatLines.Count == 0)
				return;

			eventBus.RequestDialogue(new DialogueRequest(companion.DisplayName,
				companion.ChatLines[Random.Range(0, companion.ChatLines.Count)],
				companion.Color,
				new List<DialogueOption> { new("Back", () => Interact(gameObject), true), new("Goodbye", null) }));
		}

		private void MoveIn()
		{
			if (!inventory.Remove(companion.RecruitGift.Item, companion.RecruitGift.Amount))
				return;

			eventBus.RequestDialogue(new DialogueRequest(companion.DisplayName, companion.MoveInLine, companion.Color,
				new List<DialogueOption> { new("See you at home!", null) }));

			roster.Recruit(companion);

			FxUtility.Burst(transform.position + Vector3.up * 0.5f, companion.Color, 50, 4f, 0.25f);

			gameObject.SetActive(false);
		}

		private void FaceWitch(Transform witch)
		{
			var look = witch.position - transform.position;
			look.y = 0f;

			if (look.sqrMagnitude > 0.01f)
				transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
		}
	}
}
