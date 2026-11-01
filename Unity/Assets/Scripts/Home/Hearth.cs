using System.Collections.Generic;
using System.Linq;
using DependencyInjection;
using Events.Interfaces;
using Home.Interfaces;
using Interaction.Interfaces;
using Persistence.Interfaces;
using UnityEngine;
using UserInterface;


namespace Home
{
	// The hearthstone at the heart of the home: rest here to save, and spend materials to expand the plot.
	public class Hearth : MonoBehaviour, IInteractable
	{
		private IHomeService homeService;

		private ISaveService saveService;

		private IEventBusSender eventBus;

		public string Prompt => "Sit by the hearth";

		public bool CanInteract => true;

		public Vector3 Position => transform.position;

		private void Start()
		{
			homeService = ServiceLocator.Get<IHomeService>();
			saveService = ServiceLocator.Get<ISaveService>();
			eventBus = ServiceLocator.Get<IEventBusSender>();

			if (homeService == null)
				Debug.LogError($"{nameof(homeService)} is null");
		}

		public void Interact(GameObject interactor)
		{
			var next = homeService.NextLevel;

			var body = $"Your home is level {homeService.Level}, with a comfort of {homeService.Comfort}. Cosier homes make companions fonder of you each morning. ";

			body += next == null
				? "It has grown as large as it can."
				: $"To expand to level {homeService.Level + 1}: {string.Join(", ", next.Cost.Select(stack => stack.ToString()))}"
				  + (next.RequiredCompanions > 0 ? $", and {next.RequiredCompanions} companions living here." : ".");

			var options = new List<DialogueOption>
			{
				new("Rest a while (save)", () =>
				{
					saveService.Save();
					eventBus.Notify("You rest by the fire. Game saved.");
				})
			};

			if (next != null)
				options.Add(new DialogueOption("Expand my home", () => homeService.Expand()));

			options.Add(new DialogueOption("Change my name", () => eventBus.RequestRename()));

			options.Add(new DialogueOption("Leave", null));

			eventBus.RequestDialogue(new DialogueRequest("The Hearth", body, new Color(1f, 0.6f, 0.3f), options));
		}
	}
}
