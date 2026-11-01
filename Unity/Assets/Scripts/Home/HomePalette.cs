using UnityEngine;

namespace Home
{
	// Colours the witch can paint what she builds. Entry 0 leaves the material's own colour.
	public static class HomePalette
	{
		public static readonly (string Name, Color Colour)[] Entries =
		{
			("Natural", Color.white),
			("Cream", new Color(0.96f, 0.91f, 0.8f)),
			("Rose", new Color(0.88f, 0.55f, 0.6f)),
			("Terracotta", new Color(0.8f, 0.48f, 0.36f)),
			("Butter", new Color(0.97f, 0.85f, 0.5f)),
			("Sage", new Color(0.6f, 0.75f, 0.58f)),
			("Sky", new Color(0.58f, 0.75f, 0.92f)),
			("Lavender", new Color(0.72f, 0.62f, 0.9f)),
			("Plum", new Color(0.48f, 0.3f, 0.55f)),
			("Midnight", new Color(0.24f, 0.26f, 0.42f)),
		};

		public static int Count => Entries.Length;

		public static int Clamp(int index) => index < 0 || index >= Entries.Length ? 0 : index;
	}
}
