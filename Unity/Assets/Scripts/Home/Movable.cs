using UnityEngine;
using Utilities;


namespace Home
{
	// Furniture and crates that the Levitate spell can pick up and set down somewhere new.
	public class Movable : MonoBehaviour
	{
		public bool IsHeld { get; private set; }

		/// <summary>
		/// Distance from the pivot down to the lowest point, so it can be rested on a surface.
		/// </summary>
		public float BottomOffset { get; private set; }

		private Rigidbody body;

		private int[] layers;

		private Transform[] children;

		private Light aura;

		private void Awake()
		{
			body = GetComponent<Rigidbody>();
		}

		public void BeginLevitate(Color colour)
		{
			IsHeld = true;

			children = GetComponentsInChildren<Transform>(true);
			layers = new int[children.Length];

			for (var i = 0; i < children.Length; i++)
				layers[i] = children[i].gameObject.layer;

			// Out of the way of aim and ground rays while it floats
			LayerUtility.SetLayerRecursively(gameObject, LayerMask.NameToLayer("Ignore Raycast"));

			BottomOffset = MeasureBottomOffset();

			if (body != null)
				body.isKinematic = true;

			aura = new GameObject("LevitateAura").AddComponent<Light>();
			aura.transform.SetParent(transform, false);
			aura.color = colour;
			aura.range = 4f;
			aura.intensity = 2f;

			RendererTint.Apply(gameObject, colour, 0.6f);
		}

		public void EndLevitate()
		{
			IsHeld = false;

			for (var i = 0; i < children.Length; i++)
				if (children[i] != null)
					children[i].gameObject.layer = layers[i];

			if (body != null)
				body.isKinematic = false;

			if (aura != null)
				Destroy(aura.gameObject);

			RendererTint.Clear(gameObject);
		}

		private float MeasureBottomOffset()
		{
			var renderers = GetComponentsInChildren<Renderer>();

			if (renderers.Length == 0)
				return 0f;

			var bounds = renderers[0].bounds;

			foreach (var renderer in renderers)
				bounds.Encapsulate(renderer.bounds);

			return transform.position.y - bounds.min.y;
		}
	}
}
