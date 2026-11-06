using DependencyInjection;
using Events.Interfaces;
using Identity.Interfaces;
using UnityEngine;

namespace UserInterface.Views
{
	// Renaming the witch, from the hearth.
	public class NameView : View
	{
		public NameForm Form { get; private set; }

		private IWitchProfile witch;

		private RectTransform body;

		protected override void OnBuild()
		{
			witch = ServiceLocator.Get<IWitchProfile>();

			var shade = UIFactory.Image(Root, UITheme.Scrim, "Shade");
			UIFactory.Fill(shade.rectTransform);

			var panel = UIFactory.Surface(Root, UITheme.Surface.Paper, Color.white, "Name");
			UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-400, -150), new Vector2(400, 150));
			UIFactory.Vertical(panel.gameObject, UITheme.RowGap, UITheme.Padding);

			UIFactory.Layout(UIFactory.Label(panel.transform, "Your name", UITheme.Section, UITheme.Ink, TextAnchor.MiddleLeft, FontStyle.Bold).gameObject, preferredHeight: 40);

			body = UIFactory.Rect("Form", panel.transform);
			UIFactory.Vertical(body.gameObject, 12f);
			UIFactory.Layout(body.gameObject, flexibleHeight: 1);
		}

		protected override void OnOpen()
		{
			UIFactory.Clear(body);

			Form = new NameForm(body, "What should everyone call you?", witch.HasName ? witch.Name : "", "Rename", Rename, Close, UITheme.Ink);
			Form.Field.ActivateInputField();
		}

		private void Rename(string name)
		{
			var changed = name != witch.Name;

			witch.Rename(name);

			Close();

			if (changed)
				ServiceLocator.Get<IEventBusSender>()?.Notify($"From now on you'll be known as {name}.");
		}
	}
}
