using UnityEngine;

namespace Data.ScriptableObjects
{
	[CreateAssetMenu(fileName = "Glyph", menuName = "Witch Craft/Glyph", order = 11)]
	public class GlyphDefinition : ScriptableObject
	{
		public string Id;

		public string DisplayName;

		[Tooltip("Short rune shown on glyph tiles")]
		public string Rune;

		public Color Color = Color.white;

		[TextArea] public string Description;
	}
}
