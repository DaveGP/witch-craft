using System.Collections.Generic;
using System.Linq;
using Data.Enums;
using UnityEngine;

namespace Data.ScriptableObjects
{
	[CreateAssetMenu(fileName = "Spell", menuName = "Witch Craft/Spell", order = 12)]
	public class SpellDefinition : ScriptableObject
	{
		public string Id;

		public string DisplayName;

		public SpellEffectType Effect;

		public Color Color = Color.white;

		[Tooltip("Glyphs inscribed together to discover this spell, in any order")]
		public List<GlyphDefinition> Glyphs = new();

		[TextArea] public string Description;

		[Tooltip("Riddle shown in the spellbook before the spell is discovered")]
		[TextArea] public string Hint;

		public string GlyphSummary
			=> string.Join(" + ", Glyphs.Select(glyph => glyph.DisplayName));
	}
}
