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
using Sky.Interfaces;
using Interaction;
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

			// Title screen first: start the empty slot
			var inputMode = ServiceLocator.Get<GameState.Interfaces.IInputModeService>();
			Check("The title screen opens first", inputMode.Mode == InputMode.Menu);
			Shot("00_title");

			// Slot one specifically: the others would load a different world
			var startButton = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
				.FirstOrDefault(button => button.isActiveAndEnabled && button.transform.parent.name == "Slot1" && button.GetComponentInChildren<UnityEngine.UI.Text>().text == "Start");

			Check("An empty slot offers Start", startButton != null);
			startButton?.onClick.Invoke();
			yield return new WaitForSeconds(1.5f);

			Check("Starting a slot goes into play", inputMode.Mode == InputMode.Gameplay);

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

			// Settings from the pause menu, and back out to play
			Tap(Key.Escape);
			yield return new WaitForSeconds(0.4f);

			var settingsButton = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
				.FirstOrDefault(button => button.isActiveAndEnabled && button.GetComponentInChildren<UnityEngine.UI.Text>().text == "Settings");

			Check("The pause menu has Settings", settingsButton != null);
			settingsButton?.onClick.Invoke();
			yield return new WaitForSeconds(0.5f);

			Check("Settings opens", FindAnyObjectByType<UserInterface.Views.SettingsView>().IsOpen);
			Shot("06b_settings");

			Tap(Key.Escape);
			yield return new WaitForSeconds(0.4f);
			Check("Escape from settings returns to the pause menu", FindAnyObjectByType<UserInterface.Views.PauseView>().IsOpen);

			Tap(Key.Escape);
			yield return new WaitForSeconds(0.4f);
			Check("Escape again returns to play", inputMode.Mode == InputMode.Gameplay);

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

			// Comfort, paint, the catalogue, coloured building and undo
			Check($"The starter hut is cosy (comfort {home.Comfort})", home.Comfort >= 15);

			var wall = FindObjectsByType<PlacedPiece>(FindObjectsSortMode.None).First(piece => piece.Definition.Id == "wall_timber");
			home.Paint(wall, 6);
			Check("Painting a wall changes and saves its colour", wall.Record.Colour == 6 && wall.GetComponent<Paintable>().Colour == 6);

			Tap(Key.Digit1);
			yield return new WaitForSeconds(0.3f);

			if (caster.ActiveSpell == null || caster.ActiveSpell.Id != "conjure")
			{
				Tap(Key.Digit1);
				yield return new WaitForSeconds(0.3f);
			}

			Check("Conjure is readied for building", caster.ActiveSpell != null && caster.ActiveSpell.Id == "conjure");

			caster.AltCast();
			yield return new WaitForSeconds(0.5f);
			Check("Right-click opens the build catalogue", FindAnyObjectByType<UserInterface.Views.CatalogueView>().IsOpen);
			Shot("17_catalogue");

			Tap(Key.Escape);
			yield return new WaitForSeconds(0.4f);

			var conjureSpell = player.GetComponent<Spells.Effects.ConjureSpell>();
			conjureSpell.Select(database.FindBuildPiece("floor_timber"));
			conjureSpell.SetColour(5);

			// Aim straight down at an empty cell of the plot
			var aim = new GameObject("BuildAim").transform;
			aim.position = new Vector3(-7f, 9f, 7f);
			aim.LookAt(new Vector3(-7f, 0f, 7f));
			movement.Teleport(new Vector3(-7f, 0.2f, 3f));
			eventBus.SetCameraOverride(aim);
			yield return new WaitForSeconds(1.2f);

			var piecesBefore = FindObjectsByType<PlacedPiece>(FindObjectsSortMode.None).Length;
			var timber = database.FindItem("timber");
			var timberBeforeBuild = inventory.Count(timber);

			caster.Cast();
			yield return new WaitForSeconds(0.3f);

			var built = FindObjectsByType<PlacedPiece>(FindObjectsSortMode.None);
			var newest = built.FirstOrDefault(piece => piece.Definition.Id == "floor_timber" && Vector3.Distance(piece.transform.position, new Vector3(-7f, 0f, 7f)) < 2f);

			Check($"Conjure places a coloured floor where it aims ({built.Length - piecesBefore} new)", built.Length == piecesBefore + 1 && newest != null && newest.Record.Colour == 5);
			Shot("18_painted_build");

			caster.Undo();
			yield return new WaitForSeconds(0.3f);

			Check("Z undoes it and refunds the timber", FindObjectsByType<PlacedPiece>(FindObjectsSortMode.None).Length == piecesBefore && inventory.Count(timber) == timberBeforeBuild);

			eventBus.SetCameraOverride(null);
			Destroy(aim.gameObject);

			// The sky spire's final jump: from the last step onto the summit, as a player would
			var spireCentre = new Vector3(50f, 0f, -8f);
			movement.Teleport(spireCentre + new Vector3(0f, 20.8f, -7f));
			yield return new WaitForSeconds(0.6f);

			// Face the camera (and so forward on W) at the summit
			var cameraController = FindAnyObjectByType<global::Camera.CameraController>();
			typeof(global::Camera.CameraController).GetField("yaw", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(cameraController, 0f);
			yield return null;

			Hold(Key.W);
			yield return new WaitForSeconds(0.08f);
			Hold(Key.W, Key.Space);
			yield return new WaitForSeconds(0.5f);
			Hold(Key.W);
			yield return new WaitForSeconds(0.4f);
			Release();
			yield return new WaitForSeconds(0.8f);

			var onSummit = player.transform.position;
			var fromCentre = Vector2.Distance(new Vector2(onSummit.x, onSummit.z), new Vector2(spireCentre.x, spireCentre.z));
			Check($"The last spire jump lands on the summit (height {onSummit.y:0.0}, {fromCentre:0.0}m from centre)", onSummit.y > 21.3f && onSummit.y < 22.5f && fromCentre < 4.5f && movement.IsGrounded);

			// New lands: glyphs, spells and companions beyond the sea
			Check($"Nine glyphs and nine spells exist ({database.Glyphs.Count}, {database.Spells.Count})", database.Glyphs.Count == 9 && database.Spells.Count == 9);

			var recruits = FindObjectsByType<CompanionRecruit>(FindObjectsSortMode.None).Select(found => found.name).ToList();
			Check("Tilly waits on Tidewater and Nova on Starfall Ridge", recruits.Any(found => found.StartsWith("Tilly")) && recruits.Any(found => found.StartsWith("Nova")));

			foreach (var glyphId in new[] { "aqua", "astra", "motus", "vita" })
				spellBook.LearnGlyph(database.FindGlyph(glyphId));

			var bubbleSpell = spellBook.Inscribe(new[] { database.FindGlyph("aqua"), database.FindGlyph("aer") }).Spell;
			var blinkSpell = spellBook.Inscribe(new[] { database.FindGlyph("motus"), database.FindGlyph("astra") }).Spell;
			var rainSpell = spellBook.Inscribe(new[] { database.FindGlyph("vita"), database.FindGlyph("aqua") }).Spell;
			Check("Aqua + Aer, Astra + Motus and Aqua + Vita discover Bubble, Blink and Rain Call",
				bubbleSpell?.Id == "bubble" && blinkSpell?.Id == "blink" && rainSpell?.Id == "rain_call");

			spellBook.Equip(bubbleSpell, 2);
			spellBook.Equip(blinkSpell, 3);
			spellBook.Equip(rainSpell, 0);

			movement.Teleport(new Vector3(-12f, 0.3f, -26f));
			yield return new WaitForSeconds(0.8f);

			Tap(Key.Digit3);
			yield return new WaitForSeconds(0.3f);
			Check("Key 3 readies Bubble", caster.ActiveSpell?.Id == "bubble");

			var floatStart = player.transform.position.y;
			caster.Cast();
			var floatPeak = 0f;

			for (var t = 0f; t < 2.6f; t += Time.deltaTime)
			{
				floatPeak = Mathf.Max(floatPeak, player.transform.position.y - floatStart);
				yield return null;
			}

			Check($"Bubble floats the witch upwards ({floatPeak:0.0}m)", floatPeak > 7f);
			yield return new WaitForSeconds(2.5f);

			Tap(Key.Digit4);
			yield return new WaitForSeconds(0.3f);
			Check("Key 4 readies Blink", caster.ActiveSpell?.Id == "blink");

			typeof(global::Camera.CameraController).GetField("yaw", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(cameraController, 180f);
			yield return null;

			var blinkStart = player.transform.position;
			var blocker = Physics.SphereCast(blinkStart + Vector3.up * 1.04f, 0.3f, cameraController.Forward, out var blockHit, 7f, LayerMask.GetMask("Ground", "Environment"), QueryTriggerInteraction.Ignore)
				? $"{blockHit.collider.name} at {blockHit.distance:0.0}m" : "nothing";
			caster.Cast();
			yield return new WaitForSeconds(0.2f);
			var blinked = Vector3.Distance(Flat(blinkStart), Flat(player.transform.position));
			Check($"Blink steps the witch forward ({blinked:0.0}m; in the way: {blocker}; grounded {movement.IsGrounded})", blinked > 5f && blinked < 8f);

			Tap(Key.Digit1);
			yield return new WaitForSeconds(0.3f);
			Check("Key 1 now readies Rain Call", caster.ActiveSpell?.Id == "rain_call");

			var weatherService = ServiceLocator.Get<IWeather>();
			weatherService.SetWeather(WeatherKind.Clear);
			caster.Cast();
			Check("Rain Call brings the rain", weatherService.Current == WeatherKind.Rain);
			weatherService.SetWeather(WeatherKind.Clear);

			// Seafoam Draught: walk out onto the sea
			var seafoam = database.FindItem("seafoam_draught");
			inventory.Add(seafoam, 1);
			Check("Drinking Seafoam Draught works", potions.Drink(seafoam) && potions.IsActive(PotionEffect.Tidewalk));
			yield return null;

			movement.Teleport(new Vector3(0f, 0.3f, -100f));
			yield return new WaitForSeconds(1.5f);
			Check($"With Seafoam the sea holds the witch up (height {player.transform.position.y:0.0})", player.transform.position.y > -1f && movement.IsGrounded);

			potions.Clear();
			movement.Teleport(new Vector3(0f, 0.3f, -14f));
			yield return new WaitForSeconds(0.8f);

			yield return View("19_tidewater", new Vector3(-28f, 20f, 92f), new Vector3(10f, 0f, 145f));
			yield return View("20_starfall", new Vector3(-12f, 54f, 130f), new Vector3(19f, 49f, 156f));

			// A new morning makes residents fonder, more so with comfort and their favourite piece about
			var friendshipBefore = roster.Friendship(pip);
			ServiceLocator.Get<IDayCycle>().SkipTo(6.5f);
			yield return null;
			var gained = roster.Friendship(pip) - friendshipBefore;
			Check($"Each morning Pip grows fonder (+{gained}, with comfort and their favourite planter)", gained >= 3);

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

			// Day, night and weather
			var dayCycle = ServiceLocator.Get<IDayCycle>();
			var weather = ServiceLocator.Get<IWeather>();

			Check("Day cycle and weather are running", dayCycle != null && weather != null);

			var startHour = dayCycle.Hour;
			yield return new WaitForSeconds(1f);
			Check($"Time passes ({startHour:0.00} to {dayCycle.Hour:0.00})", dayCycle.Hour > startHour);

			var moonpetal = FindObjectsByType<Gatherable>(FindObjectsSortMode.None).First(found => found.name == "Moonpetal");
			Check($"By day, moonpetals are closed (\"{moonpetal.Prompt}\")", moonpetal.Prompt.Contains("opens at night"));

			var day = dayCycle.Day;
			dayCycle.SkipTo(22f);
			yield return new WaitForSeconds(1f);

			Check("Skipping to 22:00 makes it night", dayCycle.IsNight && dayCycle.Darkness > 0.9f);
			Check($"At night, moonpetals open (\"{moonpetal.Prompt}\")", !moonpetal.Prompt.Contains("opens at night"));

			var pipResident = FindObjectsByType<CompanionResident>(FindObjectsSortMode.None).First();
			Check("Pip sleeps at night", pipResident.IsAsleep);

			movement.Teleport(new Vector3(0f, 0.2f, -9f));
			yield return View("15_night", new Vector3(9f, 6f, -14f), new Vector3(0f, 1.5f, 0f));

			// Sleep in the starter bed through the dialogue, as a player would
			var bed = FindAnyObjectByType<Bed>();
			bed.Interact(player.gameObject);
			yield return new WaitForSeconds(0.3f);

			var sleep = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
				.FirstOrDefault(button => button.isActiveAndEnabled && button.GetComponentInChildren<UnityEngine.UI.Text>().text.StartsWith("Sleep"));

			Check("The bed offers to sleep until morning", sleep != null);
			sleep?.onClick.Invoke();
			yield return new WaitForSeconds(0.5f);

			Check($"Sleeping wakes you next morning (day {dayCycle.Day}, {dayCycle.Hour:0.0}h)", dayCycle.Day == day + 1 && dayCycle.Hour >= 7f && dayCycle.Hour < 8f);

			weather.SetWeather(WeatherKind.Rain);
			yield return new WaitForSeconds(9f);
			Check("Rain sets in", weather.IsRaining);
			yield return View("16_rain", new Vector3(9f, 6f, -14f), new Vector3(0f, 1.5f, 0f));

			weather.SetWeather(WeatherKind.Clear);

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
