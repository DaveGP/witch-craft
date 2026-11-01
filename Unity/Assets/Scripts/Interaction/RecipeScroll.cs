using Alchemy.Interfaces;
using Data.ScriptableObjects;
using DependencyInjection;
using UnityEngine;
using Utilities;


namespace Interaction
{
	using Interfaces;

	// A lost page of someone's recipe book. Reading it adds the recipe to the alchemy bench.
	public class RecipeScroll : MonoBehaviour, IInteractable
	{
		[SerializeField] private RecipeDefinition recipe;

		private IRecipeBook recipeBook;

		public string Prompt => $"Read the scroll ({recipe.DisplayName})";

		public bool CanInteract => !recipeBook.Knows(recipe);

		public Vector3 Position => transform.position;

		private void Awake()
		{
			recipeBook = ServiceLocator.Get<IRecipeBook>();

			if (recipeBook == null)
				Debug.LogError($"{nameof(recipeBook)} is null");
		}

		private void Start()
		{
			if (recipeBook.Knows(recipe))
				gameObject.SetActive(false);
		}

		public void Interact(GameObject interactor)
		{
			if (!recipeBook.Learn(recipe))
				return;

			FxUtility.Burst(transform.position, new Color(1f, 0.9f, 0.6f), 30, 3f);

			gameObject.SetActive(false);
		}
	}
}
