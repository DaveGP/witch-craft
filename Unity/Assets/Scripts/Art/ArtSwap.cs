using Data.ScriptableObjects;
using UnityEngine;

namespace Art
{
	// Shows the Art Set's prefab for this slot in place of the placeholder renderers, in play and in the editor.
	// Only looks change: the placeholder's colliders, scripts and lights stay, so gameplay is identical with or
	// without art. Art made in the editor is never saved into the scene or prefab.
	[ExecuteAlways]
	public class ArtSwap : MonoBehaviour
	{
		private static ArtSet artSet;

		private static bool searched;

		[SerializeField] private string key;

		[Tooltip("The placeholder renderers hidden while art is shown")]
		[SerializeField] private Renderer[] placeholder = new Renderer[0];

		/// <summary>
		/// The art's Animator, when the art has one, for animating rigged characters.
		/// </summary>
		public Animator ArtAnimator { get; private set; }

		/// <summary>
		/// The instantiated art, or null while the placeholder shows.
		/// </summary>
		public GameObject Art => art;

		public bool HasArt => art != null;

		public string Key => key;

		private GameObject art;

		public static ArtSet Set
		{
			get
			{
				if (!searched || artSet == null)
				{
					artSet = Resources.Load<ArtSet>(ArtSet.ResourcePath);
					searched = true;
				}

				return artSet;
			}
		}

		public void Configure(string slot, Renderer[] renderers)
		{
			key = slot;
			placeholder = renderers;
		}

		private void OnEnable()
		{
			ArtSet.AnyChanged += Apply;
			Apply();
		}

		private void OnDisable()
		{
			ArtSet.AnyChanged -= Apply;
			Clear();
		}

		public void Apply()
		{
			Clear();

			var entry = Set != null && !string.IsNullOrEmpty(key) ? Set.Find(key) : null;

			// A missing prefab (packs not on this machine) compares equal to null, so the placeholder stays
			if (entry == null || entry.Prefab == null)
				return;

			art = Instantiate(entry.Prefab, transform);
			art.name = $"Art ({entry.Prefab.name})";
			art.transform.localPosition = entry.Offset;
			art.transform.localRotation = Quaternion.Euler(entry.Rotation);
			art.transform.localScale = Vector3.one * entry.Scale;

			if (!Application.isPlaying)
				foreach (var child in art.GetComponentsInChildren<Transform>(true))
					child.gameObject.hideFlags = HideFlags.DontSave | HideFlags.NotEditable;

			// Art is only a picture: collision comes from the placeholder
			foreach (var collider in art.GetComponentsInChildren<Collider>(true))
				DestroyImmediate(collider);

			foreach (var body in art.GetComponentsInChildren<Rigidbody>(true))
				DestroyImmediate(body);

			foreach (var child in art.GetComponentsInChildren<Transform>(true))
				child.gameObject.layer = gameObject.layer;

			ArtAnimator = art.GetComponentInChildren<Animator>();

			if (!entry.KeepPlaceholder)
				SetPlaceholderVisible(false);
		}

		private void Clear()
		{
			if (art != null)
			{
				if (Application.isPlaying)
					Destroy(art);
				else
					DestroyImmediate(art);
			}

			art = null;
			ArtAnimator = null;

			SetPlaceholderVisible(true);
		}

		private void SetPlaceholderVisible(bool visible)
		{
			foreach (var renderer in placeholder)
				if (renderer != null)
					renderer.enabled = visible;
		}
	}
}
