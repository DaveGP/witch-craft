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
	// A magical creature met out in the world. Do what they ask and they'll move into the witch's home. Some stay out
	// of the world until the storybook that leads to them has been read.
	public class CompanionRecruit : MonoBehaviour, IInteractable
	{
		[SerializeField] private CompanionDefinition companion;

		private ICompanionRoster roster;

		private IInventory inventory;

		private IEventBusSender eventBus;

		private IQuestLog quests;

		private Vector3 home;

		private bool hidden;

		public CompanionDefinition Companion => companion;

		public string Prompt => $"Talk to {companion.DisplayName}";

		public bool CanInteract => !hidden && !roster.IsResident(companion);

		public Vector3 Position => transform.position;

		private void Awake()
		{
			roster = ServiceLocator.Get<ICompanionRoster>();
			inventory = ServiceLocator.Get<IInventory>();
			eventBus = ServiceLocator.Get<IEventBusSender>();
			quests = ServiceLocator.Get<IQuestLog>();

			if (roster == null)
				Debug.LogError($"{nameof(roster)} is null");

			if (companion == null)
				Debug.LogError($"{name}: {nameof(companion)} is null");
		}

		private void Start()
		{
			home = transform.position;

			if (roster.IsResident(companion))
				gameObject.SetActive(false);
			else
				UpdateHiding();
		}

		private void Update() => UpdateHiding();

		// Parked far below the world rather than switched off, so the art and everything else on them stays ready
		private void UpdateHiding()
		{
			var hide = !quests.IsRevealed(companion);

			if (hide == hidden)
				return;

			hidden = hide;
			transform.position = hide ? home + Vector3.down * 500f : home;
		}

		public void Interact(GameObject interactor)
		{
			FaceWitch(interactor.transform);

			if (companion.Quests.Count > 0)
			{
				Converse();
				return;
			}

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

		// Earned rather than bribed: a first meeting explains, and each return hands in or reminds
		private void Converse()
		{
			var firstMeeting = !quests.HasStarted(companion);

			quests.Begin(companion);

			var step = quests.Current(companion);

			var body = firstMeeting
				? $"{companion.Greeting}\n\n{companion.Plea}\n\n{step.Ask}"
				: quests.IsMet(step) ? companion.Expectant : step.Ask;

			ShowStep(body, step);
		}

		private void ShowStep(string body, QuestStep step)
		{
			var options = new List<DialogueOption>();

			if (quests.IsMet(step))
				options.Add(new DialogueOption(step.Offer, TurnIn, true));

			options.Add(new DialogueOption("Chat", Chat, true));
			options.Add(new DialogueOption("Goodbye", null));

			eventBus.RequestDialogue(new DialogueRequest($"{companion.DisplayName} the {companion.Species}", body, companion.Color, options));
		}

		private void TurnIn()
		{
			var thanks = quests.Current(companion).Thanks;

			if (!quests.TurnIn(companion))
				return;

			var next = quests.Current(companion);

			if (next == null)
			{
				Welcome($"{thanks}\n\n{companion.MoveInLine}");
				return;
			}

			ShowStep($"{thanks}\n\n{next.Ask}", next);
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

			Welcome(companion.MoveInLine);
		}

		private void Welcome(string line)
		{
			eventBus.RequestDialogue(new DialogueRequest(companion.DisplayName, line, companion.Color,
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
