using System.Collections.Generic;
using System.Linq;
using Data.Save;
using Data.ScriptableObjects;
using Events.Interfaces;
using Persistence.Interfaces;


namespace Spells
{
	using Interfaces;

	// Glyphs are found in the world; spells are discovered by inscribing the right glyphs together.
	public class SpellBook : ISpellBook, ISaveParticipant
	{
		private readonly GameDatabase database;

		private readonly IEventBusSender eventBus;

		private readonly List<GlyphDefinition> glyphs = new();

		private readonly List<SpellDefinition> spells = new();

		private readonly SpellDefinition[] slots = new SpellDefinition[ISpellBook.SlotCount];

		public IReadOnlyList<GlyphDefinition> KnownGlyphs => glyphs;

		public IReadOnlyList<SpellDefinition> KnownSpells => spells;

		public IReadOnlyList<SpellDefinition> AllSpells => database.Spells;

		public SpellBook(GameDatabase database, IEventBusSender eventBus)
		{
			this.database = database;
			this.eventBus = eventBus;
		}

		public bool KnowsGlyph(GlyphDefinition glyph) => glyphs.Contains(glyph);

		public bool KnowsSpell(SpellDefinition spell) => spells.Contains(spell);

		public bool LearnGlyph(GlyphDefinition glyph)
		{
			if (glyph == null || KnowsGlyph(glyph))
				return false;

			glyphs.Add(glyph);

			eventBus.LearnGlyph(glyph);
			eventBus.Notify($"Glyph learned: {glyph.DisplayName}. Inscribe it in your spellbook (B).");

			return true;
		}

		public SpellDefinition GetSlot(int slot)
			=> slot >= 0 && slot < slots.Length ? slots[slot] : null;

		public void Equip(SpellDefinition spell, int slot)
		{
			if (slot < 0 || slot >= slots.Length || !KnowsSpell(spell))
				return;

			// A spell lives in one slot at a time
			for (var i = 0; i < slots.Length; i++)
				if (slots[i] == spell)
					slots[i] = null;

			slots[slot] = spell;

			eventBus.UpdateSpellSlots();
		}

		public InscriptionResult Inscribe(IReadOnlyList<GlyphDefinition> inscribed)
		{
			if (inscribed.Count == 0)
				return new InscriptionResult(null, false, "The page is blank. Choose some glyphs first.");

			var match = database.Spells.FirstOrDefault(spell => SameGlyphs(spell.Glyphs, inscribed));

			if (match == null)
				return new InscriptionResult(null, false, DescribeNearMiss(inscribed));

			if (KnowsSpell(match))
				return new InscriptionResult(match, false, $"You already know {match.DisplayName}.");

			spells.Add(match);

			var freeSlot = System.Array.IndexOf(slots, null);

			if (freeSlot >= 0)
				Equip(match, freeSlot);

			eventBus.LearnSpell(match);
			eventBus.Notify($"Spell discovered: {match.DisplayName}!");

			return new InscriptionResult(match, true, $"The ink flares! You discovered {match.DisplayName}. {match.Description}");
		}

		private static bool SameGlyphs(IReadOnlyCollection<GlyphDefinition> required, IReadOnlyList<GlyphDefinition> inscribed)
		{
			if (required.Count != inscribed.Count)
				return false;

			var remaining = required.ToList();

			foreach (var glyph in inscribed)
				if (!remaining.Remove(glyph))
					return false;

			return true;
		}

		private string DescribeNearMiss(IReadOnlyList<GlyphDefinition> inscribed)
		{
			var best = database.Spells
				.Where(spell => !KnowsSpell(spell))
				.Select(spell => (spell, overlap: Overlap(spell.Glyphs, inscribed)))
				.OrderByDescending(pair => pair.overlap)
				.FirstOrDefault();

			if (best.spell == null || best.overlap == 0)
				return "The glyphs fizzle. They don't seem to want to work together.";

			if (inscribed.Count > best.spell.Glyphs.Count)
				return "The ink pools and sputters. Something resonates, but there are too many glyphs.";

			return $"The ink shimmers faintly. {best.overlap} of these glyphs belong to a spell you have yet to find.";
		}

		private static int Overlap(IEnumerable<GlyphDefinition> required, IEnumerable<GlyphDefinition> inscribed)
		{
			var remaining = required.ToList();

			return inscribed.Count(glyph => remaining.Remove(glyph));
		}

		public void Read(SaveData data)
		{
			glyphs.Clear();
			glyphs.AddRange(data.Glyphs.Select(database.FindGlyph).Where(glyph => glyph != null));

			spells.Clear();
			spells.AddRange(data.Spells.Select(database.FindSpell).Where(spell => spell != null));

			for (var i = 0; i < slots.Length; i++)
				slots[i] = i < data.SpellSlots.Count ? database.FindSpell(data.SpellSlots[i]) : null;

			eventBus.UpdateSpellSlots();
		}

		public void Write(SaveData data)
		{
			data.Glyphs = glyphs.Select(glyph => glyph.Id).ToList();
			data.Spells = spells.Select(spell => spell.Id).ToList();
			data.SpellSlots = slots.Select(spell => spell != null ? spell.Id : "").ToList();
		}
	}
}
