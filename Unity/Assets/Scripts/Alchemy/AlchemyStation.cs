using System.Linq;
using Alchemy.Interfaces;
using Data.Enums;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using GameState.Interfaces;
using Interaction.Interfaces;
using Items.Interfaces;
using UnityEngine;
using Utilities;


namespace Alchemy
{
	// The witch's brewing bench. Owns an AlchemySession, takes ingredients out of the satchel as they go in,
	// and drives the cauldron, fire and mortar visuals from the session state.
	public class AlchemyStation : MonoBehaviour, IInteractable
	{
		public AlchemySession Session { get; } = new();

		public BrewResult? LastResult { get; private set; }

		[SerializeField] private Transform viewpoint;

		[SerializeField] private Renderer liquid;

		[SerializeField] private Transform liquidSurface;

		[SerializeField] private Light fireLight;

		[SerializeField] private Transform flames;

		[SerializeField] private Renderer mortarContents;

		[SerializeField] private Transform pestle;

		private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

		private IEventBusSender eventBus;

		private IInputModeService inputModeService;

		private IInventory inventory;

		private IRecipeBook recipeBook;

		private GameDatabase database;

		private ParticleSystem bubbles;

		private Vector3 liquidRestScale;

		private Vector3 flameRestScale;

		private float pestleKick;

		private bool isOpen;

		public string Prompt => "Brew potions";

		public bool CanInteract => !isOpen;

		public Vector3 Position => transform.position;

		private void Awake()
		{
			eventBus = ServiceLocator.Get<IEventBusSender>();
			inputModeService = ServiceLocator.Get<IInputModeService>();
			inventory = ServiceLocator.Get<IInventory>();
			recipeBook = ServiceLocator.Get<IRecipeBook>();
			database = ServiceLocator.Get<GameDatabase>();

			if (eventBus == null)
				Debug.LogError($"{nameof(eventBus)} is null");

			if (inputModeService == null)
				Debug.LogError($"{nameof(inputModeService)} is null");

			if (inventory == null)
				Debug.LogError($"{nameof(inventory)} is null");

			if (recipeBook == null)
				Debug.LogError($"{nameof(recipeBook)} is null");

			if (liquidSurface != null)
				liquidRestScale = liquidSurface.localScale;

			if (flames != null)
				flameRestScale = flames.localScale;
		}

		private void Start()
		{
			if (liquidSurface != null)
				bubbles = FxUtility.Loop(liquidSurface, Vector3.zero, new Color(0.8f, 1f, 0.9f), 0f, 0.6f, 0.35f, 0.1f);
		}

		public void Interact(GameObject interactor)
		{
			isOpen = true;

			inputModeService.Push(InputMode.Station);

			eventBus.SetCameraOverride(viewpoint);
			eventBus.OpenAlchemy(this);
		}

		public void Close()
		{
			if (!isOpen)
				return;

			isOpen = false;

			inputModeService.Pop();

			eventBus.SetCameraOverride(null);
		}

		public bool Pour(ItemDefinition item)
			=> TakeAnd(item, () => Session.PourBase(item));

		public bool AddWhole(ItemDefinition item)
			=> TakeAnd(item, () => Session.AddWhole(item));

		public bool PlaceInMortar(ItemDefinition item)
			=> TakeAnd(item, () => Session.PlaceInMortar(item));

		public void Grind()
		{
			Session.Grind();

			pestleKick = 1f;
		}

		public BrewResult Bottle()
		{
			var result = Session.Bottle(database.Recipes, database.FailedBrew);

			LastResult = result;

			if (result.Output == null)
				return result;

			inventory.Add(result.Output, result.Bottles);

			if (result.Recipe != null && recipeBook.Learn(result.Recipe))
				eventBus.Notify($"You worked out a new recipe by yourself: {result.Recipe.DisplayName}!");

			FxUtility.Burst(liquidSurface != null ? liquidSurface.position : transform.position, result.Output.Color, 40, 3f);

			return result;
		}

		// Ingredients only leave the satchel once the session accepts them
		private bool TakeAnd(ItemDefinition item, System.Func<bool> action)
		{
			if (inventory.Count(item) <= 0)
				return false;

			if (!action())
				return false;

			inventory.Remove(item, 1);

			return true;
		}

		private void Update()
		{
			Session.Tick(Time.deltaTime);

			UpdateVisuals();
		}

		private void UpdateVisuals()
		{
			if (fireLight != null)
				fireLight.intensity = Mathf.Lerp(0.2f, 4f, Session.Heat) * (0.9f + Mathf.PerlinNoise(Time.time * 6f, 0f) * 0.2f);

			if (flames != null)
				flames.localScale = flameRestScale * Mathf.Lerp(0.3f, 1.4f, Session.Heat);

			if (liquidSurface != null)
			{
				liquidSurface.gameObject.SetActive(Session.HasStarted);

				var wobble = Session.IsBoiling ? Mathf.Sin(Time.time * 18f) * 0.02f : 0f;

				liquidSurface.localScale = liquidRestScale * (1f + wobble);
			}

			if (liquid != null && Session.HasStarted)
			{
				var colour = BrewColour();

				liquid.material.color = colour;
				liquid.material.SetColor(EmissionColor, colour * Mathf.Lerp(0.25f, 0.6f, Session.Heat));
			}

			if (bubbles != null)
			{
				var emission = bubbles.emission;
				emission.rateOverTime = Session.HasStarted && Session.Heat >= AlchemySession.MinBoilHeat ? 25f * Session.Heat : 0f;
			}

			if (mortarContents != null)
			{
				mortarContents.gameObject.SetActive(Session.MortarItem != null);

				if (Session.MortarItem != null)
				{
					mortarContents.material.color = Session.MortarItem.Color;
					mortarContents.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 0.2f, Session.GrindProgress);
				}
			}

			if (pestle != null)
			{
				pestleKick = Mathf.MoveTowards(pestleKick, 0f, Time.deltaTime * 5f);
				pestle.localRotation = Quaternion.Euler(20f + Mathf.Sin(pestleKick * Mathf.PI) * 25f, 0f, 0f);
			}
		}

		private Color BrewColour()
		{
			var colour = Session.Base != null ? Session.Base.Color : Color.grey;

			var contents = Session.Contents.ToList();

			foreach (var item in contents)
				colour = Color.Lerp(colour, item.Color, 0.25f);

			// Keep it jewel bright rather than letting the mix drift to grey
			Color.RGBToHSV(colour, out var hue, out var saturation, out var value);
			colour = Color.HSVToRGB(hue, Mathf.Max(saturation, 0.55f), Mathf.Max(value, 0.8f));

			return Color.Lerp(colour, Color.white, Session.Heat * 0.15f);
		}
	}
}
