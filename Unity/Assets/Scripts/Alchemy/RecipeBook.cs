using System.Collections.Generic;
using System.Linq;
using Data.Save;
using Data.ScriptableObjects;
using Events.Interfaces;
using Persistence.Interfaces;


namespace Alchemy
{
	using Interfaces;

	public class RecipeBook : IRecipeBook, ISaveParticipant
	{
		private readonly GameDatabase database;

		private readonly IEventBusSender eventBus;

		private readonly List<RecipeDefinition> known = new();

		public IReadOnlyList<RecipeDefinition> Known => known;

		public RecipeBook(GameDatabase database, IEventBusSender eventBus)
		{
			this.database = database;
			this.eventBus = eventBus;
		}

		public bool Knows(RecipeDefinition recipe) => known.Contains(recipe);

		public bool Learn(RecipeDefinition recipe)
		{
			if (recipe == null || Knows(recipe))
				return false;

			known.Add(recipe);

			eventBus.LearnRecipe(recipe);
			eventBus.Notify($"New recipe: {recipe.DisplayName}");

			return true;
		}

		public void Read(SaveData data)
		{
			known.Clear();
			known.AddRange(data.Recipes.Select(database.FindRecipe).Where(recipe => recipe != null));
		}

		public void Write(SaveData data)
		{
			data.Recipes = known.Select(recipe => recipe.Id).ToList();
		}
	}
}
