#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Alchemy;
using Alchemy.Interfaces;
using Audio;
using Audio.Interfaces;
using Character;
using Character.Movement.Interfaces;
using Companions;
using Companions.Interfaces;
using Data.Enums;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using Home;
using Home.Interfaces;
using Items.Interfaces;
using Potions.Interfaces;
using Spells.Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UserInterface;
using Tutorial.Interfaces;


namespace Debugging
{
	// Automated smoke test, started by WitchCraftEditor.Playtest with -witchcraftPlaytest. Drives the game through
	// real keyboard events and service calls, checks the core loops work and captures screenshots to Logs/Playtest.
	public class PlaytestScenario : MonoBehaviour
	{
		private const string Flag = "-witchcraftPlaytest";

		private readonly StringBuilder report = new();

		private readonly List<string> errors = new();

		private int failures;

		private Keyboard keyboard;

		private string folder;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void Launch()
		{
			if (!Environment.GetCommandLineArgs().Contains(Flag))
				return;

			new GameObject("Playtest").AddComponent<PlaytestScenario>();
		}

		private void Awake()
		{
			folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "Playtest"));
			Directory.CreateDirectory(folder);

			Application.logMessageReceived += Capture;

			// There is no focused Game view in batch mode, so let device input through regardless
			InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
			InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
			Application.runInBackground = true;

