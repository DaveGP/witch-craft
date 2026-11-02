# Technical Design

## Startup

`DependencyInjection/Bootstrapper` runs before the first scene loads and registers every service with the `ServiceLocator`:

| Service | Interface | Notes |
| ------- | --------- | ----- |
| `EventBus` | `IEventBus`, `IEventBusSender` | Events for UI, camera and gameplay |
| `SystemManager` | - | Ticks `IUpdateSystem`/`IFixedUpdateSystem`/`ILateUpdateSystem`. The scene's `SystemsRunner` drives it |
| `InputModeService` | `IInputModeService` | Gameplay, Menu or Station. Locks and frees the cursor |
| `InputProvider` | `ICharacterInputSource`, `ICameraInputSource`, `IMenuInputSource` | Buffers input; suppresses character input outside Gameplay |
| `PlayerContext`, `ActiveTargetController` | `IPlayerContext`, `IActiveTargetController` | The active `IObserverTarget` |
| `GameDatabase` | - | Loaded from `Resources/GameDatabase` |
| `SaveService` | `ISaveService` | JSON in `persistentDataPath`, autosaves every two minutes and on quit |
| `Inventory` | `IInventory` | |
| `RecipeBook` | `IRecipeBook` | |
| `SpellBook` | `ISpellBook` | |
| `CompanionRoster` | `ICompanionRoster` | |
| `PotionEffects` | `IPotionEffects` | |

Scene objects that are services register themselves: `CameraController` (`ICameraController`) and `HomeService` (`IHomeService`).

Anything with state implements `ISaveParticipant`; registering reads the current save into it, and `Save()` asks each one to write.

## Character

The structure mirrors Crewfall.

```text
UnityInputReader ──> InputProvider ──(FixedUpdate)──> PlayerCharacter ──> CharacterMotor
                                                                            ├─> MovementController (on foot)
                                                                            ├─> BroomController   (flying)
                                                                            └─> ActionController ──> SpellCaster ──> spell effects
```

* `PlayerCharacter` replaces Crewfall's `CharacterNetworkController`: it consumes input each physics step, passes it to the motor and exposes `ICharacterMovementState` for animation, UI and the cat.
* `CharacterMotor` turns input into camera relative movement, decides gait, toggles the broom and routes interact, spell slot, cast and adjust actions.
* `MovementController` is a Rigidbody controller with its own gravity: acceleration, coyote time, buffered and variable jumps, air jumps, launches, and moving platform carry. Potions modify it through `IPotionEffects`.
* `BroomController` suspends the movement controller while flying and owns the Rigidbody: charge, boost, banking, hover ceiling and updraft uplift.
* `ActionController` finds the nearest `IInteractable` on the Interactable layer in front of the witch and announces it for the HUD prompt.
* `CharacterAnimator` is procedural (squash, bob, lean) until a rigged model arrives.
* `ObserverTarget` exposes the camera target, movement, broom, broom seat and spell state to observers.

## Camera

`CameraController` is an orbit rig (root → yaw pivot → pitch pivot → camera) following the active target, with a sphere cast boom that pulls in against walls and hides the witch when squeezed. It blends to a fixed viewpoint when an override is set (the alchemy bench).

## Systems

