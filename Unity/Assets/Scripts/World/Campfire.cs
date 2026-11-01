using UnityEngine;
using Utilities;

namespace World
{
	// Flickering firelight with drifting embers.
	public class Campfire : MonoBehaviour
	{
		[SerializeField] private Light fireLight;

		[SerializeField] private Transform flames;

		private float baseIntensity;

		private Vector3 flameScale;

		private void Start()
		{
			if (fireLight != null)
				baseIntensity = fireLight.intensity;

			if (flames != null)
				flameScale = flames.localScale;

			FxUtility.Loop(transform, Vector3.up * 0.4f, new Color(1f, 0.6f, 0.25f), 6f, 1.2f, 0.25f, 0.07f);
		}

		private void Update()
		{
			var flicker = 0.85f + Mathf.PerlinNoise(Time.time * 5f, 0.3f) * 0.3f;

			if (fireLight != null)
				fireLight.intensity = baseIntensity * flicker;

			if (flames != null)
				flames.localScale = new Vector3(flameScale.x, flameScale.y * flicker, flameScale.z);
		}
	}
}
