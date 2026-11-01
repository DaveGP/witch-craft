using System.Collections.Generic;
using UnityEngine;

namespace World
{
	// Ghostly stepping stones that only become solid near a source of magical light (a Wisp or Glimmer).
	public class HiddenPath : MonoBehaviour
	{
		private static readonly List<Transform> revealers = new();

		public static void AddRevealer(Transform revealer)
		{
			if (!revealers.Contains(revealer))
				revealers.Add(revealer);
		}

		public static void RemoveRevealer(Transform revealer)
		{
			revealers.Remove(revealer);
		}

		[SerializeField] private float revealRadius = 14f;

		private Renderer[] renderers;

		private Collider[] colliders;

		private float visibility;

		private Vector3 fullScale;

		private void Awake()
		{
			renderers = GetComponentsInChildren<Renderer>();
			colliders = GetComponentsInChildren<Collider>();

			fullScale = transform.localScale;

			Apply(0f);
		}

		private void Update()
		{
			var revealed = false;

			revealers.RemoveAll(revealer => revealer == null);

			foreach (var revealer in revealers)
				if ((revealer.position - transform.position).sqrMagnitude < revealRadius * revealRadius)
					revealed = true;

			var target = revealed ? 1f : 0f;

			if (Mathf.Approximately(target, visibility))
				return;

			Apply(Mathf.MoveTowards(visibility, target, Time.deltaTime * 2f));
		}

		private void Apply(float value)
		{
			visibility = value;

			var shown = value > 0.01f;

			foreach (var renderer in renderers)
				renderer.enabled = shown;

			foreach (var collider in colliders)
				collider.enabled = value > 0.6f;

			transform.localScale = fullScale * Mathf.Lerp(0.6f, 1f, value);
		}
	}
}
