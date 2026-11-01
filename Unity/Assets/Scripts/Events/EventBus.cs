using System;
using Alchemy;
using Character.Interfaces;
using Data.ScriptableObjects;
using Interaction.Interfaces;
using UnityEngine;
using UserInterface;


namespace Events
{
	using Interfaces;

	public class EventBus : IEventBusSender
	{
		public event Action<IObserverTarget> ActiveTargetChanged;

		public event Action<IInteractable> InteractionFocusChanged;

		public event Action<string> Notified;

		public event Action InventoryChanged;

		public event Action<ItemDefinition, int> ItemCollected;

		public event Action<GlyphDefinition> GlyphLearned;

		public event Action<SpellDefinition> SpellLearned;

		public event Action SpellSlotsChanged;

		public event Action<RecipeDefinition> RecipeLearned;

		public event Action<CompanionDefinition> CompanionRecruited;

		public event Action<int> HomeLevelChanged;

		public event Action<Transform> CameraOverrideChanged;

		public event Action<AlchemyStation> AlchemyOpened;

		public event Action<DialogueRequest> DialogueRequested;

		public event Action PotionEffectsChanged;


		public void SetActiveTarget(IObserverTarget target)
			=> ActiveTargetChanged?.Invoke(target);

		public void SetInteractionFocus(IInteractable interactable)
			=> InteractionFocusChanged?.Invoke(interactable);

		public void Notify(string message)
			=> Notified?.Invoke(message);

		public void InventoryUpdated()
			=> InventoryChanged?.Invoke();

		public void CollectItem(ItemDefinition item, int amount)
			=> ItemCollected?.Invoke(item, amount);

		public void LearnGlyph(GlyphDefinition glyph)
			=> GlyphLearned?.Invoke(glyph);

		public void LearnSpell(SpellDefinition spell)
			=> SpellLearned?.Invoke(spell);

		public void UpdateSpellSlots()
			=> SpellSlotsChanged?.Invoke();

		public void LearnRecipe(RecipeDefinition recipe)
			=> RecipeLearned?.Invoke(recipe);

		public void RecruitCompanion(CompanionDefinition companion)
			=> CompanionRecruited?.Invoke(companion);

		public void ChangeHomeLevel(int level)
			=> HomeLevelChanged?.Invoke(level);

		public void SetCameraOverride(Transform viewpoint)
			=> CameraOverrideChanged?.Invoke(viewpoint);

		public void OpenAlchemy(AlchemyStation station)
			=> AlchemyOpened?.Invoke(station);

		public void RequestDialogue(DialogueRequest request)
			=> DialogueRequested?.Invoke(request);

		public void UpdatePotionEffects()
			=> PotionEffectsChanged?.Invoke();
	}
}
