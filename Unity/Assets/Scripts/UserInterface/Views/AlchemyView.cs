using System.Collections.Generic;
using System.Linq;
using Alchemy;
using Alchemy.Interfaces;
using Data.Enums;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using Items.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace UserInterface.Views
{
	// The brewing bench overlay. The recipe book sits on the left, the ingredient shelf on the right and the
	// fire, mortar and hourglass controls along the bottom, leaving the cauldron itself in view.
	public class AlchemyView : View
	{
		private const int LogLines = 6;

		protected override bool TakesFocus => false;

		private IInventory inventory;

		private IRecipeBook recipeBook;

		private IEventBus eventBus;

		private AlchemyStation station;

		private RecipeDefinition selectedRecipe;

		private RectTransform recipeList;

		private Text recipePage;

		private RectTransform shelf;

		private Text cauldronStatus;

		private Image heatFill;

		private Image hourglassFill;

		private Text log;

		private readonly Queue<string> logLines = new();

		protected override void OnBuild()
		{
			inventory = ServiceLocator.Get<IInventory>();
			recipeBook = ServiceLocator.Get<IRecipeBook>();
			eventBus = ServiceLocator.Get<IEventBus>();

			BuildRecipeBook();
			BuildShelf();
			BuildBench();

			eventBus.InventoryChanged += InventoryChanged;
			eventBus.RecipeLearned += RecipeLearned;
		}

		// The event bus outlives the scene, so let go of it when the scene reloads
		private void OnDestroy()
		{
			if (eventBus == null)
				return;

			eventBus.InventoryChanged -= InventoryChanged;
			eventBus.RecipeLearned -= RecipeLearned;
		}

		private void InventoryChanged()
		{
			if (IsOpen)
				RefreshShelf();
		}

		private void RecipeLearned(RecipeDefinition recipe)
		{
			if (IsOpen)
				RefreshRecipes();
		}

		private void BuildRecipeBook()
		{
			var book = UIFactory.Image(Root, UIFactory.Parchment, "RecipeBook");
			UIFactory.Place(book.rectTransform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(24, 300), new Vector2(520, -24));

			var title = UIFactory.Label(book.transform, "Recipe Book", 34, UIFactory.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
			UIFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -64), new Vector2(-24, -16));

			recipeList = UIFactory.Rect("Recipes", book.transform);
			UIFactory.Place(recipeList, new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -260), new Vector2(-24, -70));
			UIFactory.Vertical(recipeList.gameObject, 6f);

			recipePage = UIFactory.Label(book.transform, "", 19, UIFactory.Ink);
			UIFactory.Place(recipePage.rectTransform, Vector2.zero, new Vector2(1, 1), new Vector2(24, 20), new Vector2(-24, -270));
		}

		private void BuildShelf()
		{
			var panel = UIFactory.Image(Root, UIFactory.Panel, "Shelf");
			UIFactory.Place(panel.rectTransform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-520, 300), new Vector2(-24, -24));

			var title = UIFactory.Label(panel.transform, "Ingredient Shelf", 30, UIFactory.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
			UIFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(20, -60), new Vector2(-20, -14));

			var area = UIFactory.Rect("Items", panel.transform);
			UIFactory.Place(area, Vector2.zero, Vector2.one, new Vector2(16, 16), new Vector2(-16, -66));

			shelf = UIFactory.ScrollList(area, out var scroll, 6f);
			UIFactory.Fill((RectTransform)scroll.transform);
		}

		private void BuildBench()
		{
			var panel = UIFactory.Image(Root, UIFactory.Panel, "Bench");
			UIFactory.Place(panel.rectTransform, Vector2.zero, new Vector2(1, 0), new Vector2(24, 24), new Vector2(-24, 280));

			cauldronStatus = UIFactory.Label(panel.transform, "", 21, UIFactory.Text);
			UIFactory.Place(cauldronStatus.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(24, -64), new Vector2(-12, -14));

			// Heat bar with the boiling band marked
			var heatLabel = UIFactory.Label(panel.transform, "Fire", 18, UIFactory.TextMuted);
			UIFactory.Place(heatLabel.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(24, -92), new Vector2(-12, -68));

			var heat = UIFactory.Rect("Heat", panel.transform);
			UIFactory.Place(heat, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(24, -122), new Vector2(-12, -94));
			UIFactory.Vertical(heat.gameObject);
			heatFill = UIFactory.Bar(heat, new Color(0, 0, 0, 0.5f), new Color(1f, 0.55f, 0.2f), 26);

			var band = UIFactory.Image(heatFill.transform.parent, new Color(0.5f, 1f, 0.5f, 0.25f), "BoilBand");
			UIFactory.Place(band.rectTransform, new Vector2(AlchemySession.MinBoilHeat, 0), new Vector2(AlchemySession.MaxBoilHeat, 1), Vector2.zero, Vector2.zero);

			var glassLabel = UIFactory.Label(panel.transform, "Hourglass", 18, UIFactory.TextMuted);
			UIFactory.Place(glassLabel.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(24, -150), new Vector2(-12, -126));

			var glass = UIFactory.Rect("Hourglass", panel.transform);
			UIFactory.Place(glass, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(24, -172), new Vector2(-12, -152));
			UIFactory.Vertical(glass.gameObject);
			hourglassFill = UIFactory.Bar(glass, new Color(0, 0, 0, 0.5f), new Color(0.95f, 0.85f, 0.55f), 18);

			var buttons = UIFactory.Rect("Actions", panel.transform);
			UIFactory.Place(buttons, new Vector2(0, 0), new Vector2(0.5f, 0), new Vector2(24, 16), new Vector2(-12, 80));
			var grid = buttons.gameObject.AddComponent<GridLayoutGroup>();
			grid.cellSize = new Vector2(205, 52);
			grid.spacing = new Vector2(8, 8);

			UIFactory.Button(buttons, "Bellows (Space)", Pump, 18, new Color(0.6f, 0.3f, 0.2f));
			UIFactory.Button(buttons, "Turn hourglass", () => station.Session.StartTurn(), 18);
			UIFactory.Button(buttons, "Grind (G)", () => station.Grind(), 18);
			UIFactory.Button(buttons, "Add from mortar", () => station.Session.AddFromMortar(), 18);
			UIFactory.Button(buttons, "Bottle", Bottle, 18, new Color(0.3f, 0.45f, 0.35f));
			UIFactory.Button(buttons, "Empty", () => station.Session.Empty(), 18, new Color(0.35f, 0.3f, 0.3f));
			UIFactory.Button(buttons, "Leave (Esc)", Cancel, 18, new Color(0.3f, 0.25f, 0.35f));

			log = UIFactory.Label(panel.transform, "", 19, UIFactory.Text);
			UIFactory.Place(log.rectTransform, new Vector2(0.5f, 0), new Vector2(1, 1), new Vector2(12, 16), new Vector2(-24, -14));
		}

		public void Show(AlchemyStation target)
		{
			if (station != null)
				station.Session.Logged -= Log;

			station = target;
			station.Session.Logged += Log;

			logLines.Clear();
			Log("The bench is ready. Follow a recipe from your book, step by step.");

			selectedRecipe ??= recipeBook.Known.FirstOrDefault();

			Open();
		}

		protected override void OnOpen()
		{
			RefreshRecipes();
			RefreshShelf();
		}

		protected override void OnClose()
		{
			station?.Close();
		}

		public void Pump()
		{
			if (!IsOpen)
				return;

			station.PumpBellows();
		}

		private void Bottle()
		{
			var brewed = station.Bottle();

			Log(brewed.Verdict);

			if (brewed.Output != null)
				Log($"You fill {brewed.Bottles} {(brewed.Bottles == 1 ? "bottle" : "bottles")} of {brewed.Output.DisplayName}.");
		}

		private void Log(string line)
		{
			logLines.Enqueue(line);

			while (logLines.Count > LogLines)
				logLines.Dequeue();

			log.text = string.Join("\n", logLines.Select((entry, index) => index == logLines.Count - 1 ? $"<b>{entry}</b>" : $"<color=#{UIFactory.Hex(UIFactory.TextMuted)}>{entry}</color>"));
		}

		private void RefreshRecipes()
		{
			UIFactory.Clear(recipeList);

			foreach (var recipe in recipeBook.Known)
			{
				var chosen = recipe;

				var selected = recipe == selectedRecipe;

				UIFactory.Button(recipeList, recipe.DisplayName, () => { selectedRecipe = chosen; RefreshRecipes(); }, 20,
					selected ? new Color(0.5f, 0.3f, 0.35f) : new Color(0.62f, 0.5f, 0.45f), 38);
			}

			RefreshPage();
		}

		private void RefreshPage()
		{
			if (selectedRecipe == null)
			{
				recipePage.text = "<i>No recipes yet.</i>";
				return;
			}

			var steps = selectedRecipe.Steps.Select((step, index) => $"{index + 1}. {step.Describe()}");

			recipePage.text = $"<b><size=26>{selectedRecipe.DisplayName}</size></b>\n<i>{selectedRecipe.Flavour}</i>\n\n{string.Join("\n", steps)}";
		}

		private void RefreshShelf()
		{
			UIFactory.Clear(shelf);

			var stacks = inventory.Items
				.Where(stack => stack.Item.Category is ItemCategory.Base or ItemCategory.Ingredient)
				.OrderBy(stack => stack.Item.Category)
				.ToList();

			if (stacks.Count == 0)
				UIFactory.Label(shelf, "Your shelf is bare. Gather herbs and spring water out in the world.", 18, UIFactory.TextMuted, TextAnchor.UpperLeft, FontStyle.Italic);

			foreach (var stack in stacks)
				ShelfRow(stack);
		}

		private void ShelfRow(ItemStack stack)
		{
			var item = stack.Item;

			var row = UIFactory.Image(shelf, UIFactory.PanelLight, "Ingredient");
			UIFactory.Horizontal(row.gameObject, 6f, 6, false).childAlignment = TextAnchor.MiddleLeft;
			UIFactory.Layout(row.gameObject, preferredHeight: 50);

			var swatch = UIFactory.Image(row.transform, item.Color, "Swatch");
			UIFactory.Layout(swatch.gameObject, minWidth: 26, preferredWidth: 26, preferredHeight: 26);

			var label = UIFactory.Label(row.transform, $"{item.DisplayName} ×{stack.Amount}", 19, UIFactory.Text, TextAnchor.MiddleLeft);
			UIFactory.Layout(label.gameObject, flexibleWidth: 1, preferredHeight: 40);

			if (item.Category == ItemCategory.Base)
			{
				Small(row.transform, "Pour", () => station.Pour(item));
				return;
			}

			Small(row.transform, "Add", () => station.AddWhole(item));
			Small(row.transform, "Mortar", () => station.PlaceInMortar(item));
		}

		private static void Small(Transform parent, string text, System.Action action)
		{
			var button = UIFactory.Button(parent, text, action, 17, null, 38);
			UIFactory.Layout(button.gameObject, minWidth: 84, preferredWidth: 84, preferredHeight: 38, minHeight: 38);
		}

		private void Update()
		{
			if (!IsOpen || station == null)
				return;

			var session = station.Session;

			heatFill.fillAmount = session.Heat;
			heatFill.color = session.Heat > AlchemySession.MaxBoilHeat ? new Color(1f, 0.25f, 0.15f)
				: session.Heat >= AlchemySession.MinBoilHeat ? new Color(1f, 0.7f, 0.25f)
				: new Color(0.7f, 0.45f, 0.3f);

			hourglassFill.fillAmount = session.IsBoiling ? 1f - session.TurnProgress : 0f;

			if (UnityEngine.InputSystem.Keyboard.current?.gKey.wasPressedThisFrame == true)
				station.Grind();

			var mortar = session.MortarItem != null ? $"{session.MortarItem.DisplayName} ({Mathf.RoundToInt(session.GrindProgress * 100)}% ground)" : "empty";

			var done = session.Performed.Count == 0 ? "nothing yet" : string.Join(" → ", session.Performed.Select(Short));

			cauldronStatus.text = $"Base: <b>{(session.Base != null ? session.Base.DisplayName : "none")}</b>    Mortar: <b>{mortar}</b>\n<size=16><color=#{UIFactory.Hex(UIFactory.TextMuted)}>So far: {done}</color></size>";
		}

		private static string Short(AlchemyStep step)
		{
			var name = step.Item != null ? step.Item.DisplayName : "";

			return step.Type switch
			{
				AlchemyStepType.PourBase => name,
				AlchemyStepType.AddWhole => $"{step.Amount}× {name}",
				AlchemyStepType.AddGround => $"{step.Amount}× ground {name}",
				AlchemyStepType.Boil => $"boil {step.Amount}",
				_ => step.Type.ToString()
			};
		}
	}
}
