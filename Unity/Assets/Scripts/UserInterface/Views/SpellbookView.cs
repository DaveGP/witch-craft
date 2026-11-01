using System.Collections.Generic;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using Spells.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace UserInterface.Views
{
	// An open grimoire. The left page combines glyphs to discover spells; the right page lists spells,
	// known ones with equip buttons and unknown ones as riddles hinting at their glyphs.
	public class SpellbookView : View
	{
		private const int MaxGlyphs = 3;

		private ISpellBook spellBook;

		private IEventBus eventBus;

		private readonly List<GlyphDefinition> inscription = new();

		private RectTransform glyphGrid;

		private RectTransform inscriptionRow;

		private Text result;

		private RectTransform spellList;

		protected override void OnBuild()
		{
			spellBook = ServiceLocator.Get<ISpellBook>();
			eventBus = ServiceLocator.Get<IEventBus>();

			var book = UIFactory.Image(Root, new Color(0.35f, 0.2f, 0.25f, 1f), "Book");
			UIFactory.Place(book.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-860, -440), new Vector2(860, 440));

			var left = Page(book.transform, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(24, 24), new Vector2(-8, -24));
			var right = Page(book.transform, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(8, 24), new Vector2(-24, -24));

			BuildGlyphPage(left);
			BuildSpellPage(right);

			eventBus.GlyphLearned += _ => { if (IsOpen) Refresh(); };
			eventBus.SpellSlotsChanged += () => { if (IsOpen) Refresh(); };
		}

		private static RectTransform Page(Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
		{
			var page = UIFactory.Image(parent, UIFactory.Parchment, "Page");
			UIFactory.Place(page.rectTransform, min, max, offsetMin, offsetMax);

			return page.rectTransform;
		}

		private void BuildGlyphPage(RectTransform page)
		{
			var title = UIFactory.Label(page, "Glyphs", 38, UIFactory.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
			UIFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -70), new Vector2(-30, -20));

			var intro = UIFactory.Label(page, "Choose up to three glyphs and inscribe them together. Matching glyphs awaken a spell.", 18, UIFactory.Ink, TextAnchor.UpperLeft, FontStyle.Italic);
			UIFactory.Place(intro.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -120), new Vector2(-30, -72));

			glyphGrid = UIFactory.Rect("Glyphs", page);
			UIFactory.Place(glyphGrid, new Vector2(0, 0.42f), new Vector2(1, 1), new Vector2(30, 0), new Vector2(-30, -130));

			var grid = glyphGrid.gameObject.AddComponent<GridLayoutGroup>();
			grid.cellSize = new Vector2(180, 110);
			grid.spacing = new Vector2(14, 14);

			var label = UIFactory.Label(page, "Inscription", 24, UIFactory.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
			UIFactory.Place(label.rectTransform, new Vector2(0, 0.42f), new Vector2(1, 0.42f), new Vector2(30, -40), new Vector2(-30, 0));

			inscriptionRow = UIFactory.Rect("Inscription", page);
			UIFactory.Place(inscriptionRow, new Vector2(0, 0.42f), new Vector2(1, 0.42f), new Vector2(30, -150), new Vector2(-30, -50));
			UIFactory.Horizontal(inscriptionRow.gameObject, 14f);

			var actions = UIFactory.Rect("Actions", page);
			UIFactory.Place(actions, new Vector2(0, 0.42f), new Vector2(1, 0.42f), new Vector2(30, -220), new Vector2(-30, -165));
			UIFactory.Horizontal(actions.gameObject, 14f);

			UIFactory.Button(actions, "Inscribe", Inscribe, 24, new Color(0.45f, 0.25f, 0.45f), 54);
			UIFactory.Button(actions, "Clear", () => { inscription.Clear(); Refresh(); }, 22, new Color(0.4f, 0.32f, 0.3f), 54);

			result = UIFactory.Label(page, "", 20, UIFactory.Ink, TextAnchor.UpperLeft, FontStyle.Italic);
			UIFactory.Place(result.rectTransform, Vector2.zero, new Vector2(1, 0.42f), new Vector2(30, 20), new Vector2(-30, -230));
		}

		private void BuildSpellPage(RectTransform page)
		{
			var title = UIFactory.Label(page, "Spells", 38, UIFactory.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
			UIFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -70), new Vector2(-30, -20));

			var hint = UIFactory.Label(page, "B to close", 18, UIFactory.Ink, TextAnchor.UpperRight);
			UIFactory.Place(hint.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -60), new Vector2(-30, -30));

			var area = UIFactory.Rect("Spells", page);
			UIFactory.Place(area, Vector2.zero, Vector2.one, new Vector2(24, 24), new Vector2(-24, -80));

			spellList = UIFactory.ScrollList(area, out var scroll, 10f);
			UIFactory.Fill((RectTransform)scroll.transform);
		}

		protected override void OnOpen()
		{
			result.text = "";
			Refresh();
		}

		private void Refresh()
		{
			RefreshGlyphs();
			RefreshInscription();
			RefreshSpells();
		}

		private void RefreshGlyphs()
		{
			UIFactory.Clear(glyphGrid);

			foreach (var glyph in spellBook.KnownGlyphs)
			{
				var chosen = glyph;

				var button = UIFactory.Button(glyphGrid, $"<size=34>{glyph.Rune}</size>\n{glyph.DisplayName}", () => AddGlyph(chosen), 20, Color.Lerp(glyph.Color, Color.black, 0.35f));

				button.interactable = inscription.Count < MaxGlyphs;
			}

			if (spellBook.KnownGlyphs.Count == 0)
				UIFactory.Label(glyphGrid, "No glyphs yet. Seek out the carved shrines.", 18, UIFactory.Ink, TextAnchor.MiddleLeft, FontStyle.Italic);
		}

		private void RefreshInscription()
		{
			UIFactory.Clear(inscriptionRow);

			for (var i = 0; i < MaxGlyphs; i++)
			{
				if (i < inscription.Count)
				{
					var index = i;
					var glyph = inscription[i];

					UIFactory.Button(inscriptionRow, $"<size=30>{glyph.Rune}</size>\n{glyph.DisplayName}", () => { inscription.RemoveAt(index); Refresh(); }, 18, glyph.Color, 100);
				}
				else
				{
					var empty = UIFactory.Image(inscriptionRow, new Color(0.3f, 0.2f, 0.2f, 0.15f), "Empty");
					UIFactory.Layout(empty.gameObject, preferredHeight: 100);
				}
			}
		}

		private void RefreshSpells()
		{
			UIFactory.Clear(spellList);

			foreach (var spell in spellBook.AllSpells)
			{
				if (spellBook.KnowsSpell(spell))
					KnownSpell(spell);
				else
					UnknownSpell(spell);
			}
		}

		private void KnownSpell(SpellDefinition spell)
		{
			var entry = UIFactory.Image(spellList, new Color(1f, 1f, 1f, 0.35f), "Spell");
			UIFactory.Vertical(entry.gameObject, 4f, 12);

			UIFactory.Label(entry.transform, $"<color=#{UIFactory.Hex(Color.Lerp(spell.Color, Color.black, 0.4f))}><b>{spell.DisplayName}</b></color>   <size=16>{spell.GlyphSummary}</size>", 26, UIFactory.Ink);
			UIFactory.Label(entry.transform, spell.Description, 18, UIFactory.Ink);

			var row = UIFactory.Row(entry.transform, 8f, 40f);

			var equip = UIFactory.Label(row, "Equip to:", 18, UIFactory.Ink, TextAnchor.MiddleLeft);
			UIFactory.Layout(equip.gameObject, preferredWidth: 100);

			for (var slot = 0; slot < ISpellBook.SlotCount; slot++)
			{
				var target = slot;

				var equipped = spellBook.GetSlot(slot) == spell;

				UIFactory.Button(row, $"{slot + 1}", () => spellBook.Equip(spell, target), 20, equipped ? spell.Color : new Color(0.45f, 0.35f, 0.4f));
			}
		}

		private void UnknownSpell(SpellDefinition spell)
		{
			var entry = UIFactory.Image(spellList, new Color(0f, 0f, 0f, 0.08f), "Unknown");
			UIFactory.Vertical(entry.gameObject, 4f, 12);

			UIFactory.Label(entry.transform, "<b>? ? ?</b>", 26, new Color(0.4f, 0.32f, 0.35f));
			UIFactory.Label(entry.transform, spell.Hint, 18, new Color(0.35f, 0.27f, 0.3f), TextAnchor.UpperLeft, FontStyle.Italic);
		}

		private void AddGlyph(GlyphDefinition glyph)
		{
			if (inscription.Count >= MaxGlyphs)
				return;

			inscription.Add(glyph);

			Refresh();
		}

		private void Inscribe()
		{
			var inscribed = spellBook.Inscribe(inscription);

			result.text = inscribed.Message;

			if (inscribed.Spell != null)
				inscription.Clear();

			Refresh();
		}
	}
}
