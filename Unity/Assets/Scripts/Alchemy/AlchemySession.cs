using System;
using System.Collections.Generic;
using System.Linq;
using Data.Enums;
using Data.ScriptableObjects;


namespace Alchemy
{
	public readonly struct BrewResult
	{
		public readonly RecipeDefinition Recipe;

		public readonly ItemDefinition Output;

		/// <summary>
		/// 0 ruined, 1 passable, 2 fine, 3 perfect.
		/// </summary>
		public readonly int Quality;

		public readonly int Bottles;

		public readonly string Verdict;

		public BrewResult(RecipeDefinition recipe, ItemDefinition output, int quality, int bottles, string verdict)
		{
			Recipe = recipe;
			Output = output;
			Quality = quality;
			Bottles = bottles;
			Verdict = verdict;
		}
	}

	// The brewing minigame's rules, free of Unity so they are easy to reason about. Like a real alchemist's bench,
	// order matters: pour a base, add herbs whole or ground in the mortar, keep the fire in the boiling band with
	// the bellows while the hourglass runs, then bottle. The brew is judged against every recipe, so following
	// an unknown recipe by accident (or by companion gossip) discovers it.
	public class AlchemySession
	{
		public const float TurnDuration = 6f;

		public const float HeatDecayPerSecond = 0.11f;

		public const float BellowsHeat = 0.11f;

		public const float MinBoilHeat = 0.45f;

		public const float MaxBoilHeat = 0.82f;

		public const float GrindPerPress = 0.2f;

		public event Action<string> Logged;

		public ItemDefinition Base { get; private set; }

		public ItemDefinition MortarItem { get; private set; }

		public float GrindProgress { get; private set; }

		public float Heat { get; private set; }

		public bool IsBoiling { get; private set; }

		public float TurnProgress { get; private set; }

		public IReadOnlyList<AlchemyStep> Performed => performed;

		public IEnumerable<ItemDefinition> Contents => performed
			.Where(step => step.Item != null && step.Type != AlchemyStepType.PourBase)
			.Select(step => step.Item);

		public bool HasStarted => Base != null;

		private readonly List<AlchemyStep> performed = new();

		private float turnInBandTime;

		private float turnScorchTime;

		private int scorchedTurns;

		private float grindShortfall;


		public bool PourBase(ItemDefinition item)
		{
			if (Base != null)
			{
				Log("The cauldron already holds a base. Empty it to start over.");
				return false;
			}

			Base = item;

			Record(AlchemyStepType.PourBase, item);

			Log($"You pour {item.DisplayName} into the cauldron.");

			return true;
		}

		public bool PlaceInMortar(ItemDefinition item)
		{
			if (MortarItem != null)
			{
				Log($"The mortar already holds {MortarItem.DisplayName}.");
				return false;
			}

			MortarItem = item;

			GrindProgress = 0f;

			Log($"You drop {item.DisplayName} into the mortar.");

			return true;
		}

		public void Grind()
		{
			if (MortarItem == null)
				return;

			GrindProgress = Math.Min(1f, GrindProgress + GrindPerPress);

			if (GrindProgress >= 1f)
				Log($"The {MortarItem.DisplayName} is ground to a fine powder.");
		}

		public bool AddFromMortar()
		{
			if (MortarItem == null || !RequireBase())
				return false;

			if (GrindProgress < 1f)
				grindShortfall += 1f - GrindProgress;

			Record(AlchemyStepType.AddGround, MortarItem);

			Log(GrindProgress < 1f
				? $"You tip in the coarsely ground {MortarItem.DisplayName}. It could have been finer."
				: $"You add the ground {MortarItem.DisplayName}.");

			MortarItem = null;
			GrindProgress = 0f;

			return true;
		}

		public bool AddWhole(ItemDefinition item)
		{
			if (!RequireBase())
				return false;

			Record(AlchemyStepType.AddWhole, item);

			Log($"You add {item.DisplayName}.");

			return true;
		}

		public void PumpBellows()
		{
			Heat = Math.Min(1f, Heat + BellowsHeat);
		}

		public bool StartTurn()
		{
			if (IsBoiling || !RequireBase())
				return false;

			IsBoiling = true;
			TurnProgress = 0f;
			turnInBandTime = 0f;
			turnScorchTime = 0f;

			Log("You turn the hourglass. Keep the fire steady!");

			return true;
		}