* **Interaction**: `IInteractable` with `Gatherable` (respawning or infinite, interact or touch), `GlyphShrine`, `RecipeScroll`, `AlchemyStation`, `Hearth`, `CompanionRecruit`, `CompanionResident`, `CatCompanion`, `StoryBookPickup` and `Bookcase`.
* **Alchemy**: `AlchemySession` holds the brewing rules with no Unity dependencies. `AlchemyStation` connects it to the satchel and drives the cauldron, fire and mortar visuals. `AlchemyView` is the bench UI.
* **Spells**: `SpellBook` stores glyphs, spells and slots and resolves inscriptions. `SpellCaster` readies the slotted spell's `ISpellEffect` component. Effects derive from `SpellEffect` (aiming, services) and live on the player: `ConjureSpell`, `LevitateSpell`, `UnmakeSpell`, `GustSpell`, `BloomSpell`, `WispSpell`.
* **Home building tools**: `ConjureSpell` implements `IBuildPalette` (selected piece, colour, paintbrush, undo history) for `CatalogueView`. `Paintable` tints a piece's main surfaces with property blocks from `HomePalette`; the colour is saved on the `PieceRecord`. `HomeService.Comfort` sums `BuildPieceDefinition.Comfort`; `CompanionSpawner` gives the morning bonus on `IDayCycle.NewDay`.
* **Home**: `HomePlot` snaps pieces to the grid (floors and roofs to cells, walls to edges, furniture free). `HomeService` places, removes, moves and saves pieces, seeds the starter hut and handles expansion. `PlacedPiece` ties a scene object to its save record; `Movable` lets Levitate lift it.
* **Storybooks**: `StoryBookDefinition` holds a book's title, author, colour, hint and pages; `StoryBookContent` (editor) writes the six stories. `BookCollection` (`IBookCollection`) records which are found and saves their ids. `StoryBookPickup` collects a book and raises `BookOpened`; `Bookcase` sits on the bookshelf prefab and raises `BookcaseOpened`. `BookView` reads a book two pages at a time; `BookcaseView` lists them, and a book opened from it returns to it on close. `HomeService` gives existing saves a bookshelf once (`HomeRecord.BookcaseGiven`).
* **Companions**: `CompanionRoster` holds friendship and grants rewards. `CompanionSpawner` creates residents at home.
* **World**: `MovingPlatform`, `BouncePad`, `Updraft`, `HiddenPath`, `BloomSpot`, `Campfire`, `Bobbing`.
* **Settings**: `GameSettings` (`IGameSettings`) keeps look sensitivity, invert Y and volumes in PlayerPrefs, per machine rather than per slot.
* **Witch**: `WitchProfile` (`IWitchProfile`) holds the witch's name, saved as `SaveData.WitchName`. The title screen names a new slot before it loads (`SetPendingName`, taken by the slot as it reads its save) and the hearth renames through `RenameRequested` and `NameView`; both use `NameForm`. `Personalise` fills `{name}` in dialogue bodies and HUD messages, so content lines can use the token.
* **Saves and flow**: `SaveService` has three slots (slot one keeps the original file name). Saving is off while the title screen is up. `GameFlow` reloads the scene to switch slot or return to the title.
* **Input devices**: `UnityInputReader` also implements `IControlScheme`, tracking whether the last input was a gamepad; views focus their first button for gamepad navigation and the HUD swaps its prompts.
* **Web**: `Assets/WebGLTemplates/WitchCraft` is the page around the web build: a themed loading screen, full-window canvas and `autoSyncPersistentDataPath` so browser saves persist.
* **Sky**: `DayCycle` (`IDayCycle`) moves the sun and moon and drives ambient light, fog, sky exposure, reflections and fireflies; time is saved. `WeatherSystem` (`IWeather`) rolls weather and drives rain particles and sound; the drops collide with the Ground and Environment layers and die where they hit, so roofs keep the rain out.
* **Art**: `ArtSet` (Resources) maps slot keys to optional prefabs. `ArtSwap` on each placeholder shows the slot's prefab (in play and, unsaved, in the editor), hides the placeholder renderers and strips the art's colliders; a missing prefab keeps the placeholder. The builders add slots with `WitchCraftBuildKit.ArtSlot` and `SyncArtSet` lists them. `CharacterAnimator` and `CatCompanion` drive an art Animator's parameters when present (`AnimatorParameters` skips missing ones). For the witch that's Velocity, IsGrounded, IsFlying and Jump, plus SpellReady while a spell is readied and a Cast trigger with a `CastStyle` (Point, Push, Call, Sweep, chosen per spell) from `ISpellCasterState.SpellCast`. Local-only setup tools under `Editor/Art` turn packs into art: `CasualCatSetup` (Sable), `EnchantedRealmSetup` (the island, ground surfaces and scenery kits) and `StellarWitchSetup` (the witch: a two-layer Animator Controller with Synty Base Locomotion on the base layer and the Human Spellcasting clips on an upper-body layer, her pack's toon materials moved onto URP Simple Lit in place, her wand in her hand; her hair and cloth sway with UnityChanSpringBone, the `com.unity.springbone` git package, using the 42 spring bones the model ships with). Roof pieces keep a shell (gable ends and ceiling) under their art, which only replaces the slope. The island's ground is one mesh with the `WitchCraft/SplatGround` shader (`Assets/Shaders`): grass blended with forest floor, meadow, moss and earth by a control map, like a terrain's splat map. `WorldSceneBuilder.PaintGround` paints that map (`Assets/Textures/Generated/IslandControl.png`, one channel per layer) as soft, wobbly blobs; textures are projected from above in world space. The art setup gives the same material the packs' ground textures (the `surface:island` slot). Hills are generated bell meshes (`WitchCraftBuildKit.HillMesh`) that rise from level ground with no seam, wear the same splat material (so the control map paints moss and earth across them) and collide with their exact surface, not a convex hull. Floating isles hang over jagged, faceted rock (`RockyUnderside`, seeded per isle) with a few loose crags beneath, on the `surface:rock` slot. The sky comes from the `sky:day`, `sky:dusk` and `sky:night` slots when they hold Polyverse Skies (BOXOPHOBIC) materials, filled by `PolyverseSkySetup` (presets Sky 04 and 01, and for the day a copy of Sky 02 with its neon horizon softened to a pale gold glow, saved in `ThirdParty/AssetStore/BOXOPHOBIC/WitchCraft`): `DayCycle` blends day to dusk to night by its darkness, greys the colours under cloud and sets `PolyverseSunDir`/`PolyverseMoonDir` from the lights. The working sky starts as a copy of the night preset, since a blend can't turn a material's stars or clouds on or off. With no sky art the plain skybox darkens as before. `Scenery` holds art-only spots (flowers, tufts, leaf litter, shrubs, pebbles, far mountains) that collide with nothing and never look like a gatherable; scenic mushrooms use a recoloured, glow-free copy of the forest atlas (`EnchantedRealmSetup.MutedMushrooms`), so only Glowcaps glow.
* **Audio**: `AudioDirector` (`IAudioService`) generates clips with `SoundSynth`, plays music, ambience and broom wind, and turns events into sounds.
* **Tutorial**: `TutorialGuide` (`ITutorialGuide`) runs Sable's guided steps, sets the HUD objective and places a beacon. Progress is saved.
* **Debugging**: `FeelTuner` is a backquote-toggled panel of live sliders for movement, broom and camera values. `PlaytestScenario` is the automated playtest.
* **UI**: `UIRoot` builds a uGUI canvas in code via `UIFactory` and routes menu keys. Views: `HudView`, `SatchelView`, `SpellbookView`, `AlchemyView`, `DialogueView`, `PauseView`, `BookView`, `BookcaseView`.

## Layers

| Layer | Use |
| ----- | --- |
| Ground (3) | Terrain, platforms. Walkable and the broom's ground reference |
| Environment (6) | Buildings, furniture, trees, crates. Walkable and blocks the camera |
| Interactable (7) | Trigger volumes for things you can interact with |
| Player (8) | The witch |
| Ignore Raycast (2) | Ghost previews and levitated objects, so aim rays pass through |

## Content pipeline

`Assets/Editor` holds the builders, run from the **Witch Craft** menu or in batch mode:

* `ContentBuilder` writes items, glyphs, spells, recipes, build pieces, companions and the `GameDatabase`, and builds the building, furniture and companion prefabs from primitives.
* `WorldSceneBuilder` generates the whole island scene, the witch, the cat and the camera.
* `Playtest` and `Scripts/Debugging/PlaytestScenario` run the automated playtest.

To add an item, spell, recipe, build piece or companion, add it in `ContentBuilder` and rebuild.
