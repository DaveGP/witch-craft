using System.Linq;
using Data.Enums;
using Data.ScriptableObjects;
using DependencyInjection;
using Home;
using Items.Interfaces;
using Spells.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace UserInterface.Views
{
	// The Conjure spell's build catalogue: every piece with its cost and comfort, the colours to paint with,
	// the paintbrush, and undo. Picking a piece closes it; picking a colour keeps it open.
	public class CatalogueView : View
	{
		private enum Tab
		{
			Structures,
			Furniture,
			Tools,
		}

		private IInventory inventory;

		private IBuildPalette palette;

		private Tab tab = Tab.Structures;

		private RectTransform tabs;

		private RectTransform grid;

		private RectTransform colours;

		private Text colourName;

		protected override void OnBuild()
		{
			inventory = ServiceLocator.Get<IInventory>();

			var shade = UIFactory.Image(Root, new Color(0.05f, 0.03f, 0.08f, 0.55f), "Shade");
			UIFactory.Fill(shade.rectTransform);

			var panel = UIFactory.Image(Root, UIFactory.Panel, "Catalogue");
			UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-780, -440), new Vector2(780, 440));

			var title = UIFactory.Label(panel.transform, "Build catalogue", 38, UIFactory.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
			UIFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(36, -76), new Vector2(0, -24));

			var hint = UIFactory.Label(panel.transform, "Right-click or Esc to close", 18, UIFactory.TextMuted, TextAnchor.UpperRight);
			UIFactory.Place(hint.rectTransform, new Vector2(0.5f, 1), new Vector2(1, 1), new Vector2(0, -60), new Vector2(-36, -30));

			tabs = UIFactory.Rect("Tabs", panel.transform);
			UIFactory.Place(tabs, new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -140), new Vector2(-36, -90));
			UIFactory.Horizontal(tabs.gameObject, 10f, 0, false);

			grid = UIFactory.Rect("Pieces", panel.transform);
			UIFactory.Place(grid, new Vector2(0, 0), new Vector2(1, 1), new Vector2(36, 190), new Vector2(-36, -156));

			var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
			layout.cellSize = new Vector2(352, 120);
			layout.spacing = new Vector2(14, 14);

			colourName = UIFactory.Label(panel.transform, "", 20, UIFactory.Text, TextAnchor.LowerLeft);
			UIFactory.Place(colourName.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(36, 140), new Vector2(-36, 176));

			colours = UIFactory.Rect("Colours", panel.transform);
			UIFactory.Place(colours, new Vector2(0, 0), new Vector2(1, 0), new Vector2(36, 36), new Vector2(-36, 130));

			var swatches = colours.gameObject.AddComponent<GridLayoutGroup>();
			swatches.cellSize = new Vector2(136, 86);
			swatches.spacing = new Vector2(12, 0);
		}

		public void Show(IBuildPalette buildPalette)
		{
			palette = buildPalette;

			tab = palette.Painting ? Tab.Tools
				: palette.Selected != null && palette.Selected.Category == BuildCategory.Furniture ? Tab.Furniture
				: Tab.Structures;

			Open();
		}

		protected override void OnOpen() => Refresh();

		private void Refresh()
		{
			RefreshTabs();
			RefreshGrid();
			RefreshColours();

			if (IsOpen)
				StartCoroutine(FocusFirst());
		}

		private void RefreshTabs()
		{
			UIFactory.Clear(tabs);

			foreach (var each in new[] { Tab.Structures, Tab.Furniture, Tab.Tools })
			{
				var chosen = each;

				var button = UIFactory.Button(tabs, each.ToString(), () => { tab = chosen; Refresh(); }, 20,
					each == tab ? new Color(0.55f, 0.34f, 0.62f) : new Color(0.28f, 0.22f, 0.36f), 46);

				UIFactory.Layout(button.gameObject, minWidth: 200, preferredWidth: 200, minHeight: 46, preferredHeight: 46);
			}
		}

		private void RefreshGrid()
		{
			UIFactory.Clear(grid);

			if (tab == Tab.Tools)
			{
				Tile("<b>Paintbrush</b>\n<size=17>Repaint anything you've built in the chosen colour. Free.</size>", palette.Painting, true, () =>
				{
					palette.SelectPaintbrush();
					Close();
				});

				Tile($"<b>Undo last piece</b>\n<size=17>{(palette.CanUndo ? "Takes back the last thing you conjured and returns its materials." : "Nothing to undo yet.")}</size>", false, palette.CanUndo, () =>
				{
					palette.Undo();
					Refresh();
				});

				Tile("<b>Copy a piece</b>\n<size=17>Middle-click (or V) on something you built to conjure more like it, colour and all.</size>", false, false, null);

				return;
			}

			var category = tab == Tab.Furniture ? BuildCategory.Furniture : BuildCategory.Structure;

			foreach (var piece in palette.AllPieces.Where(piece => piece.Category == category))
				PieceTile(piece);
		}

		private void PieceTile(BuildPieceDefinition piece)
		{
			var unlocked = piece.RequiredHomeLevel <= palette.HomeLevel;

			var affordable = inventory.Has(piece.Cost);

			var cost = string.Join(", ", piece.Cost.Select(stack =>
				$"<color=#{UIFactory.Hex(inventory.Count(stack.Item) >= stack.Amount ? new Color(0.7f, 0.95f, 0.7f) : new Color(1f, 0.6f, 0.55f))}>{stack.Amount} {stack.Item.DisplayName}</color>"));

			var details = unlocked
				? cost + (piece.Comfort > 0 ? $"\n<color=#{UIFactory.Hex(UIFactory.Accent)}>+{piece.Comfort} comfort</color>" : "")
				: $"<color=#{UIFactory.Hex(UIFactory.TextMuted)}>Unlocks at home level {piece.RequiredHomeLevel}</color>";

			var selected = !palette.Painting && palette.Selected == piece;

			var label = $"<b>{piece.DisplayName}</b>{(affordable || !unlocked ? "" : "  <size=15>(need more)</size>")}\n<size=17>{details}</size>";

			Tile(label, selected, unlocked, () =>
			{
				palette.Select(piece);
				Close();
			});
		}

		private void Tile(string text, bool selected, bool interactable, System.Action onClick)
		{
			var colour = selected ? new Color(0.55f, 0.34f, 0.62f) : UIFactory.PanelLight;

			var button = UIFactory.Button(grid, "", onClick, 20, colour);
			button.interactable = interactable;

			var label = button.GetComponentInChildren<Text>();
			label.text = text;
			label.alignment = TextAnchor.MiddleLeft;
			UIFactory.Fill(label.rectTransform, 14f);
		}

		private void RefreshColours()
		{
			UIFactory.Clear(colours);

			for (var i = 0; i < HomePalette.Count; i++)
			{
				var index = i;
				var (name, shade) = HomePalette.Entries[i];

				var swatch = UIFactory.Button(colours, name, () => { palette.SetColour(index); Refresh(); }, 16,
					i == 0 ? new Color(0.62f, 0.52f, 0.42f) : shade);

				var label = swatch.GetComponentInChildren<Text>();
				label.color = shade.grayscale > 0.6f ? UIFactory.Ink : UIFactory.Text;

				if (i != palette.Colour)
					continue;

				var outline = swatch.gameObject.AddComponent<Outline>();
				outline.effectColor = UIFactory.Accent;
				outline.effectDistance = new Vector2(4, -4);
			}

			colourName.text = $"Colour: <b>{HomePalette.Entries[palette.Colour].Name}</b>  <size=16><color=#{UIFactory.Hex(UIFactory.TextMuted)}>walls, floors, roofs and fabrics take paint; trims and glass keep their own</color></size>";
		}

		private void Update()
		{
			// Right-click closes, as it opened
			if (IsOpen && UnityEngine.InputSystem.Mouse.current?.rightButton.wasPressedThisFrame == true)
				Close();
		}
	}
}
