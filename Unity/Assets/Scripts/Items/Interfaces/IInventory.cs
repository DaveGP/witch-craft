using System.Collections.Generic;
using Data.ScriptableObjects;

namespace Items.Interfaces
{
	public interface IInventory
	{
		IEnumerable<ItemStack> Items { get; }

		int Count(ItemDefinition item);

		void Add(ItemDefinition item, int amount);

		/// <summary>
		/// Removes the amount if it is all there, otherwise removes nothing.
		/// </summary>
		bool Remove(ItemDefinition item, int amount);

		bool Has(IEnumerable<ItemStack> cost);

		bool RemoveAll(IEnumerable<ItemStack> cost);
	}
}
