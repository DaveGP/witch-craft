using System;
using Audio;
using Audio.Interfaces;
using DependencyInjection;
using UnityEngine;
using UnityEngine.UI;

namespace UserInterface
{
	// Builds uGUI from code so every screen lives in source and needs no prefab authoring.
	public static class UIFactory
	{
		public static readonly Color Panel = UITheme.Night;
		public static readonly Color PanelLight = UITheme.Leather;
		public static readonly Color Parchment = UITheme.Parchment;
		public static readonly Color Ink = UITheme.Ink;
		public static readonly Color Accent = UITheme.Gold;
		public static readonly Color Text = UITheme.Linen;
		public static readonly Color TextMuted = UITheme.Muted;
		public static readonly Color ButtonColour = UITheme.Leather;

		public static Font Font => UITheme.BodyFont;

		public static Image Surface(Transform parent, UITheme.Surface style, Color colour, string name)
		{
			var image = Image(parent, colour, name);
			UITheme.Style(image, style);
			return image;
		}

		public static RectTransform Rect(string name, Transform parent)
		{
			var gameObject = new GameObject(name, typeof(RectTransform));
			gameObject.transform.SetParent(parent, false);

			return (RectTransform)gameObject.transform;
		}

		public static RectTransform Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
		{
			rect.anchorMin = anchorMin;
			rect.anchorMax = anchorMax;
			rect.offsetMin = offsetMin;
			rect.offsetMax = offsetMax;

			return rect;
		}

		public static RectTransform Fill(RectTransform rect, float padding = 0f)
			=> Place(rect, Vector2.zero, Vector2.one, new Vector2(padding, padding), new Vector2(-padding, -padding));

		public static Image Image(Transform parent, Color colour, string name = "Panel")
		{
			var rect = Rect(name, parent);

			var image = rect.gameObject.AddComponent<Image>();
			image.color = colour;

			return image;
		}

		public static Text Label(Transform parent, string text, int size = UITheme.Body, Color? colour = null, TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
		{
			var rect = Rect("Label", parent);

			var label = rect.gameObject.AddComponent<Text>();
			label.font = size >= UITheme.Section ? UITheme.DisplayFont : Font;
			label.text = text;
			label.fontSize = size;
			label.color = colour ?? Text;
			label.alignment = anchor;
			label.fontStyle = style;
			foreach (var character in text ?? string.Empty)
			{
				if (character > 127 && !label.font.HasCharacter(character))
				{
					label.font = UITheme.FallbackFont;
					break;
				}
			}
			label.supportRichText = true;
			label.horizontalOverflow = HorizontalWrapMode.Wrap;
			label.verticalOverflow = VerticalWrapMode.Overflow;
			label.raycastTarget = false;

			return label;
		}

		public static Button Button(Transform parent, string text, Action onClick, int size = UITheme.Control, Color? colour = null, float height = 40f)
		{
			var image = Image(parent, colour ?? ButtonColour, "Button");

			var button = image.gameObject.AddComponent<Button>();

			UITheme.Style(image, UITheme.Surface.Leather);
			button.targetGraphic = image;
			button.colors = UITheme.ControlColours();

			button.onClick.AddListener(() => ServiceLocator.Get<IAudioService>()?.Play(Sound.Click, 0.4f));

			if (onClick != null)
				button.onClick.AddListener(() => onClick());

			var label = Label(image.transform, text, size, Text, TextAnchor.MiddleCenter);
			Fill(label.rectTransform, UITheme.Gap);
			label.resizeTextForBestFit = true;
			label.resizeTextMinSize = Mathf.Min(size, UITheme.Meta);
			label.resizeTextMaxSize = size;
			label.verticalOverflow = VerticalWrapMode.Truncate;
			UIFocus.Attach(button);

			Layout(image.gameObject, preferredHeight: height, minHeight: height);

			return button;
		}

		/// <summary>
		/// A single-line text field on a parchment background.
		/// </summary>
		public static InputField InputField(Transform parent, string value, string placeholder, int characterLimit, int size = 26, float height = 56f)
		{
			var image = Surface(parent, UITheme.Surface.Paper, Parchment, "InputField");

			var field = image.gameObject.AddComponent<InputField>();
			field.targetGraphic = image;
			field.colors = UITheme.ControlColours();
			UIFocus.Attach(field);

			var hint = Label(image.transform, placeholder, size, new Color(Ink.r, Ink.g, Ink.b, 0.45f), TextAnchor.MiddleLeft, FontStyle.Italic);
			Fill(hint.rectTransform, 14f);

			var text = Label(image.transform, "", size, Ink, TextAnchor.MiddleLeft);
			Fill(text.rectTransform, 14f);
			text.supportRichText = false;

			field.textComponent = text;
			field.placeholder = hint;
			field.characterLimit = characterLimit;
			field.lineType = UnityEngine.UI.InputField.LineType.SingleLine;
			field.text = value;

			Layout(image.gameObject, preferredHeight: height, minHeight: height);

			return field;
		}

		/// <summary>
		/// A horizontal slider with a filled track and a round handle.
		/// </summary>
		public static Slider Slider(Transform parent, float min, float max, float value, Action<float> onChanged, float height = 30f)
		{
			var root = Rect("Slider", parent);
			Layout(root.gameObject, minHeight: height, preferredHeight: height, flexibleWidth: 1);

			var track = Surface(root, UITheme.Surface.Plaque, UITheme.Track, "Track");
			Place(track.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -5f), new Vector2(0f, 5f));

			var fillArea = Rect("FillArea", root);
			Place(fillArea, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -5f), new Vector2(0f, 5f));

