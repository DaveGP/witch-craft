using System;
using Alchemy;
using Character.Interfaces;
using Data.ScriptableObjects;
using Interaction.Interfaces;
using UnityEngine;
using UserInterface;

namespace Events.Interfaces
{
	public interface IEventBus
	{
		event Action<IObserverTarget> ActiveTargetChanged;

		event Action<IInteractable> InteractionFocusChanged;

		event Action<string> Notified;

		event Action InventoryChanged;

		event Action<ItemDefinition, int> ItemCollected;

		event Action<GlyphDefinition> GlyphLearned;

		event Action<SpellDefinition> SpellLearned;

		event Action SpellSlotsChanged;

		event Action<RecipeDefinition> RecipeLearned;

		event Action<CompanionDefinition> CompanionRecruited;

		event Action<int> HomeLevelChanged;

		event Action<Transform> CameraOverrideChanged;

		event Action<AlchemyStation> AlchemyOpened;

		event Action<DialogueRequest> DialogueRequested;

		event Action PotionEffectsChanged;
	}
}
