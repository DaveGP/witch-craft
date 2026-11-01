using DependencyInjection;
using Potions.Interfaces;
using UnityEngine.SceneManagement;

namespace GameState
{
	// Moving between the title screen and play. Changing slot reloads the scene so the world (gathered herbs,
	// recruited companions, the home) is rebuilt from that slot's save.
	public static class GameFlow
	{
		private static bool skipTitle;

		public static void Reload(bool showTitle)
		{
			skipTitle = !showTitle;

			ServiceLocator.Get<IPotionEffects>()?.Clear();

			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
		}

		/// <summary>
		/// True once after a reload that should go straight into play.
		/// </summary>
		public static bool ConsumeSkipTitle()
		{
			var skip = skipTitle;
			skipTitle = false;
			return skip;
		}
	}
}