			var fill = Image(fillArea, Accent, "Fill");
			Place(fill.rectTransform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);

			var handleArea = Rect("HandleArea", root);
			Place(handleArea, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));

			var handle = Surface(handleArea, UITheme.Surface.Plaque, Accent, "Handle");
			handle.rectTransform.sizeDelta = new Vector2(22f, 0f);
			handle.rectTransform.anchorMin = new Vector2(0f, 0f);
			handle.rectTransform.anchorMax = new Vector2(0f, 1f);

			var slider = root.gameObject.AddComponent<Slider>();
			slider.fillRect = fill.rectTransform;
			slider.handleRect = handle.rectTransform;
			slider.targetGraphic = handle;
			slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
			slider.minValue = min;
			slider.maxValue = max;
			slider.SetValueWithoutNotify(value);

			slider.colors = UITheme.ControlColours();
			UIFocus.Attach(slider);

			slider.onValueChanged.AddListener(changed => onChanged(changed));

			return slider;
		}

		public static VerticalLayoutGroup Vertical(GameObject target, float spacing = 6f, int padding = 0, bool expandWidth = true)
		{
			var group = target.AddComponent<VerticalLayoutGroup>();
			group.spacing = spacing;
			group.padding = new RectOffset(padding, padding, padding, padding);
			group.childControlWidth = true;
			group.childControlHeight = true;
			group.childForceExpandWidth = expandWidth;
			group.childForceExpandHeight = false;

			return group;
		}

		public static HorizontalLayoutGroup Horizontal(GameObject target, float spacing = 6f, int padding = 0, bool expandWidth = true)
		{
			var group = target.AddComponent<HorizontalLayoutGroup>();
			group.spacing = spacing;
			group.padding = new RectOffset(padding, padding, padding, padding);
			group.childControlWidth = true;
			group.childControlHeight = true;
			group.childForceExpandWidth = expandWidth;
			group.childForceExpandHeight = false;

			return group;
		}

		public static LayoutElement Layout(GameObject target, float minWidth = -1, float minHeight = -1, float preferredWidth = -1, float preferredHeight = -1, float flexibleWidth = -1, float flexibleHeight = -1)
		{
			var element = target.GetComponent<LayoutElement>() ?? target.AddComponent<LayoutElement>();
			element.minWidth = minWidth;
			element.minHeight = minHeight;
			element.preferredWidth = preferredWidth;
			element.preferredHeight = preferredHeight;
			element.flexibleWidth = flexibleWidth;
			element.flexibleHeight = flexibleHeight;

			return element;
		}

		public static RectTransform Row(Transform parent, float spacing = 6f, float height = 40f)
		{
			var row = Rect("Row", parent);

			Horizontal(row.gameObject, spacing);
			Layout(row.gameObject, minHeight: height, preferredHeight: height);

			return row;
		}

		/// <summary>
		/// A horizontal bar. Set fill.fillAmount 0-1.
		/// </summary>
		public static Image Bar(Transform parent, Color back, Color fill, float height = 18f)
		{
			var background = Surface(parent, UITheme.Surface.Plaque, back, "Bar");
			Layout(background.gameObject, minHeight: height, preferredHeight: height);

			var fillImage = Image(background.transform, fill, "Fill");
			Fill(fillImage.rectTransform, 2f);

			fillImage.type = UnityEngine.UI.Image.Type.Filled;
			fillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
			fillImage.sprite = WhiteSprite;

			return fillImage;
		}

		/// <summary>
		/// A vertical scrolling list. Add children to content.
		/// </summary>
		public static RectTransform ScrollList(Transform parent, out ScrollRect scroll, float spacing = 6f)
		{
			var viewport = Image(parent, new Color(0, 0, 0, 0.01f), "Viewport");
			viewport.gameObject.AddComponent<RectMask2D>();

			var content = Rect("Content", viewport.transform);
			content.anchorMin = new Vector2(0, 1);
			content.anchorMax = new Vector2(1, 1);
			content.pivot = new Vector2(0.5f, 1);
			content.offsetMin = Vector2.zero;
			content.offsetMax = Vector2.zero;

			Vertical(content.gameObject, spacing);
			content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

			scroll = viewport.gameObject.AddComponent<ScrollRect>();
			scroll.content = content;
			scroll.viewport = viewport.rectTransform;
			scroll.horizontal = false;
			scroll.scrollSensitivity = 30f;
			scroll.movementType = ScrollRect.MovementType.Clamped;

			return content;
		}

		public static void Clear(Transform parent)
		{
			for (var i = parent.childCount - 1; i >= 0; i--)
				UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
		}

		private static Sprite whiteSprite;

		private static Sprite WhiteSprite
		{
			get
			{
				if (whiteSprite != null)
					return whiteSprite;

				var texture = Texture2D.whiteTexture;
				whiteSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

				return whiteSprite;
			}
		}

		public static string Hex(Color colour) => ColorUtility.ToHtmlStringRGB(colour);
	}
}
