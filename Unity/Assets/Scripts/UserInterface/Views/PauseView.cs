using DependencyInjection;
using Persistence.Interfaces;
using Potions.Interfaces;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UserInterface.Views
{
	public class PauseView : View
	{
		private Text newGameLabel;

		private bool confirmingNewGame;

		protected override void OnBuild()
		{
			var shade = UIFactory.Image(Root, new Color(0.05f, 0.03f, 0.08f, 0.6f), "Shade");
			UIFactory.Fill(shade.rectTransform);

			var panel = UIFactory.Image(Root, UIFactory.Panel, "Pause");
			UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-220, -230), new Vector2(220, 230));
			UIFactory.Vertical(panel.gameObject, 14f, 30);

			UIFactory.Label(panel.transform, "Witch Craft", 44, UIFactory.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);

			UIFactory.Button(panel.transform, "Resume", Close, 24, null, 54);
			UIFactory.Button(panel.transform, "Save game", Save, 24, null, 54);

			var newGame = UIFactory.Button(panel.transform, "New game", NewGame, 24, null, 54);
			newGameLabel = newGame.GetComponentInChildren<Text>();

			UIFactory.Button(panel.transform, "Quit", Quit, 24, null, 54);
		}

		protected override void OnOpen()
		{
			confirmingNewGame = false;
			newGameLabel.text = "New game";
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
