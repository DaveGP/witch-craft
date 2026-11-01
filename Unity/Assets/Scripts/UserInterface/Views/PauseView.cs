using Audio.Interfaces;
using DependencyInjection;
using Persistence.Interfaces;
using Potions.Interfaces;
using Tutorial.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UserInterface.Views
{
	public class PauseView : View
	{
		private Text newGameLabel;

		private bool confirmingNewGame;

		private Text musicLabel;

		private Text soundLabel;

		private GameObject skipTutorial;

		protected override void OnBuild()
		{
			var shade = UIFactory.Image(Root, new Color(0.05f, 0.03f, 0.08f, 0.6f), "Shade");
			UIFactory.Fill(shade.rectTransform);

			var panel = UIFactory.Image(Root, UIFactory.Panel, "Pause");
			UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-230, -300), new Vector2(230, 300));
			UIFactory.Vertical(panel.gameObject, 14f, 30);

			UIFactory.Label(panel.transform, "Witch Craft", 44, UIFactory.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);

			UIFactory.Button(panel.transform, "Resume", Close, 24, null, 54);

			var toggles = UIFactory.Row(panel.transform, 10f, 46f);
			musicLabel = UIFactory.Button(toggles, "", ToggleMusic, 20, new Color(0.3f, 0.25f, 0.4f), 46).GetComponentInChildren<Text>();
			soundLabel = UIFactory.Button(toggles, "", ToggleSound, 20, new Color(0.3f, 0.25f, 0.4f), 46).GetComponentInChildren<Text>();

			skipTutorial = UIFactory.Button(panel.transform, "Skip tutorial", SkipTutorial, 20, new Color(0.3f, 0.25f, 0.4f), 46).gameObject;
			UIFactory.Button(panel.transform, "Save game", Save, 24, null, 54);

			var newGame = UIFactory.Button(panel.transform, "New game", NewGame, 24, null, 54);
			newGameLabel = newGame.GetComponentInChildren<Text>();

			// A browser tab can't quit itself
			if (Application.platform != RuntimePlatform.WebGLPlayer)
				UIFactory.Button(panel.transform, "Quit", Quit, 24, null, 54);
		}

		protected override void OnOpen()
		{
			confirmingNewGame = false;
			newGameLabel.text = "New game";

			RefreshToggles();

			skipTutorial.SetActive(ServiceLocator.Get<ITutorialGuide>()?.IsActive == true);
		}

		private void ToggleMusic()
		{
			var audio = ServiceLocator.Get<IAudioService>();

			if (audio != null)
				audio.MusicEnabled = !audio.MusicEnabled;

			RefreshToggles();
		}

		private void ToggleSound()
		{
			var audio = ServiceLocator.Get<IAudioService>();

			if (audio != null)
				audio.SoundEnabled = !audio.SoundEnabled;

			RefreshToggles();
		}

		private void RefreshToggles()
		{
			var audio = ServiceLocator.Get<IAudioService>();

			musicLabel.text = $"Music: {(audio?.MusicEnabled == true ? "on" : "off")}";
			soundLabel.text = $"Sound: {(audio?.SoundEnabled == true ? "on" : "off")}";
		}

		private void SkipTutorial()
		{
			ServiceLocator.Get<ITutorialGuide>()?.Skip();
			Close();
		}

		private void Save()
		{
			ServiceLocator.Get<ISaveService>().Save();
			Close();
		}

		private void NewGame()
		{
			if (!confirmingNewGame)
			{
				confirmingNewGame = true;
				newGameLabel.text = "Really start over?";
				return;
			}

			Close();

			ServiceLocator.Get<IPotionEffects>().Clear();
			ServiceLocator.Get<ISaveService>().NewGame();

			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}

		private static void Quit()
		{
			ServiceLocator.Get<ISaveService>().Save();

#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}
	}
}
