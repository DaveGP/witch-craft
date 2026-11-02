using Data.Enums;
using DependencyInjection;
using Home.Interfaces;
using UnityEngine;

namespace Home
{
	// Finishes a roof's art to suit its neighbours: a verge covers each side with no roof beside it, and where two
	// slopes meet back to back only one of them carries the ridge cap they share, so the caps never overlap. Looks
	// again every half second, as pieces are built, unmade and moved.
	public class RoofTrim : MonoBehaviour
	{
		private const float Cell = 2f;

		private const float CheckInterval = 0.5f;

		private IHomeService home;

		private float nextCheck;

		private void Start()
		{
			home = ServiceLocator.Get<IHomeService>();
		}

		private void Update()
		{
			if (home == null || Time.time < nextCheck)
				return;

			nextCheck = Time.time + CheckInterval;
			Refresh();
		}

		private void Refresh()
		{
			var position = transform.position;
			var left = position - transform.right * Cell;
			var right = position + transform.right * Cell;
			var behind = position + transform.forward * Cell;

			bool roofLeft = false, roofRight = false, backToBack = false;

			foreach (var piece in home.Pieces)
			{
				if (piece == null || piece.gameObject == gameObject || piece.Definition == null || piece.Definition.Snap != BuildSnap.Roof)
					continue;

				var other = piece.transform;
				var facing = Vector3.Dot(other.forward, transform.forward);

				if (facing > 0.9f)
				{
					roofLeft |= Near(other.position, left);
					roofRight |= Near(other.position, right);
				}
				else if (facing < -0.9f)
					backToBack |= Near(other.position, behind);
			}

			Show("VergeLeft", !roofLeft);
			Show("VergeRight", !roofRight);

			// Of a back-to-back pair, the one facing south or west carries the cap
			Show("RidgeShared", !backToBack || Mathf.Repeat(transform.eulerAngles.y + 45f, 360f) < 180f);
		}

		private static bool Near(Vector3 a, Vector3 b) => Mathf.Abs(a.x - b.x) < 0.3f && Mathf.Abs(a.z - b.z) < 0.3f && Mathf.Abs(a.y - b.y) < 0.3f;

		private void Show(string part, bool visible)
		{
			foreach (var child in GetComponentsInChildren<Transform>(true))
				if (child.name == part && child.gameObject.activeSelf != visible)
					child.gameObject.SetActive(visible);
		}
	}
}
