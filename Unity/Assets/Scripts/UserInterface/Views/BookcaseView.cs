using System;
using Books.Interfaces;
using Data.ScriptableObjects;
using DependencyInjection;
using UnityEngine;
using UnityEngine.UI;

namespace UserInterface.Views
{
	// The bookcase at home: every storybook found so far, ready to read again, and a hint for each still missing.
	public class BookcaseView : View
	{
		public Action<StoryBookDefinition> Read { get; set; }

		private IBookCollection books;

		private Text heading;

		private RectTransform shelf;

		protected override void OnBuild()
		{
			books = ServiceLocator.Get<IBookCollection>();

			var shade = UIFactory.Image(Root, UITheme.Scrim, "Shade");
			UIFactory.Fill(shade.rectTransform);

			var panel = UIFactory.Surface(Root, UITheme.Surface.Wood, UITheme.WoodTint, "Bookcase");
			UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-560, -420), new Vector2(560, 420));

			heading = UIFactory.Label(panel.transform, "", UITheme.Heading, UIFactory.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
			UIFactory.Place(heading.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -86), new Vector2(-36, -24));

			var hint = UIFactory.Label(panel.transform, "Esc to close", 18, UIFactory.TextMuted, TextAnchor.UpperRight);
			UIFactory.Place(hint.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -60), new Vector2(-36, -30));

			var area = UIFactory.Rect("Shelf", panel.transform);
			UIFactory.Place(area, Vector2.zero, Vector2.one, new Vector2(36, 36), new Vector2(-36, -100));

			shelf = UIFactory.ScrollList(area, out var scroll, 12f);
			UIFactory.Fill((RectTransform)scroll.transform);
		}

		protected override void OnOpen() => Refresh();

		private void Refresh()
		{
			UIFactory.Clear(shelf);

			heading.text = $"Your storybooks  <size=22><color=#{UIFactory.Hex(UIFactory.Text)}>{books.Count} of {books.AllBooks.Count} found</color></size>";

			foreach (var book in books.AllBooks)
				Entry(book);
		}

		private void Entry(StoryBookDefinition book)
		{
			var found = books.Has(book);

			var row = UIFactory.Surface(shelf, UITheme.Surface.Plaque, UITheme.Leather, "Book");
			UIFactory.Horizontal(row.gameObject, 16f, 14, false).childAlignment = TextAnchor.MiddleLeft;
			UIFactory.Layout(row.gameObject, preferredHeight: 84);

			var spine = UIFactory.Image(row.transform, found ? book.Color : UITheme.SoftInk, "Spine");
			UIFactory.Layout(spine.gameObject, minWidth: 22, preferredWidth: 22, preferredHeight: 60);

			var text = found
				? $"<b>{book.Title}</b>\n<size=17><i>by {book.Author}</i></size>"
				: $"<b><color=#{UIFactory.Hex(UIFactory.TextMuted)}>? ? ?</color></b>\n<size=17><i>{book.Hint}</i></size>";

			var label = UIFactory.Label(row.transform, text, 23, UIFactory.Text, TextAnchor.MiddleLeft);
			UIFactory.Layout(label.gameObject, flexibleWidth: 1, preferredHeight: 64);

			if (!found)
				return;

			var read = UIFactory.Button(row.transform, "Read", () => Read?.Invoke(book), 20, UITheme.Mulberry, 48);
			UIFactory.Layout(read.gameObject, minWidth: 130, preferredWidth: 130, minHeight: 48, preferredHeight: 48);
		}
	}
}
