using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data.ScriptableObjects
{
	// Optional art for everything the builders make out of placeholder shapes. Each entry names a slot (the witch,
	// a tree, a build piece...) and may point at a prefab, typically from Assets/ThirdParty. A slot with no prefab,
	// or whose prefab is missing (a fresh clone without the packs), keeps the placeholder.
	// Lives in Resources so it is found in the editor and in builds alike.
	[CreateAssetMenu(fileName = "ArtSet", menuName = "Witch Craft/Art Set", order = 1)]
	public class ArtSet : ScriptableObject
	{
		public const string ResourcePath = "ArtSet";

		/// <summary>
		/// Raised when any Art Set is edited, so placed art can refresh in the editor.
		/// </summary>
		public static event Action AnyChanged;

		public List<ArtEntry> Entries = new();

		private Dictionary<string, ArtEntry> lookup;

		public ArtEntry Find(string key)
		{
			if (lookup == null)
			{
				lookup = new Dictionary<string, ArtEntry>();

				foreach (var entry in Entries)
					if (!string.IsNullOrEmpty(entry.Key))
						lookup[entry.Key] = entry;
			}

			return lookup.TryGetValue(key, out var found) ? found : null;
		}

		public void NotifyChanged()
		{
			lookup = null;
			AnyChanged?.Invoke();
		}

		private void OnValidate() => NotifyChanged();
	}

	[Serializable]
	public class ArtEntry
	{
		[Tooltip("The slot this art fills, written by the builders. Don't rename it")]
		public string Key;

		[Tooltip("What the slot is, as a reminder")]
		public string Note;

		[Tooltip("Prefab to show instead of the placeholder. Empty keeps the placeholder")]
		public GameObject Prefab;

		[Tooltip("Nudge the art to sit right on the placeholder's spot")]
		public Vector3 Offset;

		public Vector3 Rotation;

		public float Scale = 1f;

		[Tooltip("Show the placeholder as well as the art, for lining them up")]
		public bool KeepPlaceholder;
	}
}
