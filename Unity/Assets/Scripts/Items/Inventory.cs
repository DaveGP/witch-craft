using System.Collections.Generic;
using System.Linq;
using Data.Save;
using Data.ScriptableObjects;
using Events.Interfaces;
using Persistence.Interfaces;


namespace Items
{
	using Interfaces;

	// The witch's satchel. Counts are keyed by item so the save stays small and readable.
	public class Inventory : IInventory, ISaveParticipant
	{
		private readonly GameDatabase database;

		private readonly IEventBusSender eventBus;

		private readonly Dictionary<ItemDefinition, int> counts = new();

		public Inventory(GameDatabase database, IEventBusSender eventBus)
		{
			this.database = database;
			this.eventBus = eventBus;
		}

		public IEnumerable<ItemStack> Items
			=> database.Items
				.Where(item => Count(item) > 0)
				.Select(item => new ItemStack(item, counts[item]));

		public int Count(ItemDefinition item)
			=> item != null && counts.TryGetValue(item, out var count) ? count : 0;

		public void Add(ItemDefinition item, int amount)
		{
			if (item == null || amount <= 0)
				return;

			counts[item] = Count(item) + amount;

			eventBus.CollectItem(item, amount);
			eventBus.InventoryUpdated();
		}

		public bool Remove(ItemDefinition item, int amount)
		{
			if (Count(item) < amount)
				return false;

			counts[item] -= amount;

			eventBus.InventoryUpdated();

			return true;
		}

		public bool Has(IEnumerable<ItemStack> cost)
			=> Merge(cost).All(pair => Count(pair.Key) >= pair.Value);

		public bool RemoveAll(IEnumerable<ItemStack> cost)
		{
			var merged = Merge(cost);

			if (!merged.All(pair => Count(pair.Key) >= pair.Value))
				return false;

			foreach (var pair in merged)
				counts[pair.Key] -= pair.Value;

			eventBus.InventoryUpdated();

			return true;
		}

		private static Dictionary<ItemDefinition, int> Merge(IEnumerable<ItemStack> cost)
			=> cost.Where(stack => stack.Item != null)
				.GroupBy(stack => stack.Item)
				.ToDictionary(group => group.Key, group => group.Sum(stack => stack.Amount));

		public void Read(SaveData data)
		{
			counts.Clear();

			foreach (var entry in data.Items)
			{
				var item = database.FindItem(entry.Id);

				if (item != null)
					counts[item] = entry.Amount;
			}

			eventBus.InventoryUpdated();
		}

		public void Write(SaveData data)
		{
			data.Items = counts
				.Where(pair => pair.Value > 0)
				.Select(pair => new ItemCount { Id = pair.Key.Id, Amount = pair.Value })
				.ToList();
		}
	}
}
