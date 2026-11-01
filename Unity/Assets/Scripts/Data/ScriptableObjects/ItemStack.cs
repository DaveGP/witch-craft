using System;

namespace Data.ScriptableObjects
{
	[Serializable]
	public class ItemStack
	{
		public ItemDefinition Item;

		public int Amount = 1;

		public ItemStack() { }

		public ItemStack(ItemDefinition item, int amount)
		{
			Item = item;
			Amount = amount;
		}

		public override string ToString()
			=> $"{Amount} {Item.DisplayName}";
	}
}
