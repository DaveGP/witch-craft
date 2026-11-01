using UnityEngine;
using Alchemy;
using Alchemy.Interfaces;
using Character.Input;
using Character.Input.Interfaces;
using Character.Interfaces;
using Companions;
using Companions.Interfaces;
using Controls;
using Data.ScriptableObjects;
using Events;
using Events.Interfaces;
using GameState;
using GameState.Interfaces;
using Items;
using Items.Interfaces;
using LocalPlayer;
using LocalPlayer.Interfaces;
using LocalPlayer.Targeting;
using Persistence;
using Persistence.Interfaces;
using Potions;
using Potions.Interfaces;
using Spells;
using Spells.Interfaces;
using SystemManagement;


namespace DependencyInjection
{
	public static class Bootstrapper
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init()
		{
			InitialiseEventBus();

			InitialiseSystemManager();

			InitialiseInput();

			InitialisePlayerContext();

			InitialiseGameState();

			Debug.Log("Bootstrapper: Services registered.");
		}

		private static void InitialiseEventBus()
		{
			var eventBus = new EventBus();

			ServiceLocator.Register<IEventBus>(eventBus);

			ServiceLocator.Register<IEventBusSender>(eventBus);
		}

		private static void InitialiseSystemManager()
		{
			ServiceLocator.Register<SystemManager>(new SystemManager());
		}

		private static void InitialiseInput()
		{
			IInputModeService inputModeService = new InputModeService();

			ServiceLocator.Register(inputModeService);

			PlayerControls playerControls = new();

			IInputReader inputReader = new UnityInputReader(playerControls);

			InputProvider inputProvider = new(inputReader, inputModeService);

			ServiceLocator.Register<ICharacterInputSource>(inputProvider);

			ServiceLocator.Register<ICameraInputSource>(inputProvider);

			ServiceLocator.Register<IMenuInputSource>(inputProvider);

			var systemManager = ServiceLocator.Get<SystemManager>();

			systemManager.Register(inputProvider);
		}

		private static void InitialisePlayerContext()
		{
			ServiceLocator.Register<IPlayerContext>(new PlayerContext());

			ServiceLocator.Register<IActiveTargetController>(new ActiveTargetController());
		}

		private static void InitialiseGameState()
		{
			var database = Resources.Load<GameDatabase>(GameDatabase.ResourcePath);

			if (database == null)
			{
				Debug.LogError("Bootstrapper: No GameDatabase in Resources. Run Witch Craft > Build Everything.");
				database = ScriptableObject.CreateInstance<GameDatabase>();
			}

			ServiceLocator.Register(database);

			var eventBus = ServiceLocator.Get<IEventBusSender>();
			var systemManager = ServiceLocator.Get<SystemManager>();

			var saveService = new SaveService(database);
			ServiceLocator.Register<ISaveService>(saveService);
			systemManager.Register(saveService);

			var inventory = new Inventory(database, eventBus);
			ServiceLocator.Register<IInventory>(inventory);

			var recipeBook = new RecipeBook(database, eventBus);
			ServiceLocator.Register<IRecipeBook>(recipeBook);

			var spellBook = new SpellBook(database, eventBus);
			ServiceLocator.Register<ISpellBook>(spellBook);

			var roster = new CompanionRoster(database, eventBus, spellBook, recipeBook);
			ServiceLocator.Register<ICompanionRoster>(roster);

			var potionEffects = new PotionEffects(inventory, eventBus);
			ServiceLocator.Register<IPotionEffects>(potionEffects);
			systemManager.Register(potionEffects);

			saveService.Register(inventory);
			saveService.Register(recipeBook);
			saveService.Register(spellBook);
			saveService.Register(roster);

			Application.quitting += saveService.Save;
		}
	}
}
