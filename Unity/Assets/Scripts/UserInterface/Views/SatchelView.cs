using System.Linq;
using Data.Enums;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using Items.Interfaces;
using Potions.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace UserInterface.Views
{
	// Everything the witch is carrying, grouped by use. Potions can be drunk from here.
	public class SatchelView : View
	{
		private IInventory inventory;

		private IPotionEffects potionEffects;

		private IEventBus eventBus;

		private RectTransform content;

		protected override void OnBuild()
		{
			inventory = ServiceLocator.Get<IInventory>();
			potionEffects = ServiceLocator.Get<IPotionEffects>();
			eventBus = ServiceLocator.Get<IEventBus>();

			var panel = UIFactory.Image(Root, UIFactory.Panel, "Satchel");
			UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-480, -420), new Vector2(480, 420));

			var title = UIFactory.Label(panel.transform, "Satchel", 40, UIFactory.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
			UIFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -70), new Vector2(-30, -20));

			var hint = UIFactory.Label(panel.transform, "Tab to close", 18, UIFactory.TextMuted, TextAnchor.UpperRight);
			UIFactory.Place(hint.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -60), new Vector2(-30, -30));

			var listArea = UIFactory.Rect("List", panel.transform);
			UIFactory.Place(listArea, Vector2.zero, Vector2.one, new Vector2(30, 30), new Vector2(-30, -80));

			content = UIFactory.ScrollList(listArea, out var scroll, 8f);
			UIFactory.Fill((RectTransform)scroll.transform);

			eventBus.InventoryChanged += () =>
			{
				if (IsOpen)
					Refresh();
			};
		}

		protected override void OnOpen() => Refresh();

		private void Refresh()
		{
			UIFactory.Clear(content);

			Section("Building materials", ItemCategory.Material);
			Section("Brewing bases", ItemCategory.Base);
			Section("Herbs and ingredients", ItemCategory.Ingredient);
			Section("Potions", ItemCategory.Potion);
		}

		private void Section(string heading, ItemCategory category)
		{
			var stacks = inventory.Items.Where(stack => stack.Item.Category == category).ToList();

			var header = UIFactory.Label(content, heading, 26, UIFactory.Accent, TextAnchor.LowerLeft, FontStyle.Bold);
			UIFactory.Layout(header.gameObject, preferredHeight: 40);

			if (stacks.Count == 0)
			{
				var empty = UIFactory.Label(content, "Nothing yet.", 18, UIFactory.TextMuted, TextAnchor.MiddleLeft, FontStyle.Italic);
				UIFactory.Layout(empty.gameObject, preferredHeight: 26);
				return;
			}

			foreach (var stack in stacks)
				Entry(stack);
		}

		private void Entry(ItemStack stack)
		{
			var row = UIFactory.Image(content, UIFactory.PanelLight, "Item");
			UIFactory.Horizontal(row.gameObject, 12f, 10, false).childAlignment = TextAnchor.MiddleLeft;
			UIFactory.Layout(row.gameObject, preferredHeight: 64);

			var swatch = UIFactory.Image(row.transform, stack.Item.Color, "Swatch");
			UIFactory.Layout(swatch.gameObject, minWidth: 36, preferredWidth: 36, preferredHeight: 36);

			var text = UIFactory.Label(row.transform,
				$"<b>{stack.Item.DisplayName}</b>  ×{stack.Amount}\n<size=16><color=#{UIFactory.Hex(UIFactory.TextMuted)}>{stack.Item.Description}</color></size>", 21);
			UIFactory.Layout(text.gameObject, flexibleWidth: 1, preferredHeight: 50);

			if (stack.Item.Category != ItemCategory.Potion || stack.Item.PotionEffect == PotionEffect.None)
				return;

			var item = stack.Item;

			var drink = UIFactory.Button(row.transform, "Drink", () => potionEffects.Drink(item), 20);
			UIFactory.Layout(drink.gameObject, minWidth: 110, preferredWidth: 110, preferredHeight: 44);
		}
	}
}
