using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


namespace WitchCraftEditor
{
	// Batch entry point for the automated playtest: wipes the save, opens the world and enters play mode.
	// Run with -executeMethod WitchCraftEditor.Playtest.Run -witchcraftPlaytest (no -quit, the scenario exits).
	public static class Playtest
	{
		public const string OptionsEnabledKey = "WitchCraft.Playtest.OptionsEnabled";

		public const string OptionsKey = "WitchCraft.Playtest.Options";

		public static void Run()
		{
			var save = Path.Combine(Application.persistentDataPath, "witchcraft_save.json");

			if (File.Exists(save))
				File.Delete(save);

			// Always test slot one, starting fresh
			PlayerPrefs.SetInt("WitchCraft.Slot", 0);
			PlayerPrefs.Save();

			EditorSceneManager.OpenScene(WorldSceneBuilder.ScenePath, OpenSceneMode.Single);

			// Batch mode can crash reloading the domain for play mode, so skip the reload for this run only.
			// The scenario puts the project's own settings back before it exits.
			SessionState.SetBool(OptionsEnabledKey, EditorSettings.enterPlayModeOptionsEnabled);
			SessionState.SetInt(OptionsKey, (int)EditorSettings.enterPlayModeOptions);

			EditorSettings.enterPlayModeOptionsEnabled = true;
			EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

			EditorApplication.EnterPlaymode();
		}
	}
}
