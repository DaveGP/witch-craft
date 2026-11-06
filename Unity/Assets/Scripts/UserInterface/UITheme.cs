using UnityEngine;
using UnityEngine.UI;

namespace UserInterface
{
	public static class UITheme
	{
		public enum Surface { Leather, Paper, Wood, Plaque }

		public static readonly Color Night = Hex(0x2A2028);
		public static readonly Color Leather = Hex(0x422B39);
		public static readonly Color Parchment = Hex(0xF5E7CA);
		public static readonly Color Ink = Hex(0x34251F);
		public static readonly Color Linen = Hex(0xFFF2DC);
		public static readonly Color Muted = Hex(0xDBC7B1);
		public static readonly Color Brass = Hex(0xC18A49);
		public static readonly Color Gold = Hex(0xFFE2A1);
		public static readonly Color Moss = Hex(0x315344);
		public static readonly Color Brick = Hex(0x813D3D);
		public static readonly Color Scrim = Hex(0x1B151C, 150);
		public static readonly Color HudBacking = Hex(0x2A2028, 204);
		public static readonly Color Track = Hex(0x1B151C, 220);
		public static readonly Color SkyMagic = Hex(0xB8D6F2);
		public static readonly Color Walnut = Hex(0x51372B);
		public static readonly Color WoodTint = Hex(0xB4947E);
		public static readonly Color Mulberry = Hex(0x684760);
		public static readonly Color SoftInk = Hex(0x60483B);
		public static readonly Color Positive = Hex(0xBCDDAF);
		public static readonly Color Warning = Hex(0xFFB0A5);
		public static readonly Color Fire = Hex(0xFFAD58);
		public static readonly Color Ember = Hex(0xF36C49);
		public static readonly Color Sand = Hex(0xF2D990);
		public static readonly Color BoilBand = Hex(0x8BDC87, 100);
		public static readonly Color InsetPaper = Hex(0x60483B, 26);

		public const int Meta = 16;
		public const int Small = 18;
		public const int Control = 20;
		public const int Body = 22;
		public const int Reading = 26;
		public const int Section = 28;
		public const int Heading = 40;
		public const int Display = 88;
		public const int TinyGap = 4;
		public const int Gap = 8;
		public const int RowGap = 12;
		public const int Inset = 16;
		public const int Padding = 24;
		public const int Gutter = 32;
		public const int Margin = 48;
		public const int ControlHeight = 44;

		private static Font bodyFont;
		private static Font displayFont;
		private static Font fallbackFont;
		private static Sprite frame;
		private static Sprite paper;
		private static Sprite wood;

		public static Font FallbackFont => fallbackFont ??= Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		public static Font BodyFont => bodyFont ??= Resources.Load<Font>("UI/Fonts/Body") ?? FallbackFont;
		public static Font DisplayFont => displayFont ??= Resources.Load<Font>("UI/Fonts/Display") ?? FallbackFont;

		public static Color32 Hex(int rgb, byte alpha = 255)
			=> new((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, alpha);

		public static void Style(Image image, Surface surface)
		{
			image.sprite = surface switch
			{
				Surface.Paper => paper ??= Resources.Load<Sprite>("UI/Paper"),
				Surface.Wood => wood ??= Resources.Load<Sprite>("UI/Wood"),
				_ => frame ??= Resources.Load<Sprite>("UI/Frame")
			};

			if (image.sprite != null)
				image.type = Image.Type.Sliced;
		}

		public static ColorBlock ControlColours()
		{
			var colours = ColorBlock.defaultColorBlock;
			colours.normalColor = Hex(0xE5D8D1);
			colours.highlightedColor = Color.white;
			colours.selectedColor = Hex(0xFFE2A1);
			colours.pressedColor = Hex(0xBEA99A);
			colours.disabledColor = Hex(0xA49690, 175);
			colours.fadeDuration = 0.075f;
			return colours;
		}
	}
}
