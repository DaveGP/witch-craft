using System.Collections.Generic;
using Data.Enums;
using Data.ScriptableObjects;

namespace Potions.Interfaces
{
	public interface IPotionEffects
	{
		float SpeedMultiplier { get; }

		float JumpMultiplier { get; }

		int ExtraAirJumps { get; }

		float FallGravityMultiplier { get; }

		float MaxFallSpeedMultiplier { get; }

		IReadOnlyDictionary<PotionEffect, float> Active { get; }

		bool IsActive(PotionEffect effect);

		/// <summary>
		/// Drinks a potion from the satchel. Returns false if it isn't a drinkable potion or none are carried.
		/// </summary>
		bool Drink(ItemDefinition potion);

		void Clear();
	}
}
