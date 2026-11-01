using System.Collections.Generic;
using System.Linq;
using Data.Enums;
using Data.ScriptableObjects;
using Events.Interfaces;
using Items.Interfaces;
using SystemManagement.Interfaces;
using UnityEngine;


namespace Potions
{
	using Interfaces;

	public class PotionEffects : IPotionEffects, IUpdateSystem
	{
		private readonly IInventory inventory;

		private readonly IEventBusSender eventBus;

		private readonly Dictionary<PotionEffect, float> active = new();

		public IReadOnlyDictionary<PotionEffect, float> Active => active;

		public float SpeedMultiplier => IsActive(PotionEffect.Swiftness) ? 1.5f : 1f;

		public float JumpMultiplier => IsActive(PotionEffect.Leaping) ? 1.6f : 1f;

		public int ExtraAirJumps => IsActive(PotionEffect.Leaping) ? 1 : 0;

		public float FallGravityMultiplier => IsActive(PotionEffect.Featherfall) ? 0.25f : 1f;

		public float MaxFallSpeedMultiplier => IsActive(PotionEffect.Featherfall) ? 0.15f : 1f;

		public PotionEffects(IInventory inventory, IEventBusSender eventBus)
		{
			this.inventory = inventory;
			this.eventBus = eventBus;
		}

		public bool IsActive(PotionEffect effect) => active.ContainsKey(effect);

		public bool Drink(ItemDefinition potion)
		{
			if (potion == null || potion.PotionEffect == PotionEffect.None)
				return false;

			if (!inventory.Remove(potion, 1))
				return false;

			active[potion.PotionEffect] = potion.PotionDuration;

			eventBus.Notify($"You drink the {potion.DisplayName}. {potion.Description}");
			eventBus.UpdatePotionEffects();

			return true;
		}

		public void Clear()
		{
			active.Clear();

			eventBus.UpdatePotionEffects();
		}

		public void RunUpdate()
		{
			if (active.Count == 0)
				return;

			var expired = false;

			foreach (var effect in active.Keys.ToList())
			{
				active[effect] -= Time.deltaTime;

				if (active[effect] > 0f)
					continue;

				active.Remove(effect);

				eventBus.Notify($"The {effect} potion wears off.");

				expired = true;
			}

			if (expired)
				eventBus.UpdatePotionEffects();
		}
	}
}
