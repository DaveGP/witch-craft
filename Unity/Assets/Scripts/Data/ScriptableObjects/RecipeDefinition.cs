using System.Collections.Generic;
using UnityEngine;

namespace Data.ScriptableObjects
{
	[CreateAssetMenu(fileName = "Recipe", menuName = "Witch Craft/Recipe", order = 13)]
	public class RecipeDefinition : ScriptableObject
	{
		public string Id;

		public string DisplayName;

		public ItemDefinition Output;

		[TextArea] public string Flavour;

		public List<AlchemyStep> Steps = new();
	}
}
