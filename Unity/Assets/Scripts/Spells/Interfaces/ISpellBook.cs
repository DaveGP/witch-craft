using System.Collections.Generic;
using Data.ScriptableObjects;

namespace Spells.Interfaces
{
	public interface ISpellBook
	{
		public const int SlotCount = 4;

		IReadOnlyList<GlyphDefinition> KnownGlyphs { get; }

		IReadOnlyList<SpellDefinition> KnownSpells { get; }

		IReadOnlyList<SpellDefinition> AllSpells { get; }

		bool KnowsGlyph(GlyphDefinition glyph);

		bool KnowsSpell(SpellDefinition spell);

		bool LearnGlyph(GlyphDefinition glyph);

		SpellDefinition GetSlot(int slot);

		void Equip(SpellDefinition spell, int slot);

		/// <summary>
		/// Combines glyphs on a spellbook page. Discovers a spell when the set matches one.
		/// </summary>
		InscriptionResult Inscribe(IReadOnlyList<GlyphDefinition> glyphs);
	}

	public readonly struct InscriptionResult
	{
		public readonly SpellDefinition Spell;

		public readonly bool IsNew;

		public readonly string Message;

		public InscriptionResult(SpellDefinition spell, bool isNew, string message)
		{
			Spell = spell;
			IsNew = isNew;
			Message = message;
		}
	}
}
