using System.Collections.Generic;
using System.Linq;
using Companions.Interfaces;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using Home.Interfaces;
using Interaction.Interfaces;
using Items.Interfaces;
using Sky.Interfaces;
using UnityEngine;
using UserInterface;
using Utilities;


namespace Companions
{
	// A companion who lives at the witch's home. Potters about the plot, chats, and grows fonder with gifts,
	// sharing recipes and glyphs as friendship deepens.
	public class CompanionResident : MonoBehaviour, IInteractable
	{
		private const float ChatCooldown = 45f;

		private CompanionDefinition companion;

		private ICompanionRoster roster;

		private IInventory inventory;

		private IEventBusSender eventBus;

		private IHomeService home;

		private Vector3 destination;

		private float waitUntil;

		private float nextChatFriendship;

		private float talkingUntil;

		private Transform witch;

		private LayerMask groundMask;

		private IDayCycle dayCycle;

		private float nextSnore;

		/// <summary>
		/// Residents sleep at night, except nocturnal ones, who sleep by day.
		/// </summary>
		public bool IsAsleep => dayCycle != null && dayCycle.IsNight != companion.Nocturnal;

		public string Prompt => $"Visit with {companion.DisplayName}";

		public bool CanInteract => true;

		public Vector3 Position => transform.position;

		public void Initialise(CompanionDefinition definition)
		{
			companion = definition;
		}

		private void Start()
		{
			roster = ServiceLocator.Get<ICompanionRoster>();
			inventory = ServiceLocator.Get<IInventory>();
			eventBus = ServiceLocator.Get<IEventBusSender>();
			home = ServiceLocator.Get<IHomeService>();
			dayCycle = ServiceLocator.Get<IDayCycle>();

			// Ground only, so they walk round the house rather than over its roof
			groundMask = LayerMask.GetMask("Ground");

			destination = transform.position;
		}

		public void Interact(GameObject interactor)
		{
			witch = interactor.transform;

			if (IsAsleep)
			{
				var when = companion.Nocturnal ? "nightfall" : "morning";

				eventBus.RequestDialogue(new DialogueRequest($"{companion.DisplayName} the {companion.Species}",
					$"{companion.DisplayName} is curled up fast asleep, snoring softly. Best visit again in the {when}.",
					companion.Color, new List<DialogueOption> { new("Tiptoe away", null) }));

				return;
			}

			talkingUntil = Time.time + 4f;

			var friendship = roster.Friendship(companion);

			var home = ServiceLocator.Get<IHomeService>();

			var line = companion.ChatLines.Count > 0 ? companion.ChatLines[Random.Range(0, companion.ChatLines.Count)] : companion.Greeting;

			var body = $"{line}\n\n<i>Friendship: {Hearts(friendship)}</i>";

			var next = companion.Rewards.Where(reward => reward.Friendship > friendship).Select(reward => reward.Friendship).DefaultIfEmpty(0).Min();

			if (next > 0)
				body += $"<i>  ({next - friendship} more until they share a secret)</i>";

			var favourite = companion.FavouriteFurniture;

			if (favourite != null && home != null)
				body += home.Has(favourite)
					? $"\n<i>They adore your {favourite.DisplayName}.</i>"
					: $"\n<i>They keep mentioning how lovely a {favourite.DisplayName} would be.</i>";

			if (Time.time >= nextChatFriendship)
			{
				nextChatFriendship = Time.time + ChatCooldown;

				// A comfortable home makes for warmer chats
				Grant(home != null && home.Comfort >= 12 ? 2 : 1);
			}

			var options = companion.LikedGifts
				.Where(item => inventory.Count(item) > 0)
				.Select(item => new DialogueOption($"Give {item.DisplayName} ({inventory.Count(item)})", () => Give(item)))
				.ToList();

			options.Add(new DialogueOption("Goodbye", null));

			eventBus.RequestDialogue(new DialogueRequest($"{companion.DisplayName} the {companion.Species}", body, companion.Color, options));
		}

		private void Give(ItemDefinition item)
		{
			if (!inventory.Remove(item, 1))
				return;

			FxUtility.Burst(transform.position + Vector3.up * 0.8f, new Color(1f, 0.5f, 0.7f), 20, 2f, 0.22f);

			var comfort = ServiceLocator.Get<IHomeService>()?.Comfort ?? 0;

			var rewards = Grant(3 + Mathf.Min(2, comfort / 15));

			var body = $"\"Oh! {item.DisplayName}! You remembered!\"";

			foreach (var reward in rewards)
				body += $"\n\n{reward.Line}";

			eventBus.RequestDialogue(new DialogueRequest(companion.DisplayName, body, companion.Color,
				new List<DialogueOption> { new("Visit some more", () => Interact(witch.gameObject), true), new("Goodbye", null) }));
		}

		private IReadOnlyList<FriendshipReward> Grant(int amount)
			=> roster.AddFriendship(companion, amount);

		private static string Hearts(int friendship)
		{
			var hearts = Mathf.Clamp(friendship / 3, 0, 10);

			return new string('♥', hearts) + new string('·', 10 - hearts);
		}

		private void Update()
		{
			if (companion == null || home == null)
				return;

			if (IsAsleep)
			{
				Sleep();
				return;
			}

			if (Time.time < talkingUntil && witch != null)
			{
				Face(witch.position - transform.position);
				return;
			}

			Wander();
		}

		// Stay put and let little z's drift up now and then
		private void Sleep()
		{
			transform.GetChild(0).localPosition = Vector3.up * (Mathf.Sin(Time.time * 1.5f) * 0.02f);

			if (Time.time < nextSnore)
				return;

			nextSnore = Time.time + Random.Range(2f, 3.5f);

			FxUtility.Burst(transform.position + Vector3.up * 1f, new Color(0.85f, 0.85f, 1f), 3, 0.6f, 0.15f);
		}

		private void Wander()
		{
			var toDestination = destination - transform.position;
			toDestination.y = 0f;

			if (toDestination.magnitude < 0.3f)
			{
				if (Time.time < waitUntil)
					return;

				destination = home.Plot.RandomPoint(1.5f);
				waitUntil = Time.time + Random.Range(3f, 9f);
				return;
			}

			var step = toDestination.normalized * (1.4f * Time.deltaTime);

			var next = transform.position + step;

			if (Physics.Raycast(next + Vector3.up * 3f, Vector3.down, out var hit, 6f, groundMask, QueryTriggerInteraction.Ignore))
				next.y = hit.point.y;

			// A gentle hop as they walk
			transform.position = next;
			transform.GetChild(0).localPosition = Vector3.up * (Mathf.Abs(Mathf.Sin(Time.time * 7f)) * 0.08f);

			Face(toDestination);
		}

		private void Face(Vector3 direction)
		{
			direction.y = 0f;

			if (direction.sqrMagnitude > 0.01f)
				transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction.normalized), 1f - Mathf.Exp(-6f * Time.deltaTime));
		}
	}
}
