using System;
using Data.Enums;

namespace Data.ScriptableObjects
{
	[Serializable]
	public class AlchemyStep
	{
		public AlchemyStepType Type;

		public ItemDefinition Item;

		public int Amount = 1;

		public AlchemyStep() { }

		public AlchemyStep(AlchemyStepType type, ItemDefinition item, int amount = 1)
		{
			Type = type;
			Item = item;
			Amount = amount;
		}

		public bool Matches(AlchemyStep other)
			=> other != null
			   && Type == other.Type
			   && Item == other.Item
			   && Amount == other.Amount;

		public string Describe()
		{
			var name = Item != null ? Item.DisplayName : "";

			return Type switch
			{
				AlchemyStepType.PourBase => $"Pour {name} into the cauldron.",
				AlchemyStepType.AddWhole => Amount > 1 ? $"Add {Amount} {name}." : $"Add {name}.",
				AlchemyStepType.AddGround => Amount > 1 ? $"Grind {Amount} {name} and add them." : $"Grind {name} and add it.",
				AlchemyStepType.Boil => Amount > 1 ? $"Boil for {Amount} turns of the hourglass." : "Boil for one turn of the hourglass.",
				AlchemyStepType.Bottle => "Bottle the brew.",
				_ => "?"
			};
		}
	}
}
