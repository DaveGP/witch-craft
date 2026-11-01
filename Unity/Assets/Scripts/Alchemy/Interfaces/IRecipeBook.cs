using System.Collections.Generic;
using Data.ScriptableObjects;

namespace Alchemy.Interfaces
{
	public interface IRecipeBook
	{
		IReadOnlyList<RecipeDefinition> Known { get; }

		bool Knows(RecipeDefinition recipe);

		/// <summary>
		/// Adds the recipe to the book. Returns false if it was already known.
		/// </summary>
		bool Learn(RecipeDefinition recipe);
	}
}
