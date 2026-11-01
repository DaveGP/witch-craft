using Alchemy;
using Character.Interfaces;
using Data.ScriptableObjects;
using Interaction.Interfaces;
using UnityEngine;
using UserInterface;

namespace Events.Interfaces
{
	public interface IEventBusSender : IEventBus
	{
		public void SetActiveTarget(IObserverTarget target);

		public void SetInteractionFocus(IInteractable interactable);

		public void Notify(string message);

		public void InventoryUpdated();

		public void CollectItem(ItemDefinition item, int amount);

		public void LearnGlyph(GlyphDefinition glyph);

		public void LearnSpell(SpellDefinition spell);

		public void UpdateSpellSlots();

		public void LearnRecipe(RecipeDefinition recipe);

		public void RecruitCompanion(CompanionDefinition companion);

		public void ChangeHomeLevel(int level);

		public void SetCameraOverride(Transform viewpoint);

		public void OpenAlchemy(AlchemyStation station);

		public void RequestDialogue(DialogueRequest request);

		public void UpdatePotionEffects();
	}
}
