using System.Collections.Generic;
using UnityEngine;

namespace Data.ScriptableObjects
{
	// A short story hidden somewhere in the world. Found books go on the bookcase at home to be read again.
	[CreateAssetMenu(fileName = "StoryBook", menuName = "Witch Craft/Story Book", order = 16)]
	public class StoryBookDefinition : ScriptableObject
	{
		public string Id;

		public string Title;

		public string Author;

		public Color Color = Color.white;

		[Tooltip("Shown on the bookcase before the book is found")]
		public string Hint;

		[TextArea(4, 10)] public List<string> Pages = new();
	}
}
