using Data.ScriptableObjects;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UserInterface.Views
{
	// An open storybook: two parchment pages at a time, turned with the buttons, the arrow keys or A and D.
	public class BookView : View
	{
		/// <summary>
		/// Where closing the book returns to, when it was opened from the bookcase.
		/// </summary>
		public System.Action Back { get; set; }

		private StoryBookDefinition book;

		private int spread;

		private Text title;

		private Text left;

		private Text right;

		private Text pageNumbers;

		private Button previous;

		private Button next;

		private Image ribbon;

		protected override void OnBuild()
		{
			var shade = UIFactory.Image(Root, new Color(0.05f, 0.03f, 0.08f, 0.7f), "Shade");
			UIFactory.Fill(shade.rectTransform);

			var cover = UIFactory.Image(Root, new Color(0.35f, 0.2f, 0.25f, 1f), "Cover");
			UIFactory.Place(cover.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-760, -420), new Vector2(760, 420));

			ribbon = UIFactory.Image(cover.transform, UIFactory.Accent, "Ribbon");
			UIFactory.Place(ribbon.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-5, 14), new Vector2(5, -14));

			left = Page(cover.transform, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(24, 96), new Vector2(-8, -24));
			right = Page(cover.transform, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(8, 96), new Vector2(-24, -24));

			title = UIFactory.Label(left.transform.parent, "", 34, UIFactory.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
			UIFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(48, -100), new Vector2(-48, -36));

			var footer = UIFactory.Rect("Footer", cover.transform);
			UIFactory.Place(footer, new Vector2(0, 0), new Vector2(1, 0), new Vector2(40, 22), new Vector2(-40, 78));
			UIFactory.Horizontal(footer.gameObject, 16f, 0, false).childAlignment = TextAnchor.MiddleCenter;

			previous = UIFactory.Button(footer, "< Previous", () => Turn(-1), 20, new Color(0.5f, 0.32f, 0.38f), 50);
			UIFactory.Layout(previous.gameObject, minWidth: 200, preferredWidth: 200, minHeight: 50, preferredHeight: 50);

			pageNumbers = UIFactory.Label(footer, "", 20, UIFactory.Text, TextAnchor.MiddleCenter);
			UIFactory.Layout(pageNumbers.gameObject, minWidth: 260, preferredWidth: 260, preferredHeight: 50);

			next = UIFactory.Button(footer, "Next >", () => Turn(1), 20, new Color(0.5f, 0.32f, 0.38f), 50);
			UIFactory.Layout(next.gameObject, minWidth: 200, preferredWidth: 200, minHeight: 50, preferredHeight: 50);

			var close = UIFactory.Button(footer, "Close book", Close, 20, new Color(0.3f, 0.25f, 0.35f), 50);
			UIFactory.Layout(close.gameObject, minWidth: 200, preferredWidth: 200, minHeight: 50, preferredHeight: 50);
		}

		private static Text Page(Transform parent, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
		{
			var page = UIFactory.Image(parent, UIFactory.Parchment, "Page");
			UIFactory.Place(page.rectTransform, min, max, offsetMin, offsetMax);

			var text = UIFactory.Label(page.transform, "", 28, UIFactory.Ink);
			UIFactory.Place(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(48, 40), new Vector2(-48, -40));
			text.lineSpacing = 1.15f;

			return text;
		}

		public void Show(StoryBookDefinition storyBook)
		{
			book = storyBook;
			spread = 0;

			Open();
		}

		public int Spread => spread;

		public StoryBookDefinition Book => book;

		public int SpreadCount => Mathf.Max(1, Mathf.CeilToInt(book.Pages.Count / 2f));

		/// <summary>
		/// Whether either page of the open spread has more text than fits on it.
		/// </summary>
		public bool Overflowing => Overflows(left) || Overflows(right);

		private static bool Overflows(Text text) => text.preferredHeight > text.rectTransform.rect.height + 1f;

		protected override void OnOpen() => Refresh();

		protected override void OnClose()
		{
			var back = Back;
			Back = null;
			back?.Invoke();
		}

		public void Turn(int direction)
		{
			spread = Mathf.Clamp(spread + direction, 0, SpreadCount - 1);

			Refresh();
		}

		private void Refresh()
		{
			if (book == null)
				return;

			var leftIndex = spread * 2;
			var rightIndex = leftIndex + 1;

			title.text = spread == 0 ? $"{book.Title}\n<size=19><i>by {book.Author}</i></size>" : "";

			left.text = (spread == 0 ? "\n\n\n" : "") + (leftIndex < book.Pages.Count ? book.Pages[leftIndex] : "");
			right.text = rightIndex < book.Pages.Count ? book.Pages[rightIndex] : spread == SpreadCount - 1 ? "\n\n<i>The End</i>" : "";

			pageNumbers.text = $"Pages {leftIndex + 1}–{Mathf.Min(rightIndex + 1, book.Pages.Count)} of {book.Pages.Count}";

			previous.interactable = spread > 0;
			next.interactable = spread < SpreadCount - 1;

			ribbon.color = Color.Lerp(book.Color, UIFactory.Accent, 0.3f);
		}

		private void Update()
		{
			if (!IsOpen || Keyboard.current == null)
				return;

			if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
				Turn(1);

			if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
				Turn(-1);
		}
	}
}
