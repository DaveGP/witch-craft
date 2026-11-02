# Witch Craft

## Overview

**Witch Craft** is a cosy 3D platformer about a young witch building a home.

Run, jump and fly around a little island gathering herbs for potions, glyphs for spells and materials for your house. Return home to brew, inscribe and build, then head back out to explore further. Magical companions you befriend along the way move in, and you can visit with them, give gifts and learn their secrets.

**Play it in your browser: https://davegp.github.io/witch-craft/**

## Features

* Snappy platforming: coyote time, buffered and variable height jumps, double jump, bounce mushrooms, moving platforms
* Broom flight with a recharging flight meter and a hover ceiling, lifted higher by updrafts
* Sable, a cat familiar who follows you everywhere and rides on your broom
* An alchemy bench brewing minigame: follow recipes step by step, grind in the mortar, keep the fire in the boiling band with the bellows while the hourglass runs
* A spellbook where you combine glyphs to discover spells
* Building spells: Conjure walls, floors, roofs and furniture onto a snapping grid, Levitate furniture to rearrange it, Unmake to get materials back
* A build catalogue with costs and comfort, ten paint colours and a paintbrush, undo, and copying a built piece's type and colour
* Home comfort: furniture makes companions fonder each morning, especially their favourite piece
* Companions to recruit with gifts, who live at home and reward friendship with recipes
* A home plot that expands as you gather materials and friends
* With the local art packs: a rigged witch who walks, runs, jumps and casts each spell with its own gesture, and an island dressed with varied ground, smooth rolling hills, wildflowers, leaf litter and far mountains under a Polyverse sky that shifts from day to dusk to starry night
* Six storybooks hidden around the world, each a short tale to read; found books go on your bookcase at home to read again
* A guided first five minutes: Sable suggests what to try next, with a beacon showing where
* Sound effects, ambience and a music loop, all generated in code, with separate music, sound and ambience volumes
* A second island across the sea and floating isles above it, with new glyphs, spells (Bubble, Rain Call, Blink), a water-walking potion and two more companions
* Day and night with moonlit nights and fireflies, night-only flowers, companions who sleep, beds to sleep or nap in, and passing rain showers that make herbs regrow faster
* Name your witch when you start; companions use it, and the hearth lets you change it
* A title screen with three save slots, autosave, and saving by resting at the hearth or in bed
* Settings for look sensitivity, invert Y, music and sound volume, with a controls reference
* Gamepad support: stick look, menus navigable with the D-pad, prompts that follow the device in use
* A Witch Craft loading screen for the web build, with saves kept between visits

## Development

### Technology Stack

| Technology      | Details       |
| --------------- | ------------- |
| Engine          | Unity 6 (6000.5.3f1) |
| Render pipeline | URP           |
| Language        | C#            |
| Input           | Input System  |
| Version Control | Git + Git LFS |

## Project Structure

```text
witch-craft/
│
├── Unity/                   # The Unity Project
├──── Assets/
├────── Scripts/               # Runtime code (see Documentation/TechnicalDesign.md)
├────── Editor/                # Content, world and playtest builders
├────── Controls/              # Input action map
├────── Data/                  # Generated items, glyphs, spells, recipes, build pieces, companions, storybooks
├────── Prefabs/               # Generated building, furniture and companion prefabs
├────── Resources/             # GameDatabase
├────── Scenes/                # Generated WitchCraft scene
├────── ThirdParty/            # Local only: Synty/ and AssetStore/ packs (not committed, see its README)
├──── Packages/
├──── ProjectSettings/
│
├── Documentation/           # Design and technical documentation
│
├── README.md
├── .gitignore
└── .gitattributes
```

## Getting Started

Open `Unity/` in Unity Hub with Unity 6000.5.3f1, open `Assets/Scenes/WitchCraft.unity` and press Play.

The data assets, prefabs and the scene are generated from code. After changing a builder, run **Witch Craft > Build Everything** in the editor, or from a terminal:

```bash
/Applications/Unity/Hub/Editor/6000.5.3f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath Unity -executeMethod WitchCraftEditor.WorldSceneBuilder.BuildEverythingBatch -logFile Unity/Logs/build.log
```

### Web build

The browser version is served by GitHub Pages from the `gh-pages` branch. To build and publish it (close the Unity Editor first):

```bash
./deploy-webgl.sh
```

### Automated playtest

A scripted playtest enters Play mode, drives the game with real key presses, and checks movement, jumping, spells, building, brewing, the broom, menus, recruiting a companion and expanding the home. It writes a report and screenshots to `Unity/Logs/Playtest/`.

```bash
/Applications/Unity/Hub/Editor/6000.5.3f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath Unity -executeMethod WitchCraftEditor.Playtest.Run -witchcraftPlaytest -logFile Unity/Logs/playtest.log
```

The playtest deletes your local save before it runs.

## Controls

| Action              | Input            |
| ------------------- | ---------------- |
| Move                | WASD             |
| Look                | Mouse            |
| Jump (hold for higher) / climb on broom | Space |
| Sprint / broom boost | Left Shift      |
| Descend on broom    | Left Ctrl or C   |
| Interact            | E                |
| Mount / dismount broom | F             |
| Ready spell (press again to put away) | 1-4 |
| Cast                | Left mouse       |
| Spell alternate (cancel, swap category) | Right mouse |
| Rotate held or conjured piece | Q / R  |
| Build catalogue (with Conjure) | Right mouse |
| Undo last conjured piece | Z |
| Copy a built piece (with Conjure) | Middle mouse or V |
| Next piece / push and pull levitated object | Mouse wheel |
| Satchel             | Tab              |
| Spellbook           | B                |
| Menu / leave a station | Escape        |
| Bellows at the bench | Space           |
| Grind at the bench  | G                |
| Feel tuning panel (dev) | ` (backquote) |

Gamepad: left stick move, right stick look, A jump, X interact, Y broom, RT cast, LT spell alternate, LB/RB rotate, D-pad left/right ready spell 1/2, D-pad up/down next piece, View satchel, right stick click spellbook, Start menu, B back.

## Documentation

* [Game Design Document](Documentation/GameDesign.md)
* [Technical Design](Documentation/TechnicalDesign.md)
* [Decisions](Documentation/Decisions.md)

## Credits

Created by David Pearson.

The witch's hair and cloth sway with [UnityChanSpringBone](https://github.com/unity3d-jp/UnityChanSpringBone) by Unity Technologies Japan (MIT licence), a git package in `Packages/manifest.json` pinned to 1.2.1-preview. On first import Unity's script updater fixes one obsolete attribute in its editor code automatically.

## License

This project is currently private and all rights reserved.
