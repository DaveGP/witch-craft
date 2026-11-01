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

* **Interaction**: `IInteractable` with `Gatherable` (respawning or infinite, interact or touch), `GlyphShrine`, `RecipeScroll`, `AlchemyStation`, `Hearth`, `CompanionRecruit`, `CompanionResident` and `CatCompanion`.
* **Alchemy**: `AlchemySession` holds the brewing rules with no Unity dependencies. `AlchemyStation` connects it to the satchel and drives the cauldron, fire and mortar visuals. `AlchemyView` is the bench UI.
* **Spells**: `SpellBook` stores glyphs, spells and slots and resolves inscriptions. `SpellCaster` readies the slotted spell's `ISpellEffect` component. Effects derive from `SpellEffect` (aiming, services) and live on the player: `ConjureSpell`, `LevitateSpell`, `UnmakeSpell`, `GustSpell`, `BloomSpell`, `WispSpell`.
* **Home**: `HomePlot` snaps pieces to the grid (floors and roofs to cells, walls to edges, furniture free). `HomeService` places, removes, moves and saves pieces, seeds the starter hut and handles expansion. `PlacedPiece` ties a scene object to its save record; `Movable` lets Levitate lift it.
* **Companions**: `CompanionRoster` holds friendship and grants rewards. `CompanionSpawner` creates residents at home.
* **World**: `MovingPlatform`, `BouncePad`, `Updraft`, `HiddenPath`, `BloomSpot`, `Campfire`, `Bobbing`.
* **Sky**: `DayCycle` (`IDayCycle`) moves the sun and moon and drives ambient light, fog, sky exposure, reflections and fireflies; time is saved. `WeatherSystem` (`IWeather`) rolls weather and drives rain particles and sound.
* **Audio**: `AudioDirector` (`IAudioService`) generates clips with `SoundSynth`, plays music, ambience and broom wind, and turns events into sounds.
* **Tutorial**: `TutorialGuide` (`ITutorialGuide`) runs Sable's guided steps, sets the HUD objective and places a beacon. Progress is saved.
* **Debugging**: `FeelTuner` is a backquote-toggled panel of live sliders for movement, broom and camera values. `PlaytestScenario` is the automated playtest.
* **UI**: `UIRoot` builds a uGUI canvas in code via `UIFactory` and routes menu keys. Views: `HudView`, `SatchelView`, `SpellbookView`, `AlchemyView`, `DialogueView`, `PauseView`.

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
