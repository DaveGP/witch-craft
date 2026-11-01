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

			var shade = UIFactory.Image(Root, new Color(0.05f, 0.03f, 0.08f, 0.55f), "Shade");
			UIFactory.Fill(shade.rectTransform);

			var panel = UIFactory.Image(Root, UIFactory.Panel, "Name");
			UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-400, -150), new Vector2(400, 150));
			UIFactory.Vertical(panel.gameObject, 14f, 28);

			UIFactory.Layout(UIFactory.Label(panel.transform, "Your name", 30, UIFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold).gameObject, preferredHeight: 40);

			body = UIFactory.Rect("Form", panel.transform);
			UIFactory.Vertical(body.gameObject, 12f);
			UIFactory.Layout(body.gameObject, flexibleHeight: 1);
		}

		protected override void OnOpen()
		{
			UIFactory.Clear(body);

			Form = new NameForm(body, "What should everyone call you?", witch.HasName ? witch.Name : "", "Rename", Rename, Close);
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
