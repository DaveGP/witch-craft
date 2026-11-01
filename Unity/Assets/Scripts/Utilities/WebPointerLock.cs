#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;

namespace Utilities
{
	// Pointer lock for the browser build. Asking for it outside a click makes Safari throw, so the page
	// (Plugins/WebGL/PointerLock.jslib) locks on the next click into the game while lock is wanted.
	public static class WebPointerLock
	{
		[DllImport("__Internal")]
		private static extern void WitchCraft_SetPointerLockWanted(int wanted);

		public static void SetWanted(bool wanted) => WitchCraft_SetPointerLockWanted(wanted ? 1 : 0);
	}
}
#endif
