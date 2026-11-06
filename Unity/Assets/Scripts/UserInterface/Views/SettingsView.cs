using System;
using Character.Input.Interfaces;
using DependencyInjection;
using Settings;
using Settings.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace UserInterface.Views
{
	// Look sensitivity, invert Y, music and sound volume, and a reference card of the controls for whichever
	// device is in use. Closing returns to whatever opened it (the pause menu or the title screen).
	public class SettingsView : View
	{
		public Action Back { get; set; }

		private IGameSettings settings;

		private IControlScheme controlScheme;

		private Slider look;

		private Slider music;

		private Slider sound;

		private Slider ambience;

		private Text invertLabel;

		private Text controls;

		protected override void OnBuild()
		{
			settings = ServiceLocator.Get<IGameSettings>();
			controlScheme = ServiceLocator.Get<IControlScheme>();

			var shade = UIFactory.Image(Root, UITheme.Scrim, "Shade");
			UIFactory.Fill(shade.rectTransform);

			var panel = UIFactory.Surface(Root, UITheme.Surface.Leather, UITheme.Leather, "Settings");
			UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-640, -400), new Vector2(640, 400));

			var left = UIFactory.Rect("Options", panel.transform);
			UIFactory.Place(left, new Vector2(0f, 0f), new Vector2(0.48f, 1f), new Vector2(40, 40), new Vector2(-10, -40));
			UIFactory.Vertical(left.gameObject, 12f);

			UIFactory.Layout(UIFactory.Label(left, "Settings", UITheme.Heading, UIFactory.Accent, TextAnchor.UpperLeft, FontStyle.Bold).gameObject, preferredHeight: 60);

			look = Setting(left, "Look sensitivity", 0.02f, 0.5f, value => settings.LookSensitivity = value);

			invertLabel = UIFactory.Button(left, "", ToggleInvert, 20, UITheme.Leather, 46).GetComponentInChildren<Text>();

			music = Setting(left, "Music volume", 0f, 1f, value => settings.MusicVolume = value);
			sound = Setting(left, "Sound volume", 0f, 1f, value => settings.SoundVolume = value);
			ambience = Setting(left, "Ambience (footsteps, rain, birds)", 0f, 1f, value => settings.AmbienceVolume = value);

			UIFactory.Button(left, "Reset look sensitivity", () =>
			{
				settings.LookSensitivity = GameSettings.DefaultLookSensitivity;
				Refresh();
			}, 18, UITheme.Leather, 40);

			UIFactory.Button(left, "Back", Close, 22, null, 52);

			var right = UIFactory.Surface(panel.transform, UITheme.Surface.Paper, Color.white, "Controls");
			UIFactory.Place(right.rectTransform, new Vector2(0.52f, 0f), new Vector2(1f, 1f), new Vector2(10, 40), new Vector2(-40, -40));

			controls = UIFactory.Label(right.transform, "", UITheme.Small, UITheme.Ink);
			UIFactory.Fill(controls.rectTransform, 24f);
		}

		private static Slider Setting(Transform parent, string label, float min, float max, Action<float> onChanged)
		{
			UIFactory.Layout(UIFactory.Label(parent, label, 20, UIFactory.TextMuted, TextAnchor.LowerLeft).gameObject, preferredHeight: 28);

			return UIFactory.Slider(parent, min, max, min, onChanged);
		}

		protected override void OnOpen()
		{
			Refresh();
		}

		protected override void OnClose()
		{
			var back = Back;
			Back = null;
			back?.Invoke();
		}

		private void ToggleInvert()
		{
			settings.InvertY = !settings.InvertY;
			Refresh();
		}

		private void Refresh()
		{
			look.SetValueWithoutNotify(settings.LookSensitivity);
			music.SetValueWithoutNotify(settings.MusicVolume);
			sound.SetValueWithoutNotify(settings.SoundVolume);
			ambience.SetValueWithoutNotify(settings.AmbienceVolume);

			invertLabel.text = $"Invert look up/down: {(settings.InvertY ? "on" : "off")}";

			controls.text = controlScheme != null && controlScheme.UsingGamepad ? GamepadControls : KeyboardControls;
		}

		private const string KeyboardControls =
			"<b><size=26>Controls  ·  keyboard and mouse</size></b>\n\n" +
			"Move  <b>WASD</b>\nLook  <b>Mouse</b>\nJump (hold for higher)  <b>Space</b>\nSprint  <b>Shift</b>\nInteract  <b>E</b>\n" +
			"Broom on/off  <b>F</b>\nClimb / dive on broom  <b>Space / Ctrl</b>\n\n" +
			"Ready a spell  <b>1-4</b>\nCast  <b>Left click</b>\nSpell alternate  <b>Right click</b>\nRotate  <b>Q / R</b>\nNext piece, push or pull  <b>Mouse wheel</b>\n\n" +
			"Satchel  <b>Tab</b>\nSpellbook  <b>B</b>\nMenu / back  <b>Esc</b>\nBellows / grind at the bench  <b>Space / G</b>";

		private const string GamepadControls =
			"<b><size=26>Controls  ·  gamepad</size></b>\n\n" +
			"Move  <b>Left stick</b>\nLook  <b>Right stick</b>\nJump (hold for higher)  <b>A</b>\nSprint  <b>Left stick click</b>\nInteract  <b>X</b>\n" +
			"Broom on/off  <b>Y</b>\nClimb / dive on broom  <b>A / B</b>\n\n" +
			"Ready spell 1 / 2  <b>D-pad left / right</b>\nCast  <b>Right trigger</b>\nSpell alternate  <b>Left trigger</b>\nRotate  <b>LB / RB</b>\nNext piece, push or pull  <b>D-pad up / down</b>\n\n" +
			"Satchel  <b>View</b>\nSpellbook  <b>Right stick click</b>\nMenu  <b>Start</b>\nBack  <b>B</b>\nBellows at the bench  <b>A</b>";
	}
}
