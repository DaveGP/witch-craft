using System.Collections;
using Interaction;
using UnityEngine;
using Utilities;

namespace World
{
	// Patch of enchanted soil. The Bloom spell grows whatever is planted here: a vine platform out in the
	// world, or a harvestable herb in the home garden that can be bloomed again once picked.
	public class BloomSpot : MonoBehaviour
	{
		public bool IsBloomed { get; private set; }

		[SerializeField] private Transform growth;

		[SerializeField] private Light soilGlow;

		[SerializeField] private float growTime = 1.2f;

		[SerializeField] private Color bloomColor = new(0.55f, 1f, 0.5f);

		private Vector3 grownScale;

		private Gatherable harvest;

		private void Awake()
		{
			if (growth == null)
			{
				Debug.LogError($"{name}: {nameof(growth)} is null");
				return;
			}

			grownScale = growth.localScale;

			harvest = growth.GetComponentInChildren<Gatherable>(true);

			if (harvest != null)
				harvest.Collected += Wilt;

			growth.gameObject.SetActive(false);
		}

		public bool Bloom()
		{
			if (IsBloomed || growth == null)
				return false;

			IsBloomed = true;

			StartCoroutine(Grow());

			FxUtility.Burst(transform.position + Vector3.up * 0.3f, bloomColor, 40, 4f);

			return true;
		}

		private IEnumerator Grow()
		{
			growth.gameObject.SetActive(true);

			harvest?.Regrow();

			if (soilGlow != null)
				soilGlow.enabled = false;

			for (var t = 0f; t < growTime; t += Time.deltaTime)
			{
				var eased = 1f - Mathf.Pow(1f - t / growTime, 3f);

				growth.localScale = grownScale * Mathf.Max(0.01f, eased);

				yield return null;
			}

			growth.localScale = grownScale;
		}

		private void Wilt()
		{
			IsBloomed = false;

			growth.gameObject.SetActive(false);

			if (soilGlow != null)
				soilGlow.enabled = true;
		}
	}
}
