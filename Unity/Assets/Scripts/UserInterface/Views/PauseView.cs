using System;
using DependencyInjection;
using GameState;
using Persistence.Interfaces;
using Tutorial.Interfaces;
using UnityEngine;

namespace UserInterface.Views
{
	public class PauseView : View
	{
		public Action OpenSettings { get; set; }

		private GameObject skipTutorial;

		protected override void OnBuild()
		{
			var shade = UIFactory.Image(Root, UITheme.Scrim, "Shade");
			UIFactory.Fill(shade.rectTransform);

			var panel = UIFactory.Surface(Root, UITheme.Surface.Leather, UITheme.Leather, "Pause");
			UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-230, -290), new Vector2(230, 290));
			UIFactory.Vertical(panel.gameObject, 14f, 30);

			UIFactory.Label(panel.transform, "Witch Craft", UITheme.Heading, UIFactory.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);

			UIFactory.Button(panel.transform, "Resume", Close, 24, null, 54);
			UIFactory.Button(panel.transform, "Settings", () => OpenSettings?.Invoke(), 24, null, 54);
			UIFactory.Button(panel.transform, "Save game", Save, 24, null, 54);

			skipTutorial = UIFactory.Button(panel.transform, "Skip tutorial", SkipTutorial, 20, UITheme.Leather, 46).gameObject;

			UIFactory.Button(panel.transform, "Save and return to title", ReturnToTitle, 22, UITheme.Leather, 50);

			// A browser tab can't quit itself
			if (Application.platform != RuntimePlatform.WebGLPlayer)
				UIFactory.Button(panel.transform, "Quit", Quit, 24, null, 54);
		}

		protected override void OnOpen()
		{
			skipTutorial.SetActive(ServiceLocator.Get<ITutorialGuide>()?.IsActive == true);
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

		private void ReturnToTitle()
		{
			var saveService = ServiceLocator.Get<ISaveService>();

			saveService.Save();
			saveService.SavingEnabled = false;

			Close();

			GameFlow.Reload(true);
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