		public void Tick(float deltaTime)
		{
			Heat = Math.Max(0f, Heat - HeatDecayPerSecond * deltaTime);

			if (!IsBoiling)
				return;

			TurnProgress += deltaTime / TurnDuration;

			if (Heat >= MinBoilHeat && Heat <= MaxBoilHeat)
				turnInBandTime += deltaTime;
			else if (Heat > MaxBoilHeat)
				turnScorchTime += deltaTime;

			if (TurnProgress >= 1f)
				FinishTurn();
		}

		private void FinishTurn()
		{
			IsBoiling = false;
			TurnProgress = 0f;

			var simmered = turnInBandTime + turnScorchTime;

			if (simmered < TurnDuration * 0.5f)
			{
				Log("The brew barely simmered. That turn doesn't count, work the bellows!");
				return;
			}

			Record(AlchemyStepType.Boil, null);

			if (turnScorchTime > TurnDuration * 0.3f)
			{
				scorchedTurns++;
				Log("It boiled, but the fire ran too hot and the brew scorched a little.");
				return;
			}

			Log("A good rolling boil. One turn done.");
		}

		public BrewResult Bottle(IEnumerable<RecipeDefinition> recipes, ItemDefinition failedBrew)
		{
			if (!HasStarted)
				return new BrewResult(null, null, 0, 0, "There's nothing in the cauldron to bottle.");

			Record(AlchemyStepType.Bottle, null);

			var scored = recipes
				.Select(recipe => (recipe, mistakes: Distance(performed, recipe.Steps)))
				.OrderBy(pair => pair.mistakes)
				.FirstOrDefault();

			var quality = scored.recipe == null
				? 0
				: Math.Clamp(3 - scored.mistakes * 2 - scorchedTurns - (int)Math.Round(grindShortfall * 2f), 0, 3);

			Reset();

			if (quality == 0)
				return new BrewResult(null, failedBrew, 0, 1,
					"The brew turns a murky grey. Not quite right... check the recipe's order, amounts and boiling.");

			var verdict = quality switch
			{
				3 => "Perfect! The potion glows with a clean, bright light.",
				2 => "A fine brew, nearly flawless.",
				_ => "Passable. It works, but an exacting alchemist would wince."
			};

			return new BrewResult(scored.recipe, scored.recipe.Output, quality, quality, verdict);
		}

		public void Empty()
		{
			Reset();

			Log("You tip the cauldron out and rinse it clean.");
		}

		private void Reset()
		{
			Base = null;
			MortarItem = null;
			GrindProgress = 0f;
			IsBoiling = false;
			TurnProgress = 0f;
			scorchedTurns = 0;
			grindShortfall = 0f;
			performed.Clear();
		}

		private bool RequireBase()
		{
			if (Base != null)
				return true;

			Log("Pour a base into the cauldron first.");

			return false;
		}

		private void Record(AlchemyStepType type, ItemDefinition item)
		{
			var last = performed.LastOrDefault();

			// Repeated additions and boils read as one step with an amount, like the recipe book writes them
			var merges = last != null
			             && last.Type == type
			             && last.Item == item
			             && type is AlchemyStepType.AddWhole or AlchemyStepType.AddGround or AlchemyStepType.Boil;

			if (merges)
				last.Amount++;
			else
				performed.Add(new AlchemyStep(type, item));
		}

		private void Log(string message) => Logged?.Invoke(message);

		// Edit distance between what was done and what the recipe asks for
		private static int Distance(IReadOnlyList<AlchemyStep> done, IReadOnlyList<AlchemyStep> recipe)
		{
			var costs = new int[done.Count + 1, recipe.Count + 1];

			for (var i = 0; i <= done.Count; i++)
				costs[i, 0] = i;

			for (var j = 0; j <= recipe.Count; j++)
				costs[0, j] = j;

			for (var i = 1; i <= done.Count; i++)
			for (var j = 1; j <= recipe.Count; j++)
			{
				var substitution = done[i - 1].Matches(recipe[j - 1]) ? 0 : 1;

				costs[i, j] = Math.Min(Math.Min(costs[i - 1, j] + 1, costs[i, j - 1] + 1), costs[i - 1, j - 1] + substitution);
			}

			return costs[done.Count, recipe.Count];
		}
	}
}