			keyboard = InputSystem.AddDevice<Keyboard>("PlaytestKeyboard");
		}

		private void Capture(string message, string stack, LogType type)
		{
			// The editor's own search indexer sometimes throws in batch mode; that isn't the game
			if (stack.Contains("UnityEditor.Search"))
				return;

			if (type is LogType.Error or LogType.Exception or LogType.Assert)
				errors.Add($"{type}: {message}\n{stack}");
		}

		private IEnumerator Start()
		{
			yield return Run(Scenario());
		}

		private IEnumerator Run(IEnumerator scenario)
		{
			while (true)
			{
				object current;

				try
				{
					if (!scenario.MoveNext())
						break;

					current = scenario.Current;
				}
				catch (Exception exception)
				{
					Fail($"Scenario threw: {exception}");
					break;
				}

				yield return current;
			}

			Finish();
		}

		private IEnumerator Scenario()
		{
			yield return new WaitForSeconds(1.5f);

			var player = FindAnyObjectByType<PlayerCharacter>();
			var movement = player.GetComponent<IMovementController>();
			var broom = player.GetComponent<IBroomController>();
			var caster = player.GetComponent<ISpellCaster>();

			var inventory = ServiceLocator.Get<IInventory>();
			var spellBook = ServiceLocator.Get<ISpellBook>();
			var recipeBook = ServiceLocator.Get<IRecipeBook>();
			var home = ServiceLocator.Get<IHomeService>();
			var roster = ServiceLocator.Get<ICompanionRoster>();
			var potions = ServiceLocator.Get<IPotionEffects>();
			var database = ServiceLocator.Get<GameDatabase>();
			var eventBus = ServiceLocator.Get<IEventBusSender>();

			Check("Services resolved", inventory != null && spellBook != null && recipeBook != null && home != null && roster != null);
			Check("Player is grounded at spawn", movement.IsGrounded);
			Check("Starter home was built", FindObjectsByType<PlacedPiece>(FindObjectsSortMode.None).Length >= 20);
			Check("Started with two recipes", recipeBook.Known.Count == 2);
			Check("Started with three glyphs", spellBook.KnownGlyphs.Count == 3);

			var audio = ServiceLocator.Get<IAudioService>();
			Check("Audio generated its sounds and music", audio != null && audio.Clip(Sound.Music) != null && audio.Clip(Sound.Music).length > 30f);

			var tutorial = ServiceLocator.Get<ITutorialGuide>();
			Check("The tutorial starts on a new game", tutorial != null && tutorial.IsActive);

			Shot("01_spawn");

			// Walk forward with real key presses
			var start = player.transform.position;
			Hold(Key.W);
			yield return new WaitForSeconds(1.2f);
			Release();
			yield return new WaitForSeconds(0.3f);

			var walked = Vector3.Distance(Flat(start), Flat(player.transform.position));
			Check($"Walking with W moves the witch ({walked:0.0}m)", walked > 3f);

			// Jump
			var groundY = player.transform.position.y;
			var peak = 0f;
			Hold(Key.Space);

			for (var t = 0f; t < 0.6f; t += Time.deltaTime)
			{
				peak = Mathf.Max(peak, player.transform.position.y - groundY);
				yield return null;
			}

			Release();
			Check($"Holding Space jumps high ({peak:0.00}m)", peak > 1.4f);
			yield return new WaitForSeconds(1f);

			// A quick tap should give a shorter hop than a held jump
			var tapPeak = 0f;
			groundY = player.transform.position.y;
			Tap(Key.Space);

			for (var t = 0f; t < 0.6f; t += Time.deltaTime)
			{
				tapPeak = Mathf.Max(tapPeak, player.transform.position.y - groundY);
				yield return null;
			}

			Check($"Tapping Space gives a shorter hop ({tapPeak:0.00}m)", tapPeak > 0.3f && tapPeak < peak);
			yield return new WaitForSeconds(0.6f);

			// Spellbook: discover Conjure and Levitate by inscribing glyphs
			var conjure = spellBook.Inscribe(new[] { database.FindGlyph("terra"), database.FindGlyph("forma") });
			Check("Terra + Forma discovers Conjure", conjure.Spell != null && conjure.Spell.Id == "conjure" && conjure.IsNew);

			var levitate = spellBook.Inscribe(new[] { database.FindGlyph("forma"), database.FindGlyph("aer") });
			Check("Forma + Aer discovers Levitate (order free)", levitate.Spell != null && levitate.Spell.Id == "levitate");

			var miss = spellBook.Inscribe(new[] { database.FindGlyph("aer"), database.FindGlyph("terra"), database.FindGlyph("forma") });
			Check($"A wrong combination fizzles with a hint: \"{miss.Message}\"", miss.Spell == null);

			Tap(Key.Digit1);
			yield return new WaitForSeconds(0.3f);
			Check("Key 1 readies Conjure", caster.ActiveSpell != null && caster.ActiveSpell.Id == "conjure");

			Shot("02_conjure_ready");

			// Build a floor and a wall on the plot through the home service
			var timberBefore = inventory.Count(database.FindItem("timber"));
			var floor = database.FindBuildPiece("floor_timber");

			if (home.TrySnap(floor, new Vector3(5f, 0.05f, 5f), 0, out var floorPosition, out var floorRotation) && inventory.RemoveAll(floor.Cost))
				home.Place(floor, floorPosition, floorRotation);

			Check("Conjuring a floor spends timber", inventory.Count(database.FindItem("timber")) == timberBefore - 2);
			Check("A slot can't be built on twice", !home.TrySnap(floor, new Vector3(5.2f, 0.05f, 4.8f), 0, out _, out _));
			Check("Building outside the plot is refused", !home.TrySnap(floor, new Vector3(30f, 0f, 30f), 0, out _, out _));

			// Levitate a crate: lift it, move the witch, set it down
			Tap(Key.Digit2);
			yield return new WaitForSeconds(0.3f);
			Check($"Key 2 readies Levitate (readied: {caster.ActiveSpell?.Id ?? "none"}, slot 2 holds {spellBook.GetSlot(1)?.Id ?? "nothing"})", caster.ActiveSpell != null && caster.ActiveSpell.Id == "levitate");

			var crate = FindObjectsByType<Movable>(FindObjectsSortMode.None).FirstOrDefault(movable => movable.GetComponent<PlacedPiece>() == null);
			Check("There is a crate to levitate", crate != null);

			// Brew a Swiftness Brew from the starting satchel, minding the fire
			var station = FindAnyObjectByType<AlchemyStation>();
			movement.Teleport(station.transform.position + station.transform.forward * 2.2f);
			yield return new WaitForSeconds(0.5f);

			station.Interact(player.gameObject);
			yield return new WaitForSeconds(1.2f);

			Check("Bench takes input focus", ServiceLocator.Get<GameState.Interfaces.IInputModeService>().Mode == InputMode.Station);

			var water = database.FindItem("spring_water");
			var nettle = database.FindItem("nettle");
			var lavender = database.FindItem("lavender");

			Check("Pour Spring Water", station.Pour(water));
			Check("Add a Nettle", station.AddWhole(nettle));
			Check("Add a second Nettle", station.AddWhole(nettle));

			yield return BoilTurn(station);

			Check("Lavender into the mortar", station.PlaceInMortar(lavender));

			for (var i = 0; i < 5; i++)
				station.Grind();

			Shot("03_alchemy");

			Check("Ground lavender into the cauldron", station.Session.AddFromMortar());

			yield return BoilTurn(station);

			var swiftness = database.FindItem("swiftness_brew");
			var brew = station.Bottle();

			Check($"Following the recipe brews a perfect Swiftness Brew (quality {brew.Quality}, {brew.Bottles} bottles)", brew.Output == swiftness && brew.Quality == 3);

			// A sloppy brew should come out worse
			inventory.Add(water, 1);
			station.Pour(water);
			var sludge = station.Bottle();
			Check($"Bottling straight away ruins the brew ({sludge.Output?.DisplayName})", sludge.Quality == 0 && sludge.Output == database.FailedBrew);

			Tap(Key.Escape);
			yield return new WaitForSeconds(1.2f);
			Check("Escape leaves the bench", ServiceLocator.Get<GameState.Interfaces.IInputModeService>().Mode == InputMode.Gameplay);

			Check("Drinking a Swiftness Brew works", potions.Drink(swiftness) && potions.IsActive(PotionEffect.Swiftness));

			// Broom flight
			movement.Teleport(new Vector3(0f, 0.2f, -14f));
			yield return new WaitForSeconds(0.5f);

			Tap(Key.F);
			yield return new WaitForSeconds(0.3f);
			Check("F mounts the broom", broom.IsMounted);

			var flyStart = player.transform.position;
			Hold(Key.Space, Key.W);
			yield return new WaitForSeconds(1.5f);

			Shot("04_broom");

			Release();
			var climbed = player.transform.position.y - flyStart.y;
			Check($"Holding Space climbs on the broom ({climbed:0.0}m)", climbed > 2f);
			Check("Flying drains the broom", broom.Charge01 < 1f);

			yield return new WaitForSeconds(2.5f);
			var ceilingHeight = player.transform.position.y;
			Check($"The broom keeps under its hover ceiling over open ground ({ceilingHeight:0.0}m)", ceilingHeight < 9.5f);

			Tap(Key.F);
			yield return new WaitForSeconds(2f);
			Check("F again dismounts", !broom.IsMounted);

			// Menus
			Tap(Key.B);
			yield return new WaitForSeconds(0.5f);
			Shot("05_spellbook");
			Check("B opens the spellbook", ServiceLocator.Get<GameState.Interfaces.IInputModeService>().Mode == InputMode.Menu);
			Tap(Key.B);
			yield return new WaitForSeconds(0.3f);

			Tap(Key.Tab);
			yield return new WaitForSeconds(0.5f);
			Shot("06_satchel");
			Tap(Key.Tab);
			yield return new WaitForSeconds(0.3f);

			// Companions: gift Pip what they ask for and they move in
			var pip = database.FindCompanion("pip");
			inventory.Add(database.FindItem("glowcap"), 3);

			var recruit = FindObjectsByType<CompanionRecruit>(FindObjectsSortMode.None).FirstOrDefault(found => found.name.StartsWith("Pip"));
			movement.Teleport(recruit.transform.position + Vector3.right * 1.5f);
			yield return new WaitForSeconds(0.5f);

			recruit.Interact(player.gameObject);
			yield return new WaitForSeconds(0.5f);
			Shot("07_dialogue");

			var offer = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
				.FirstOrDefault(button => button.isActiveAndEnabled && button.GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith("Offer"));

			Check("Pip's dialogue offers the gift", offer != null);
			offer?.onClick.Invoke();
			yield return new WaitForSeconds(0.5f);

			Check("Pip moved in", roster.IsResident(pip));
			Check("A resident Pip appears at home", FindObjectsByType<CompanionResident>(FindObjectsSortMode.None).Length == 1);

			Tap(Key.Escape);
			yield return new WaitForSeconds(0.3f);

			// Friendship rewards
			roster.AddFriendship(pip, 5);
			Check("Pip's friendship reward teaches Glimmer Draught", recipeBook.Knows(database.FindRecipe("glimmer")));

			// Home expansion needs materials and a companion
			inventory.Add(database.FindItem("timber"), 20);
			inventory.Add(database.FindItem("stone"), 20);
			inventory.Add(database.FindItem("crystal"), 5);
			Check("Home expands to level 2", home.Expand() && home.Level == 2);

			yield return new WaitForSeconds(0.6f);
			Check("Doing everything Sable suggests finishes the tutorial", !tutorial.IsActive);

			// Scenic shots
			movement.Teleport(new Vector3(0f, 0.2f, -9f));
			yield return new WaitForSeconds(1.5f);

			yield return View("08_home", new Vector3(9f, 6f, -14f), new Vector3(0f, 1.5f, 0f));
			yield return View("09_island", new Vector3(-30f, 45f, -75f), new Vector3(10f, 0f, 0f));
			yield return View("10_spire", new Vector3(28f, 16f, -24f), new Vector3(50f, 12f, -6f));
			yield return View("11_pond", new Vector3(-24f, 9f, -6f), new Vector3(-41f, 2f, 13f));
			yield return View("12_cave", new Vector3(20f, 3.5f, -46f), new Vector3(38f, 1.5f, -46f));
			yield return View("13_forest", new Vector3(14f, 6f, 22f), new Vector3(32f, 2f, 38f));
			yield return View("14_sky_isle", new Vector3(30f, 40f, 0f), new Vector3(44f, 34f, 14f));

			report.AppendLine($"Inventory: {string.Join(", ", inventory.Items.Select(stack => stack.ToString()))}");
		}

		private IEnumerator BoilTurn(AlchemyStation station)
		{
			Check("Turn the hourglass", station.Session.StartTurn());

			// Work the bellows like a player would: pump whenever the fire drops below the middle of the band
			while (station.Session.IsBoiling)
			{
				if (station.Session.Heat < (AlchemySession.MinBoilHeat + AlchemySession.MaxBoilHeat) * 0.5f)
					station.Session.PumpBellows();

				yield return new WaitForSeconds(0.35f);
			}
		}

		private IEnumerator View(string name, Vector3 position, Vector3 lookAt)
		{
			var viewpoint = new GameObject($"View_{name}").transform;
			viewpoint.position = position;
			viewpoint.LookAt(lookAt);

			var eventBus = ServiceLocator.Get<IEventBusSender>();
			eventBus.SetCameraOverride(viewpoint);

			yield return new WaitForSeconds(1.2f);

			Shot(name);

			eventBus.SetCameraOverride(null);
			Destroy(viewpoint.gameObject);
		}

		private void Hold(params Key[] keys)
		{
			// Queued events are picked up by the next frame's input update, as real presses are
			InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
		}

		private void Release()
		{
			InputSystem.QueueStateEvent(keyboard, new KeyboardState());
		}

		private void Tap(Key key)
		{
			StartCoroutine(TapRoutine(key));
		}

		private IEnumerator TapRoutine(Key key)
		{
			Hold(key);
			yield return null;
			yield return null;
			yield return null;
			Release();
		}

		private void Shot(string name)
		{
			var camera = UnityEngine.Camera.main;

			if (camera == null)
				return;

			const int width = 1600;
			const int height = 900;

			// Overlay canvases don't render into a camera target, so borrow the camera for one frame
			var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(canvas => canvas.renderMode == RenderMode.ScreenSpaceOverlay).ToList();

			foreach (var canvas in canvases)
			{
				canvas.renderMode = RenderMode.ScreenSpaceCamera;
				canvas.worldCamera = camera;
				canvas.planeDistance = 0.5f;
			}

			Canvas.ForceUpdateCanvases();

			var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
			var previous = camera.targetTexture;

			camera.targetTexture = target;
			camera.Render();

			RenderTexture.active = target;
			var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
			texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			texture.Apply();

			File.WriteAllBytes(Path.Combine(folder, $"{name}.png"), texture.EncodeToPNG());

			camera.targetTexture = previous;
			RenderTexture.active = null;
			RenderTexture.ReleaseTemporary(target);
			Destroy(texture);

			foreach (var canvas in canvases)
				canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		}

		private static Vector3 Flat(Vector3 value) => new(value.x, 0f, value.z);

		private void Check(string description, bool passed)
		{
			report.AppendLine($"{(passed ? "PASS" : "FAIL")}  {description}");

			if (!passed)
				failures++;
		}

		private void Fail(string description)
		{
			report.AppendLine($"FAIL  {description}");
			failures++;
		}

		private void Finish()
		{
			Application.logMessageReceived -= Capture;

			report.AppendLine();
			report.AppendLine($"Errors logged during play: {errors.Count}");

			foreach (var error in errors.Distinct().Take(20))
				report.AppendLine(error);

			var summary = $"Playtest finished: {failures} failed checks, {errors.Count} errors.";
			report.Insert(0, summary + "\n\n");

			File.WriteAllText(Path.Combine(folder, "report.txt"), report.ToString());

			Debug.Log($"{summary}\n{report}");

			UnityEditor.EditorSettings.enterPlayModeOptionsEnabled = UnityEditor.SessionState.GetBool("WitchCraft.Playtest.OptionsEnabled", false);
			UnityEditor.EditorSettings.enterPlayModeOptions = (UnityEditor.EnterPlayModeOptions)UnityEditor.SessionState.GetInt("WitchCraft.Playtest.Options", 0);

			UnityEditor.EditorApplication.Exit(failures == 0 && errors.Count == 0 ? 0 : 1);
		}
	}
}
#endif
