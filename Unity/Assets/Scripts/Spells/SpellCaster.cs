using System.Collections.Generic;
using System.Linq;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using LocalPlayer.Interfaces;
using UnityEngine;


namespace Spells
{
	using Interfaces;

	// Readies the spell in the chosen slot and routes casting input to its effect.
	// Pressing the slot key of the readied spell puts it away again.
	public class SpellCaster : MonoBehaviour, ISpellCaster
	{
		public int ActiveSlot { get; private set; }

		public SpellDefinition ActiveSpell => sheathed ? null : spellBook.GetSlot(ActiveSlot);

		public string Status => activeEffect?.Status ?? "";

		public bool IsAiming => activeEffect?.IsAiming ?? false;

		private ISpellBook spellBook;

		private IEventBusSender eventBus;

		private IPlayerContext playerContext;

		private readonly Dictionary<Data.Enums.SpellEffectType, ISpellEffect> effects = new();

		private ISpellEffect activeEffect;

		private SpellDefinition activeDefinition;

		private bool sheathed = true;

		private void Awake()
		{
			spellBook = ServiceLocator.Get<ISpellBook>();
			eventBus = ServiceLocator.Get<IEventBusSender>();
			playerContext = ServiceLocator.Get<IPlayerContext>();

			if (spellBook == null)
				Debug.LogError($"{nameof(spellBook)} is null");

			if (eventBus == null)
				Debug.LogError($"{nameof(eventBus)} is null");

			if (playerContext == null)
				Debug.LogError($"{nameof(playerContext)} is null");

			foreach (var effect in GetComponents<ISpellEffect>())
				effects[effect.Type] = effect;
		}

		private void Start()
		{
			eventBus.SpellSlotsChanged += Refresh;
		}

		private void OnDestroy()
		{
			eventBus.SpellSlotsChanged -= Refresh;

			playerContext.ScrollCaptured = false;
		}

		public void SelectSlot(int slot)
		{
			if (spellBook.GetSlot(slot) == null)
			{
				eventBus.Notify(spellBook.KnownSpells.Count == 0
					? "You don't know any spells yet. Combine glyphs in your spellbook (B)."
					: $"Slot {slot + 1} is empty. Equip a spell in your spellbook (B).");
				return;
			}

			sheathed = slot == ActiveSlot && !sheathed;

			ActiveSlot = slot;

			Refresh();

			eventBus.UpdateSpellSlots();
		}

		public void Cast()
		{
			if (activeEffect == null)
			{
				if (spellBook.KnownSpells.Count > 0)
					eventBus.Notify("Ready a spell with 1-4 first.");

				return;
			}

			activeEffect.Cast();
		}

		public void AltCast()
		{
			activeEffect?.AltCast();
		}

		public void Adjust(float rotate, float scroll)
		{
			activeEffect?.Adjust(rotate, scroll);

			playerContext.ScrollCaptured = activeEffect is { CapturesScroll: true };
		}

		private void Refresh()
		{
			var spell = ActiveSpell;

			var effect = spell != null && effects.TryGetValue(spell.Effect, out var found) ? found : null;

			if (effect == activeEffect && spell == activeDefinition)
				return;

			activeEffect?.End();

			activeEffect = effect;
			activeDefinition = spell;

			activeEffect?.Begin(spell);

			if (spell != null && effect == null)
				Debug.LogWarning($"SpellCaster: No effect component for {spell.Effect}");
		}

		public IEnumerable<string> EffectNames => effects.Keys.Select(key => key.ToString());
	}
}
