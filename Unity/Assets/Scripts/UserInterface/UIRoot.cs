using Alchemy;
using Character.Input.Interfaces;
using Data.Enums;
using DependencyInjection;
using GameState;
using Persistence.Interfaces;
using Events.Interfaces;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UserInterface.Views;


namespace UserInterface
{
	// Builds the canvas and every screen at startup, and routes menu keys to them. Only one menu is open at a time.
	public class UIRoot : MonoBehaviour
	{
		private IMenuInputSource menuInput;

		private IEventBus eventBus;

		private HudView hud;

		private SatchelView satchel;

		private SpellbookView spellbook;

		private AlchemyView alchemy;

		private DialogueView dialogue;

		private PauseView pause;

		private SettingsView settings;

		private TitleView title;

		private CatalogueView catalogue;

		private View current;

		private void Awake()
		{
			menuInput = ServiceLocator.Get<IMenuInputSource>();
			eventBus = ServiceLocator.Get<IEventBus>();

			if (menuInput == null)
				Debug.LogError($"{nameof(menuInput)} is null");

			if (eventBus == null)
				Debug.LogError($"{nameof(eventBus)} is null");

			EnsureEventSystem();

			var canvas = BuildCanvas();

			hud = gameObject.AddComponent<HudView>();
			hud.Build(UIFactory.Fill(UIFactory.Rect("Hud", canvas)));

			satchel = AddView<SatchelView>(canvas, "Satchel");
			spellbook = AddView<SpellbookView>(canvas, "Spellbook");
			alchemy = AddView<AlchemyView>(canvas, "Alchemy");
			dialogue = AddView<DialogueView>(canvas, "Dialogue");
			pause = AddView<PauseView>(canvas, "Pause");
			settings = AddView<SettingsView>(canvas, "Settings");
			title = AddView<TitleView>(canvas, "Title");
			catalogue = AddView<CatalogueView>(canvas, "Catalogue");

			pause.OpenSettings = () => ShowSettings(pause);
			title.OpenSettings = () => ShowSettings(title);

			eventBus.AlchemyOpened += OpenAlchemy;
			eventBus.DialogueRequested += OpenDialogue;
			eventBus.BuildCatalogueRequested += OpenCatalogue;
		}

		private void OnDestroy()
		{
			eventBus.AlchemyOpened -= OpenAlchemy;
			eventBus.DialogueRequested -= OpenDialogue;
			eventBus.BuildCatalogueRequested -= OpenCatalogue;
		}

		private void Start()
		{
			// Lock the cursor for play once everything exists
			ServiceLocator.Get<GameState.Interfaces.IInputModeService>().Reset();

			// Straight into play after picking a slot, otherwise the title screen
			if (GameFlow.ConsumeSkipTitle())
				ServiceLocator.Get<ISaveService>().SavingEnabled = true;
			else
				Show(title);
		}

		private static void EnsureEventSystem()
		{
			if (FindAnyObjectByType<EventSystem>() != null)
				return;

			var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

			DontDestroyOnLoad(eventSystem);
		}

		private Transform BuildCanvas()
		{
			var canvasObject = new GameObject("Canvas", typeof(RectTransform));
			canvasObject.transform.SetParent(transform, false);

			var canvas = canvasObject.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = 10;

			var scaler = canvasObject.AddComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920, 1080);
			scaler.matchWidthOrHeight = 0.5f;

			canvasObject.AddComponent<GraphicRaycaster>();

			return canvasObject.transform;
		}

		private T AddView<T>(Transform canvas, string name) where T : View
		{
			var root = UIFactory.Fill(UIFactory.Rect(name, canvas));

			var view = gameObject.AddComponent<T>();
			view.Build(root);

			return view;
		}

		private void Update()
		{
			if (current != null && !current.IsOpen)
				current = null;

			var actions = menuInput.ConsumeMenuInput();

			// B backs out of menus on a gamepad; in play it is the broom's dive button, so only while a menu is up
			var gamepadBack = current != null && Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;

			if (actions.HasFlag(MenuAction.Cancel) || gamepadBack)
			{
				if (current != null)
					current.Cancel();
				else
					Show(pause);
			}

			if (actions.HasFlag(MenuAction.Satchel))
				Toggle(satchel);

			if (actions.HasFlag(MenuAction.Spellbook))
				Toggle(spellbook);

			if (actions.HasFlag(MenuAction.Pump) && current == alchemy)
				alchemy.Pump();
		}

		// Settings return to whichever screen opened them
		private void ShowSettings(View from)
		{
			Show(settings);

			settings.Back = () =>
			{
				current = null;
				Show(from);
			};
		}

		private void Toggle(View view)
		{
			if (current == view)
			{
				view.Close();
				return;
			}

			// Don't stack a satchel on top of a conversation or the brewing bench
			if (current != null)
				return;

			Show(view);
		}

		private void Show(View view)
		{
			if (current != null && current != view)
				current.Close();

			current = view;

			view.Open();
		}

		private void OpenAlchemy(AlchemyStation station)
		{
			if (current != null && current != alchemy)
				current.Close();

			current = alchemy;

			alchemy.Show(station);
		}

		private void OpenCatalogue(Spells.Interfaces.IBuildPalette palette)
		{
			if (current != null)
				return;

			current = catalogue;

			catalogue.Show(palette);
		}

		private void OpenDialogue(DialogueRequest request)
		{
			if (current != null && current != dialogue)
				current.Close();

			current = dialogue;

			dialogue.Show(request);
		}
	}
}
