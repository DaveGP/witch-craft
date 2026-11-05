using System.Collections.Generic;
using UnityEngine;

namespace World
{
	// Somewhere too tangled to fly through, so a climb has to be climbed.
	public class NoFlyZone : MonoBehaviour
	{
		[SerializeField] private float radius = 13f;

		[SerializeField] private string message;

		private static readonly List<NoFlyZone> all = new();

		public string Message => message;

		private void OnEnable() => all.Add(this);

		private void OnDisable() => all.Remove(this);

		/// <summary>
		/// The zone covering this point, at any height, or null.
		/// </summary>
		public static NoFlyZone At(Vector3 point)
		{
			foreach (var zone in all)
			{
				var flat = new Vector2(point.x - zone.transform.position.x, point.z - zone.transform.position.z);

				if (flat.sqrMagnitude <= zone.radius * zone.radius)
					return zone;
			}

			return null;
		}
	}
}
