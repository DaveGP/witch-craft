using System;
using Identity;
using UnityEngine;
using UnityEngine.UI;

namespace UserInterface
{
	// Naming the witch: a text field filled with a suggestion, a button for another suggestion (so a gamepad never
	// has to type), and a confirm button. Used by the title screen for new games and by the hearth to rename.
	public class NameForm
	{
		public InputField Field { get; }

		public Button Confirm { get; }

		private readonly Action<string> confirmed;

		public NameForm(Transform parent, string prompt, string current, string confirmLabel, Action<string> onConfirm, Action onCancel)
		{
			confirmed = onConfirm;

			UIFactory.Layout(UIFactory.Label(parent, prompt, 22, UIFactory.Text, TextAnchor.MiddleLeft).gameObject, preferredHeight: 34);

			var start = string.IsNullOrEmpty(current) ? WitchProfile.Suggest() : current;

			Field = UIFactory.InputField(parent, start, "Your witch's name", WitchProfile.MaxLength);
			Field.onSubmit.AddListener(_ => Submit());
			Field.onValueChanged.AddListener(_ => Refresh());

			var buttons = UIFactory.Row(parent, 12f, 56f);

			UIFactory.Button(buttons, "Suggest another", () => Field.text = WitchProfile.Suggest(Field.text), 20, new Color(0.3f, 0.25f, 0.4f), 56);

			Confirm = UIFactory.Button(buttons, confirmLabel, Submit, 22, new Color(0.45f, 0.28f, 0.52f), 56);

			if (onCancel != null)
				UIFactory.Button(buttons, "Back", () => onCancel(), 20, new Color(0.3f, 0.25f, 0.4f), 56);

			Refresh();
		}

		private void Refresh()
		{
			Confirm.interactable = WitchProfile.Clean(Field.text).Length > 0;
		}

		public void Submit()
		{
			var name = WitchProfile.Clean(Field.text);

			if (name.Length > 0)
				confirmed?.Invoke(name);
		}
	}
}
