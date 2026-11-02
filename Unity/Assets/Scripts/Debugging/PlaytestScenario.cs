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
using Character.Animation;
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
			yield return new WaitForSeconds(0.5f);

			// A new witch is named first; the suggestion is already filled in, and stray spaces and markup are tidied
			var naming = FindAnyObjectByType<UserInterface.Views.TitleView>().Naming;
			Check($"Starting a new slot asks for a name, with a suggestion ready (\"{naming?.Field.text}\")", naming != null && naming.Field.text.Length > 0 && naming.Confirm.interactable);
			Shot("00b_name");

			naming?.Field.SetTextWithoutNotify(" <i>Marigold</i>");
			naming?.Submit();
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

			// The island's ground is one surface, its grounds blended by a control map: nothing layered on top to flicker
			var islandMaterial = FindObjectsByType<Renderer>(FindObjectsSortMode.None).Select(renderer => renderer.sharedMaterial)
				.FirstOrDefault(material => material != null && material.shader.name == "WitchCraft/SplatGround");
			var control = islandMaterial != null && islandMaterial.HasProperty("_Control") ? islandMaterial.GetTexture("_Control") : null;
			Check($"The island's ground blends its textures by a control map ({islandMaterial?.shader.name}, map {(control != null ? $"{control.width}px" : "missing")})",
				islandMaterial != null && islandMaterial.shader.name == "WitchCraft/SplatGround" && control != null && GameObject.Find("GroundPatches") == null);

			// The art swap layer: every slot listed, and assigning a prefab swaps the look but not the collision
			var artSet = Art.ArtSwap.Set;
			var artKeys = artSet != null ? artSet.Entries.Select(entry => entry.Key).ToList() : new List<string>();
			Check($"The Art Set lists every slot ({artKeys.Count})",
				new[] { "witch", "cat", "broom", "piece:bed", "piece:wall_timber", "companion:pip", "companion:nova", "tree:pine", "tree:palm", "rock:boulder", "prop:crate", "gather:Lavender", "gather:GiantClam" }.All(artKeys.Contains));

			var crateEntry = artSet.Find("prop:crate");
			var crateArtBefore = crateEntry.Prefab;
			crateEntry.Prefab = database.FindBuildPiece("lamp").Prefab;
			artSet.NotifyChanged();
			yield return null;

			var crateSwap = FindObjectsByType<Art.ArtSwap>(FindObjectsSortMode.None).First(swap => swap.Key == "prop:crate");
			var crateRenderer = crateSwap.GetComponent<Renderer>();
			var crateArt = crateSwap.transform.Cast<Transform>().FirstOrDefault(child => child.name.StartsWith("Art ("));

			Check("Assigning art shows it and hides the placeholder, keeping the placeholder's collider",
				crateSwap.HasArt && crateArt != null && !crateRenderer.enabled && crateSwap.GetComponent<Collider>().enabled && crateArt.GetComponentsInChildren<Collider>().Length == 0);

			crateEntry.Prefab = null;
			artSet.NotifyChanged();
			yield return null;

			Check("Clearing the art brings the placeholder back", !crateSwap.HasArt && crateRenderer.enabled);

			crateEntry.Prefab = crateArtBefore;
			artSet.NotifyChanged();
			yield return null;

			// The enchanted realm's art, when the Synty packs are on this machine
			var swaps = FindObjectsByType<Art.ArtSwap>(FindObjectsSortMode.None);
			var surfaces = FindObjectsByType<Art.ArtMaterialSwap>(FindObjectsSortMode.None);
			var lanterns = swaps.Where(swap => swap.Key == "scatter:lantern").ToList();

			Check($"Art-only spots and surface slots exist ({lanterns.Count} lanterns, {surfaces.Length} surfaces)", lanterns.Count > 5 && surfaces.Any(surface => surface.Key == "surface:grass"));

			if (lanterns.Any(lantern => lantern.HasArt))
			{
				var dressed = new[] { "tree:pine", "tree:broadleaf", "rock:boulder", "prop:shrine", "prop:bounce-mushroom", "prop:alchemy-bench", "prop:hearth", "piece:wall_timber", "piece:bed", "gather:Lavender" };
				var bare = dressed.Where(key => !swaps.Any(swap => swap.Key == key && swap.HasArt)).ToList();
				Check($"The enchanted realm dresses trees, rocks, shrines, the bench, the hut and herbs (bare: {string.Join(", ", bare)})", bare.Count == 0);
				Check("The meadow is painted with the realm's grass", surfaces.First(surface => surface.Key == "surface:grass").HasArt);
			}
			else
			{
				report.AppendLine("NOTE  No Synty packs on this machine; the island keeps its placeholder looks");
			}

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
			yield return Until(() => caster.ActiveSpell?.Id == "conjure");
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
			yield return Until(() => caster.ActiveSpell?.Id == "levitate");
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

			var cat = FindAnyObjectByType<CatCompanion>();
			Check($"Sable loafs on the broom seat ({cat.Activity}{RigNote(cat)})", cat.Activity == "Ride" && (cat.RigState == null || cat.RigState.Contains("Catloaf")));

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
			Check($"Sable hops down off the broom ({cat.Activity}, {Vector3.Distance(cat.transform.position, player.transform.position):0.0}m from the witch)",
				cat.transform.parent == null && !cat.IsHopping && Vector3.Distance(cat.transform.position, player.transform.position) < 5f);

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

			yield return CatScenario(player, movement);

			// Companions: gift Pip what they ask for and they move in
			var pip = database.FindCompanion("pip");
			inventory.Add(database.FindItem("glowcap"), 3);

			var recruit = FindObjectsByType<CompanionRecruit>(FindObjectsSortMode.None).FirstOrDefault(found => found.name.StartsWith("Pip"));
			movement.Teleport(recruit.transform.position + Vector3.right * 1.5f);

			var hissed = false;

			for (var t = 0f; t < 2f && !hissed; t += Time.deltaTime)
			{
				hissed = cat.Activity == "ThreatLong";
				yield return null;
			}

			Check($"Sable hisses at Pip, whom she hasn't met ({cat.Activity}{RigNote(cat)})", hissed);
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
			yield return Until(() => caster.ActiveSpell?.Id == "bubble");
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
			yield return Until(() => caster.ActiveSpell?.Id == "blink");
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
			yield return Until(() => caster.ActiveSpell?.Id == "rain_call");
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
			yield return View("08b_alchemy_bench", new Vector3(-6.5f, 2.6f, -9.5f), new Vector3(-6.5f, 1f, -5.5f));
			yield return View("08c_hearth", new Vector3(9f, 3.5f, -9f), new Vector3(5.5f, 0.5f, -5f));
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

			// At night by home, Sable takes herself off to her Cat Bed
			for (var t = 0f; t < 14f && !cat.IsInBed; t += Time.deltaTime)
				yield return null;

			yield return new WaitForSeconds(4f);
			Check($"At night near home Sable sleeps in her Cat Bed ({cat.Activity}{RigNote(cat)})", cat.IsInBed && cat.IsAsleep);
			yield return FrameCat(cat, 1.5f, true);
			Shot("26_cat_bed");
			EndFrame();

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

			// The hut's roof keeps the rain out: no drops below the roof inside its footprint
			var roofs = FindObjectsByType<PlacedPiece>(FindObjectsSortMode.None).Where(piece => piece.Definition.Snap == BuildSnap.Roof).ToList();
			var roofArea = roofs.Select(piece => piece.GetComponentInChildren<Collider>().bounds).Aggregate((all, next) => { all.Encapsulate(next); return all; });
			var drops = new ParticleSystem.Particle[3000];
			var rainSystem = GameObject.Find("Rain").GetComponent<ParticleSystem>();
			var sheltered = 0;
			var near = 0;

			for (var sample = 0; sample < 20; sample++)
			{
				var count = rainSystem.GetParticles(drops);

				for (var i = 0; i < count; i++)
				{
					var drop = drops[i].position;
					var inside = drop.x > roofArea.min.x + 0.4f && drop.x < roofArea.max.x - 0.4f && drop.z > roofArea.min.z + 0.4f && drop.z < roofArea.max.z - 0.4f;

					if (inside && drop.y < roofArea.min.y - 0.2f && drop.y > roofArea.min.y - 3f)
						sheltered++;
					else if (!inside && drop.y < 2.5f && Vector3.Distance(drop, roofArea.center) < 12f)
						near++;
				}

				yield return new WaitForSeconds(0.1f);
			}

			Check($"Under the hut's roof it stays dry ({sheltered} drops inside, {near} falling nearby)", roofs.Count > 0 && sheltered == 0 && near > 0);
			yield return View("16b_roof", new Vector3(-6f, 4.5f, -6f), new Vector3(0f, 2.8f, 2f));
			yield return View("16_rain", new Vector3(9f, 6f, -14f), new Vector3(0f, 1.5f, 0f));

			weather.SetWeather(WeatherKind.Clear);

			yield return Storybooks(inputMode, player, movement);

			yield return WitchName(inputMode, player);

			yield return WitchArt(player);

			yield return GatherPrompt(player);

			report.AppendLine($"Inventory: {string.Join(", ", inventory.Items.Select(stack => stack.ToString()))}");
		}

		// The interact prompt names what E will do: a herb in reach beats Sable at the witch's feet, and once it's
		// gathered the prompt changes straight away
		private IEnumerator GatherPrompt(PlayerCharacter player)
		{
			var hud = FindAnyObjectByType<UserInterface.Views.HudView>();
			var movement = player.GetComponent<IMovementController>();
			var cat = FindAnyObjectByType<CatCompanion>();
			var respawn = typeof(Gatherable).GetField("respawnSeconds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

			var herb = FindObjectsByType<Gatherable>(FindObjectsSortMode.None)
				.Where(gatherable => gatherable.CanInteract && !gatherable.Prompt.Contains("opens at night") && (float)respawn.GetValue(gatherable) > 0f)
				.OrderBy(gatherable => Vector3.Distance(gatherable.transform.position, player.transform.position))
				.FirstOrDefault();

			if (herb == null)
			{
				report.AppendLine("NOTE  No gatherable to test the prompt with");
				yield break;
			}

			movement.Teleport(herb.transform.position - player.transform.forward * 1.1f + Vector3.up * 0.2f);
			yield return new WaitForSeconds(0.3f);

			// Sable right where the witch is looking, closer than the herb
			cat.transform.position = player.transform.position + player.transform.forward * 0.9f;
			yield return new WaitForFixedUpdate();
			yield return null;

			var before = hud.PromptText;
			Check($"With a herb in reach, the prompt offers it, not Sable (\"{before}\")", ReferenceEquals(hud.Focus, herb) && before.Contains(herb.Prompt));

			Tap(Key.E);
			yield return new WaitForSeconds(0.3f);

			// A neighbouring herb of the same kind may take its place; this one must be gone from the prompt
			var after = hud.PromptText;
			Check($"Gathering it updates the prompt straight away (now \"{after}\")", !herb.CanInteract && !ReferenceEquals(hud.Focus, herb));
		}

		// The Stellar Witch art (on machines with the packs): locomotion from Synty, and the Human Spellcasting
		// clips on the upper body while a spell is readied and for each cast
		private IEnumerator WitchArt(PlayerCharacter player)
		{
			var swap = player.GetComponentsInChildren<Art.ArtSwap>().FirstOrDefault(art => art.Key == "witch");
			var rig = swap != null ? swap.ArtAnimator : null;

			if (rig == null || rig.runtimeAnimatorController == null || rig.parameters.All(parameter => parameter.name != "CastStyle"))
			{
				report.AppendLine("NOTE  The witch has no rigged art on this machine; animation checks skipped");
				yield break;
			}

			var movement = player.GetComponent<IMovementController>();
			var caster = player.GetComponent<ISpellCaster>();

			Check($"The witch's art is a humanoid rig with the casting layer ({rig.layerCount} layers)", rig.isHuman && rig.layerCount == 2);

			var head = rig.GetBoneTransform(HumanBodyBones.Head);
			var headHeight = head.position.y - player.transform.position.y;
			Check($"She is witch-sized (head {headHeight:0.00}m up)", headHeight > 1.25f && headHeight < 1.6f);

			var untextured = swap.Art.GetComponentsInChildren<Renderer>().SelectMany(renderer => renderer.sharedMaterials)
				.Where(material => material != null && (!material.HasProperty("_BaseMap") || material.GetTexture("_BaseMap") == null))
				.Select(material => material.name).Distinct().ToList();
			Check($"Her materials keep their textures ({string.Join(", ", untextured)})", untextured.Count == 0);

			movement.Teleport(new Vector3(-3f, 0.2f, -15f));

			// Spells put away: standing idle
			if (caster.ActiveSpell != null)
				caster.SelectSlot(caster.ActiveSlot);

			yield return new WaitForSeconds(1.5f);
			Check("Standing still she idles", rig.GetCurrentAnimatorStateInfo(0).IsName("Locomotion") && rig.GetCurrentAnimatorStateInfo(1).IsName("Empty"));
			yield return FrameWitch(player, 3.2f);
			Shot("27_witch_idle");

			caster.SelectSlot(0);
			yield return new WaitForSeconds(1.2f);
			var ready = rig.GetCurrentAnimatorStateInfo(1);
			Check($"Readying a spell raises her casting hand ({caster.ActiveSpell?.Id})", ready.IsName("Ready") || ready.IsName("ReadyIdle"));

			// Bubble is called down from the sky; everything else she points or pushes
			caster.Cast();
			yield return new WaitForSeconds(0.25f);
			var cast = rig.GetCurrentAnimatorStateInfo(1);
			var expected = $"Cast{(int)CharacterAnimator.CastStyleFor(caster.ActiveSpell.Effect)}";
			Check($"Casting plays a cast ({expected})", cast.IsName(expected));
			Shot("28_witch_cast");
			EndFrame();

			caster.SelectSlot(caster.ActiveSlot);

			// Her hair and cloth sway (UnityChanSpringBone, when installed): a hair bone swings as she sets off
			var springBones = swap.Art.GetComponentsInChildren<MonoBehaviour>().Where(component => component != null && component.GetType().Name == "SpringBone").ToList();
			var hairBone = springBones.Select(bone => bone.transform).FirstOrDefault(bone => bone.name.ToLowerInvariant().Contains("hair")) ?? springBones.FirstOrDefault()?.transform;
			var hairRest = hairBone != null ? hairBone.localRotation : Quaternion.identity;
			var hairSwing = 0f;

			// Footsteps come from her feet landing, not a timer
			var animator = player.GetComponentInChildren<CharacterAnimator>();
			var steps = new List<float>();
			void Stepped() => steps.Add(Time.time);
			animator.FootPlanted += Stepped;

			Hold(Key.W);

			for (var t = 0f; t < 1.2f; t += Time.deltaTime)
			{
				if (hairBone != null)
					hairSwing = Mathf.Max(hairSwing, Quaternion.Angle(hairRest, hairBone.localRotation));

				yield return null;
			}

			yield return new WaitForSeconds(0.8f);
			animator.FootPlanted -= Stepped;

			// Running at 5.5 m/s is about three steps a second; ignore the first half second of setting off
			var cadence = steps.Count(time => time > steps.FirstOrDefault() + 0.5f) / Mathf.Max(0.01f, (steps.LastOrDefault() - steps.FirstOrDefault() - 0.5f));
			Check($"Her footsteps fall as her feet land ({steps.Count} steps, {cadence:0.0} a second; timed steps off: {ServiceLocator.Get<IAudioService>().FootstepsFromAnimation})",
				steps.Count >= 4 && cadence > 1.5f && cadence < 6f && ServiceLocator.Get<IAudioService>().FootstepsFromAnimation);

			if (springBones.Count > 0)
				Check($"Her hair sways as she runs ({springBones.Count} spring bones; {hairBone.name} swung {hairSwing:0.0}°)", hairSwing > 2f);
			else
				report.AppendLine("NOTE  UnityChanSpringBone isn't installed; her hair doesn't sway");

			var running = rig.GetCurrentAnimatorStateInfo(0);
			Check($"Running uses the locomotion blend (speed {rig.GetFloat("Velocity"):0.0})", running.IsName("Locomotion") && rig.GetFloat("Velocity") > 4f);
			yield return FrameWitch(player, 3.5f);
			Shot("29_witch_run");
			Release();
			EndFrame();

			movement.Teleport(new Vector3(-3f, 0.2f, -15f));
			yield return new WaitForSeconds(0.5f);

			// On the broom she sits on the stick, whatever pose the clip has
			var broom = player.GetComponentsInChildren<Transform>(true).First(part => part.name == "Broom");
			var hips = rig.GetBoneTransform(HumanBodyBones.Hips);

			Tap(Key.F);
			yield return Until(() => player.GetComponent<IBroomController>().IsMounted);
			yield return new WaitForSeconds(1.5f);

			var seat = hips.position.y - broom.position.y;
			Check($"On the broom her hips sit on the stick ({seat:0.00}m above it; riding {rig.GetCurrentAnimatorClipInfo(0).FirstOrDefault().clip?.name})", Mathf.Abs(seat - 0.1f) < 0.15f);
			yield return FrameWitch(player, 3f);
			Shot("30_witch_broom");
			EndFrame();

			Tap(Key.F);
			yield return Until(() => !player.GetComponent<IBroomController>().IsMounted);
			yield return new WaitForSeconds(1f);
		}

		private IEnumerator FrameWitch(PlayerCharacter player, float distance)
		{
			var target = player.transform.position + Vector3.up * 1f;
			var offset = (player.transform.forward + player.transform.right * 0.6f).normalized * distance + Vector3.up * 0.6f;

			EndFrame();

			frame = new GameObject("View_Witch").transform;
			frame.position = target + offset;
			frame.LookAt(target);

			ServiceLocator.Get<IEventBusSender>().SetCameraOverride(frame);

			yield return new WaitForSeconds(0.3f);
		}

		// The name given on the title screen is saved, shown on the HUD, filled into dialogue, and can be changed at the hearth
		private IEnumerator WitchName(GameState.Interfaces.IInputModeService inputMode, PlayerCharacter player)
		{
			var witch = ServiceLocator.Get<Identity.Interfaces.IWitchProfile>();
			Check($"The witch is called what she was named, tidied (\"{witch.Name}\")", witch.Name == "Marigold");
			Check("Dialogue fills in her name", witch.Personalise("\"Morning, {name}.\"") == "\"Morning, Marigold.\"");

			var hud = FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None).Any(text => text.isActiveAndEnabled && text.text.Contains("Marigold's home"));
			Check("The HUD shows whose home it is", hud);

			var saves = ServiceLocator.Get<Persistence.Interfaces.ISaveService>();
			saves.Save();
			Check($"The slot remembers her name (\"{saves.Describe(saves.CurrentSlot).WitchName}\")", saves.Describe(saves.CurrentSlot).WitchName == "Marigold");

			FindAnyObjectByType<Hearth>().Interact(player.gameObject);
			yield return new WaitForSeconds(0.3f);

			var rename = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
				.FirstOrDefault(button => button.isActiveAndEnabled && button.GetComponentInChildren<UnityEngine.UI.Text>().text == "Change my name");
			Check("The hearth offers to change your name", rename != null);
			rename?.onClick.Invoke();
			yield return new WaitForSeconds(0.3f);

			var nameView = FindAnyObjectByType<UserInterface.Views.NameView>();
			Check($"Changing your name starts from the current one (\"{nameView.Form?.Field.text}\")", nameView.IsOpen && nameView.Form.Field.text == "Marigold");
			Shot("20_rename");

			nameView.Form.Field.SetTextWithoutNotify("Hazel");
			nameView.Form.Submit();
			yield return new WaitForSeconds(0.3f);
			Check("Renaming takes effect and returns to play", witch.Name == "Hazel" && !nameView.IsOpen && inputMode.Mode == InputMode.Gameplay);
		}

		// Find a storybook, read it, then read it again from the bookcase at home
		private IEnumerator Storybooks(GameState.Interfaces.IInputModeService inputMode, PlayerCharacter player, IMovementController movement)
		{
			var books = ServiceLocator.Get<Books.Interfaces.IBookCollection>();
			var pickups = FindObjectsByType<StoryBookPickup>(FindObjectsSortMode.None);

			var floating = pickups.Where(pickup => !Physics.Raycast(pickup.transform.position + Vector3.up * 0.5f, Vector3.down, 1.5f, ~0, QueryTriggerInteraction.Ignore)).Select(pickup => pickup.name).ToList();
			Check($"Six storybooks stand on solid ground around the world ({pickups.Length}; floating: {string.Join(", ", floating)})", pickups.Length == 6 && books.AllBooks.Count == 6 && floating.Count == 0);

			var shrines = FindObjectsByType<GlyphShrine>(FindObjectsSortMode.None);
			var besideShrines = pickups.Where(pickup => shrines.Any(shrine => Vector3.Distance(shrine.transform.position, pickup.transform.position) < 12f)).Select(pickup => pickup.name).ToList();
			Check($"No storybook sits beside a glyph shrine ({string.Join(", ", besideShrines)})", besideShrines.Count == 0);

			var bookcases = FindObjectsByType<Bookcase>(FindObjectsSortMode.None);
			Check($"The home has a bookcase ({bookcases.Length})", bookcases.Length >= 1);
			Check($"Before any are found, the bookcase holds no storybooks ({bookcases[0].ShownCount})", bookcases[0].ShownCount == 0);

			var database = ServiceLocator.Get<GameDatabase>();
			Check("The bookshelf can be built from home level 1", database.FindBuildPiece("bookshelf").RequiredHomeLevel == 1);

			// The starter hut is flagged, and unmaking it gives back half its cost
			var home = ServiceLocator.Get<IHomeService>();
			var placed = FindObjectsByType<PlacedPiece>(FindObjectsSortMode.None);
			var starterWall = placed.FirstOrDefault(piece => piece.Record.Starter && piece.Definition.Cost.Any(stack => stack.Amount >= 2));
			var starterCost = starterWall != null ? starterWall.Definition.Cost.Sum(stack => stack.Amount) : 0;
			var starterRefund = starterWall != null ? home.Refund(starterWall).Sum(stack => stack.Amount) : 0;
			Check($"Starter pieces are flagged ({placed.Count(piece => piece.Record.Starter)} of {placed.Length}) and Unmake returns half ({starterRefund} of {starterCost})",
				starterWall != null && bookcases.All(bookcase => bookcase.GetComponent<PlacedPiece>().Record.Starter) && starterRefund == starterWall.Definition.Cost.Sum(stack => stack.Amount / 2));

			var bubbleWitch = pickups.First(pickup => pickup.name.EndsWith("bubble_witch"));
			movement.Teleport(bubbleWitch.transform.position + Vector3.up * 0.2f);
			yield return View("17_storybook", bubbleWitch.transform.position + bubbleWitch.transform.forward * 3f + Vector3.up * 1.6f, bubbleWitch.transform.position + Vector3.up * 1f);

			bubbleWitch.Interact(player.gameObject);
			yield return new WaitForSeconds(0.3f);

			var reader = FindAnyObjectByType<UserInterface.Views.BookView>();
			Check("Picking up a storybook opens it to read", reader.IsOpen && reader.Book != null && reader.Book.Id == "bubble_witch" && books.Count == 1);
			Check("The book is taken from where it floated", !bubbleWitch.CanInteract && bubbleWitch.transform.Find("Book") is { gameObject: { activeSelf: false } });

			// And it now stands on the bookcase at home, in its own slot
			var bubbleIndex = books.AllBooks.ToList().FindIndex(storyBook => storyBook.Id == "bubble_witch");
			Check($"The book appears on the bookcase at home ({bookcases[0].ShownCount} shown)", bookcases[0].ShownCount == 1 && bookcases[0].Shows(bubbleIndex));

			Tap(Key.D);
			yield return new WaitForSeconds(0.3f);
			Check("D turns the page", reader.Spread == 1);
			Shot("18_book_pages");

			// Every page of every book fits on the page, and each page is numbered at its foot
			var overflowing = new List<string>();
			var misnumbered = new List<string>();

			foreach (var storyBook in books.AllBooks)
			{
				reader.Show(storyBook);

				for (var spread = 0; spread < reader.SpreadCount; spread++)
				{
					Canvas.ForceUpdateCanvases();

					if (reader.Overflowing)
						overflowing.Add($"{storyBook.Id} spread {spread + 1}");

					var (leftPage, rightPage) = reader.PageNumbers;
					var expectedRight = spread * 2 + 2 <= storyBook.Pages.Count ? $"{spread * 2 + 2}" : "";

					if (leftPage != $"{spread * 2 + 1}" || rightPage != expectedRight)
						misnumbered.Add($"{storyBook.Id} spread {spread + 1}: {leftPage}|{rightPage}");

					reader.Turn(1);
				}
			}

			Check($"Every storybook page fits ({books.AllBooks.Sum(storyBook => storyBook.Pages.Count)} pages; too long: {string.Join(", ", overflowing)})", overflowing.Count == 0);
			Check($"Every page carries its own number ({string.Join(", ", misnumbered)})", misnumbered.Count == 0);

			// A book with an odd number of pages ends on a left page
			var odd = books.AllBooks.FirstOrDefault(storyBook => storyBook.Pages.Count % 2 == 1);

			if (odd != null)
			{
				reader.Show(odd);
				reader.Turn(reader.SpreadCount);
				yield return new WaitForSeconds(0.3f);
				Shot("18c_book_last_page");
			}

			reader.Show(books.AllBooks.First(storyBook => storyBook.Id == "phoenix_claw"));
			yield return new WaitForSeconds(0.3f);
			Shot("18b_book_title");

			Tap(Key.Escape);
			yield return new WaitForSeconds(0.3f);
			Check("Escape closes the book", !reader.IsOpen && inputMode.Mode == InputMode.Gameplay);

			var shelf3D = bookcases[0].transform;
			yield return View("19a_bookcase_books", shelf3D.position + shelf3D.forward * 2.2f + Vector3.up * 1.9f, shelf3D.position + Vector3.up * 1.7f);

			bookcases[0].Interact(player.gameObject);
			yield return new WaitForSeconds(0.3f);

			var shelf = FindAnyObjectByType<UserInterface.Views.BookcaseView>();
			Check("The bookcase lists the storybooks", shelf.IsOpen);
			Shot("19_bookcase");

			var read = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None)
				.Where(button => button.isActiveAndEnabled && button.GetComponentInChildren<UnityEngine.UI.Text>().text == "Read").ToList();
			Check($"Only found books can be read again ({read.Count})", read.Count == 1);

			read.FirstOrDefault()?.onClick.Invoke();
			yield return new WaitForSeconds(0.3f);
			Check("Read opens the book from the bookcase, at the start", reader.IsOpen && reader.Spread == 0 && !shelf.IsOpen);

			Tap(Key.Escape);
			yield return new WaitForSeconds(0.3f);
			Check("Closing it goes back to the bookcase", shelf.IsOpen && !reader.IsOpen);

			Tap(Key.Escape);
			yield return new WaitForSeconds(0.3f);
			Check("Escape again returns to play", inputMode.Mode == InputMode.Gameplay);
		}

		// Sable: the art's scale, facing and materials, then sitting, grooming, pets, sleep and hopping up and down a ledge
		private IEnumerator CatScenario(PlayerCharacter player, IMovementController movement)
		{
			var cat = FindAnyObjectByType<CatCompanion>();
			var swap = cat.GetComponent<Art.ArtSwap>();
			var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

			if (swap.HasArt)
			{
				var profile = swap.Art.GetComponentInChildren<Art.CatRigProfile>();
				var roles = (Art.CatRigProfile.Role[])Enum.GetValues(typeof(Art.CatRigProfile.Role));
				Check($"Sable's art has a rig profile covering every behaviour ({profile?.States.Count} states)", profile != null && roles.All(profile.Has));

				var bones = swap.Art.GetComponentsInChildren<Transform>(true);
				var head = bones.FirstOrDefault(bone => bone.name == "head");
				var tailBase = bones.FirstOrDefault(bone => bone.name == "tail01");

				if (head != null && tailBase != null && profile != null)
				{
					// Standing head height in the world, from the measured idle pose and the art's scale
					var headHeight = profile.HeadHeight * swap.Art.transform.lossyScale.y;
					var facing = Vector3.Dot(Flat(head.position - tailBase.position).normalized, Flat(cat.transform.forward).normalized);
					Check($"Sable's art is cat-sized (head {headHeight:0.00}m up) and faces forward ({facing:0.00})", headHeight > 0.25f && headHeight < 0.4f && facing > 0.7f);
				}

				var shaders = swap.Art.GetComponentsInChildren<Renderer>().SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null).Select(material => material.shader.name).Distinct().ToList();
				Check($"Sable's materials render in URP ({string.Join(", ", shaders)})", shaders.Count > 0 && shaders.All(shader => shader.StartsWith("Universal Render Pipeline")));
			}
			else
			{
				report.AppendLine("NOTE  Sable has no art on this machine; behaviour checks use the placeholder");
			}

			// Somewhere open, away from home and the creatures
			movement.Teleport(new Vector3(-3f, 0.2f, -15f));

			// Following at the witch's pace she walks (trots), rather than gliding in a held leap
			yield return new WaitForSeconds(3f);

			var gaits = new HashSet<string>();

			// At the witch's ordinary pace: no Swiftness Brew left over from the brewing checks
			ServiceLocator.Get<IPotionEffects>()?.Clear();

			Hold(Key.W);

			for (var t = 0f; t < 2.5f; t += Time.deltaTime)
			{
				// Once she's up and alongside: catching up from further back still bounds, as it should
				if (t > 1.2f && cat.Activity == "Walk" && cat.RigState != null && Vector3.Distance(cat.transform.position, player.transform.position) < 4.5f)
					gaits.Add(cat.RigState);

				yield return null;
			}

			Release();

			// Back where the rest of the cat checks expect her
			movement.Teleport(new Vector3(-3f, 0.2f, -15f));

			if (cat.RigState != null)
				Check($"Following at the witch's pace, Sable walks ({string.Join(", ", gaits)})", gaits.Count > 0 && gaits.All(gait => gait.Contains("Walk")));

			// She may have a long way to come; give her time to arrive and settle
			for (var t = 0f; t < 10f && (cat.Activity is not ("Sit" or "Groom" or "Scratch") || Vector3.Distance(cat.transform.position, player.transform.position) > 4f); t += Time.deltaTime)
				yield return null;

			Check($"When the witch stops, Sable sits ({cat.Activity}{RigNote(cat)}, {Vector3.Distance(cat.transform.position, player.transform.position):0.0}m from the witch)", cat.Activity is "Sit" or "Groom" or "Scratch" && Vector3.Distance(cat.transform.position, player.transform.position) < 4f);
			yield return FrameCat(cat, 1.4f);
			Shot("21_cat_sit");

			cat.GetType().GetMethod("Act", flags).Invoke(cat, new object[] { Art.CatRigProfile.Role.Groom, 3f });
			yield return new WaitForSeconds(1f);
			Check($"Sitting, she grooms ({cat.Activity}{RigNote(cat)})", cat.Activity == "Groom" && (cat.RigState == null || cat.RigState.Contains("Grooming")));
			Shot("22_cat_groom");
			yield return new WaitForSeconds(2.5f);

			cat.Interact(player.gameObject);
			yield return new WaitForSeconds(1.5f);
			Check($"A pet gets a roll over ({cat.Activity}{RigNote(cat)})", cat.Activity == "RollOver" && (cat.RigState == null || cat.RigState.Contains("Rollingover")));
			Shot("24_cat_roll");

			cat.Interact(player.gameObject);
			cat.Interact(player.gameObject);
			cat.Interact(player.gameObject);
			yield return new WaitForSeconds(0.5f);
			Check($"Too many pets and she's had enough ({cat.Activity}{RigNote(cat)})", cat.Activity == "ThreatShort");
			yield return new WaitForSeconds(3f);

			// Skip ahead through the loaf and lying down to sleep
			cat.GetType().GetField("idleTime", flags).SetValue(cat, 60f);
			yield return new WaitForSeconds(2f);
			Check($"Left long enough, Sable falls asleep ({cat.Activity}{RigNote(cat)})", cat.IsAsleep && (cat.RigState == null || cat.RigState.Contains("Sleep")));
			Shot("23_cat_sleep");

			cat.Interact(player.gameObject);
			yield return new WaitForSeconds(0.6f);
			Check($"A pet while she sleeps gets a sleepy blink and she sleeps on ({cat.Activity}{RigNote(cat)})", cat.Activity == "SleepBlink" && cat.IsAsleep);
			yield return new WaitForSeconds(2.5f);

			EndFrame();

			// A ledge: the witch climbs onto a block and Sable hops up after her, then down again
			var groundY = Physics.Raycast(new Vector3(-3f, 20f, -21f), Vector3.down, out var groundHit, 40f, LayerMask.GetMask("Ground"), QueryTriggerInteraction.Ignore) ? groundHit.point.y : 0f;
			var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
			block.name = "PlaytestLedge";
			block.layer = LayerMask.NameToLayer("Ground");
			block.transform.position = new Vector3(-3f, groundY + 0.6f, -21f);
			block.transform.localScale = new Vector3(5f, 1.2f, 5f);
			Physics.SyncTransforms();

			// The camera settles on the ledge first, as the hop itself is over in a moment
			yield return FrameSpot(new Vector3(4f, groundY + 2.2f, -17f), new Vector3(-3f, groundY + 1f, -19f));

			movement.Teleport(new Vector3(-3f, groundY + 1.4f, -21f));

			var hopped = false;

			for (var t = 0f; t < 4f && !hopped; t += Time.deltaTime)
			{
				hopped = cat.IsHopping;
				yield return null;
			}

			yield return new WaitForSeconds(0.2f);
			Shot("25_cat_hop");
			EndFrame();
			yield return new WaitForSeconds(1.5f);

			Check($"Sable hops up the ledge after the witch (height {cat.transform.position.y - groundY:0.0}m)", hopped && cat.transform.position.y - groundY > 1f);

			movement.Teleport(new Vector3(-3f, groundY + 0.2f, -15f));

			var hoppedDown = false;

			for (var t = 0f; t < 4f && !hoppedDown; t += Time.deltaTime)
			{
				hoppedDown = cat.IsHopping;
				yield return null;
			}

			yield return new WaitForSeconds(1.5f);
			Check($"...and hops back down (height {cat.transform.position.y - groundY:0.0}m)", hoppedDown && cat.transform.position.y - groundY < 0.5f);

			Destroy(block);
		}

		private static string RigNote(CatCompanion cat) => cat.RigState != null ? $", {cat.RigState}" : "";

		private Transform frame;

		// Points the camera at the cat from beside and in front, and waits for it to arrive
		private IEnumerator FrameCat(CatCompanion cat, float distance, bool fromAbove = false)
		{
			var target = cat.transform.position + Vector3.up * 0.2f;

			// From the side away from the witch, so she isn't in the way
			var away = Flat(cat.transform.position - FindAnyObjectByType<PlayerCharacter>().transform.position);
			away = away.sqrMagnitude > 0.01f ? away.normalized : cat.transform.forward;

			var offset = fromAbove
				? (cat.transform.forward * 0.5f + Vector3.up).normalized * distance
				: (away + cat.transform.forward * 0.7f).normalized * distance + Vector3.up * distance * 0.35f;

			yield return FrameSpot(target + offset, target);
		}

		private IEnumerator FrameSpot(Vector3 position, Vector3 lookAt)
		{
			EndFrame();

			frame = new GameObject("View_Cat").transform;
			frame.position = position;
			frame.LookAt(lookAt);

			ServiceLocator.Get<IEventBusSender>().SetCameraOverride(frame);

			yield return new WaitForSeconds(1.2f);
		}

		private void EndFrame()
		{
			if (frame == null)
				return;

			ServiceLocator.Get<IEventBusSender>().SetCameraOverride(null);
			Destroy(frame.gameObject);
			frame = null;
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

		// Held for a tenth of a second, like a quick real press, and across at least two physics steps: a tap of a few
		// frames could fall inside one slow frame and be gone before the game read it
		// Waits for something the game does in response to input, up to a real-time limit. A fixed wait in game time can
		// run out inside one slow frame, before the physics step that acts on a key press has happened
		private static IEnumerator Until(Func<bool> done, float seconds = 1.5f)
		{
			var giveUp = Time.realtimeSinceStartup + seconds;

			while (!done() && Time.realtimeSinceStartup < giveUp)
				yield return null;
		}

		private IEnumerator TapRoutine(Key key)
		{
			Hold(key);
			yield return new WaitForSecondsRealtime(0.1f);
			yield return new WaitForFixedUpdate();
			yield return new WaitForFixedUpdate();
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
