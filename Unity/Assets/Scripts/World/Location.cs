using System.Collections.Generic;
using UnityEngine;

namespace World
{
	// A named place: everything within its radius (on the flat) and between its floor and ceiling. Where places
	// overlap, the smallest one is where you are, so the cave inside the hills reads as the cave.
	public class Location : MonoBehaviour
	{
		[SerializeField] private string displayName;

		[SerializeField] private float radius = 20f;

		[SerializeField] private float floor = -10f;

		[SerializeField] private float ceiling = 20f;

		private static readonly List<Location> all = new();

		public string DisplayName => displayName;

		public float Radius => radius;

		private void OnEnable() => all.Add(this);

		private void OnDisable() => all.Remove(this);

		/// <summary>
		/// Whether a point is inside this place, with its radius stretched by a margin (to leave it less eagerly than
		/// to arrive).
		/// </summary>
		public bool Contains(Vector3 point, float margin = 1f)
		{
			var centre = transform.position;
			var flat = new Vector2(point.x - centre.x, point.z - centre.z);

			return flat.sqrMagnitude <= radius * margin * radius * margin
			       && point.y >= centre.y + floor && point.y <= centre.y + ceiling;
		}

		/// <summary>
		/// The smallest named place containing a point, or null out in the open.
		/// </summary>
		public static Location At(Vector3 point)
		{
			Location best = null;

			foreach (var location in all)
				if (location.Contains(point) && (best == null || location.radius < best.radius))
					best = location;

			return best;
		}
	}
}
