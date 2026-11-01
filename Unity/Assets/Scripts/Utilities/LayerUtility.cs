using UnityEngine;

namespace Utilities
{
	public static class LayerUtility
	{
		public static int Ground => LayerMask.NameToLayer("Ground");

		public static int Environment => LayerMask.NameToLayer("Environment");

		public static int Interactable => LayerMask.NameToLayer("Interactable");

		public static int Player => LayerMask.NameToLayer("Player");

		/// <summary>
		/// Everything a spell aim ray can land on.
		/// </summary>
		public static LayerMask AimMask => LayerMask.GetMask("Default", "Ground", "Environment", "Interactable");

		public static LayerMask SurfaceMask => LayerMask.GetMask("Default", "Ground", "Environment");

		public static void SetLayerRecursively(GameObject root, int layer)
		{
			foreach (var child in root.GetComponentsInChildren<Transform>(true))
				child.gameObject.layer = layer;
		}
	}
}
