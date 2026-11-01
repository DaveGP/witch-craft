using UnityEngine;
using UnityEngine.UI;

namespace UserInterface.Views
{
	// Conversation box for companions and the hearth: a title, some words and a few choices.
	public class DialogueView : View
	{
		private Text title;

		private Text body;

		private Image accent;

		private RectTransform options;

		protected override void OnBuild()
		{
			var panel = UIFactory.Image(Root, UIFactory.Panel, "Dialogue");
			UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-520, 60), new Vector2(520, 460));

			accent = UIFactory.Image(panel.transform, UIFactory.Accent, "Accent");
			UIFactory.Place(accent.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -6), Vector2.zero);

			title = UIFactory.Label(panel.transform, "", 30, UIFactory.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
			UIFactory.Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(30, -60), new Vector2(-30, -18));

			body = UIFactory.Label(panel.transform, "", 22, UIFactory.Text);
			UIFactory.Place(body.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(30, 150), new Vector2(-30, -70));

			options = UIFactory.Rect("Options", panel.transform);
			UIFactory.Place(options, Vector2.zero, new Vector2(1, 0), new Vector2(30, 20), new Vector2(-30, 140));

			var grid = options.gameObject.AddComponent<GridLayoutGroup>();
			grid.cellSize = new Vector2(480, 54);
			grid.spacing = new Vector2(20, 10);
			grid.childAlignment = TextAnchor.LowerLeft;
			grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
		}

		public void Show(DialogueRequest request)
		{
			title.text = request.Title;
			body.text = request.Body;
			accent.color = request.Colour;

			UIFactory.Clear(options);

			foreach (var option in request.Options)
			{
				var choice = option;

				UIFactory.Button(options, choice.Label, () => Choose(choice), 21);
			}

			Open();
		}

		private void Choose(DialogueOption option)
		{
			if (!option.KeepOpen)
				Close();

			option.Action?.Invoke();
		}
	}
}
