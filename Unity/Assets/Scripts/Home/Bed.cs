using System.Collections.Generic;
using DependencyInjection;
using Events.Interfaces;
using Interaction.Interfaces;
using Persistence.Interfaces;
using Sky;
using Sky.Interfaces;
using UnityEngine;
using UserInterface;
using Utilities;


namespace Home
{
	// Sleep through the night (or nap until evening, for night flowers and an owl's company). Saves the game.
	public class Bed : MonoBehaviour, IInteractable
	{
		private const float EveningHour = DayCycle.DuskHour;

		public string Prompt => "Lie down";

		public bool CanInteract => true;

		public Vector3 Position => transform.position;

		public void Interact(GameObject interactor)
		{
			var dayCycle = ServiceLocator.Get<IDayCycle>();
			var eventBus = ServiceLocator.Get<IEventBusSender>();

			if (dayCycle == null)
				return;

			var options = new List<DialogueOption>();

			if (dayCycle.IsNight)
				options.Add(new DialogueOption("Sleep until morning", () => Sleep(DayCycle.DawnHour + 1f, "You wake to birdsong, warm and rested.")));
			else
				options.Add(new DialogueOption("Nap until evening", () => Sleep(EveningHour, "You stir as the sky turns to violet. The night flowers will be opening.")));

			options.Add(new DialogueOption("Not yet", null));

			var body = dayCycle.IsNight
				? "The quilt is soft and Sable is already curled at the foot of the bed."
				: "A nap does sound lovely.";

			eventBus.RequestDialogue(new DialogueRequest("Cosy Bed", body, new Color(0.6f, 0.45f, 0.75f), options));
		}

		private void Sleep(float hour, string message)
		{
			var dayCycle = ServiceLocator.Get<IDayCycle>();

			dayCycle.SkipTo(hour);

			ServiceLocator.Get<ISaveService>()?.Save();

			FxUtility.Burst(transform.position + Vector3.up, new Color(0.75f, 0.7f, 1f), 30, 1.5f, 0.25f);

			ServiceLocator.Get<IEventBusSender>().Notify($"Day {dayCycle.Day}. {message}");
		}
	}
}
