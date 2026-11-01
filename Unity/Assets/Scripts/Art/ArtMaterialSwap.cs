using Data.ScriptableObjects;
using UnityEngine;

namespace Art
{
	// A surface slot: shows the Art Set's material for this key on the placeholder's own renderers (the island's
	// grass, the beach, path stones), in play and in the editor. Meshes and collision are untouched, and the
	// original materials come back when the slot is empty or its material is missing. The swap is taken off
	// while the scene saves, so the scene never refers to a pack's material.
	[ExecuteAlways]
	public class ArtMaterialSwap : MonoBehaviour
	{
		[SerializeField] private string key;

		[SerializeField] private Renderer[] renderers = new Renderer[0];

		[SerializeField] private Material[] originals = new Material[0];

		public string Key => key;

		public bool HasArt { get; private set; }

		public void Configure(string slot, Renderer[] targets)
		{
			key = slot;
			renderers = targets;
			originals = new Material[targets.Length];

			for (var i = 0; i < targets.Length; i++)
				originals[i] = targets[i] != null ? targets[i].sharedMaterial : null;
		}

		private void OnEnable()
		{
			ArtSet.AnyChanged += Apply;
#if UNITY_EDITOR
			UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += BeforeSave;
			UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += AfterSave;
#endif
			Apply();
		}

		private void OnDisable()
		{
			ArtSet.AnyChanged -= Apply;
#if UNITY_EDITOR
			UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= BeforeSave;
			UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= AfterSave;
#endif
			Restore();
		}

#if UNITY_EDITOR
		private void BeforeSave(UnityEngine.SceneManagement.Scene scene, string path)
		{
			if (scene == gameObject.scene)
				Restore();
		}

		private void AfterSave(UnityEngine.SceneManagement.Scene scene)
		{
			if (scene == gameObject.scene)
				Apply();
		}
#endif

		public void Apply()
		{
			var set = ArtSwap.Set;
			var entry = set != null && !string.IsNullOrEmpty(key) ? set.Find(key) : null;

			if (entry == null || entry.Material == null)
			{
				Restore();
				return;
			}

			foreach (var target in renderers)
				if (target != null)
					target.sharedMaterial = entry.Material;

			HasArt = true;
		}

		private void Restore()
		{
			for (var i = 0; i < renderers.Length && i < originals.Length; i++)
				if (renderers[i] != null && originals[i] != null)
					renderers[i].sharedMaterial = originals[i];

			HasArt = false;
		}
	}
}
