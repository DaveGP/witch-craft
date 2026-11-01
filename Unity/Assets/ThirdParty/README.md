# Third-party assets

Asset packs that aren't ours to redistribute live here and stay out of git. Only these folders and this README are committed.

| Folder | For |
| --- | --- |
| `Synty/` | Synty packs (POLYGON and others) |
| `AssetStore/` | Anything else from the Unity Asset Store |

Asset Store packages import into their own folder under `Assets/`. After importing, drag that folder in here **inside the Unity Editor** (not in Finder), so Unity keeps every reference to it.

Anything in here is missing from a fresh clone. Before game content committed elsewhere (a prefab, a material, the scene) relies on one of these assets, either copy the parts you use and have changed out into the project, as Crewfall does, or note which pack is needed in the root README.

## Using a pack in the game: the Art Set

Everything the builders make is a placeholder with a named **art slot**: the witch, Sable, the broom, the six companions, all build pieces, trees, rocks, crates, the signpost, shrines, bounce mushrooms and every kind of gatherable.

1. Open `Assets/Resources/ArtSet.asset` in the Inspector. It lists every slot with a note on what it is.
2. Drag a prefab from here into a slot's **Prefab** field. The game shows it in place of the placeholder straight away, in the editor and in play.
3. Use **Offset**, **Rotation** and **Scale** to line it up. Tick **Keep Placeholder** while lining up, to see both.

Only looks change. The placeholder's collision, scripts and lights stay, so jumps, building and gathering behave exactly the same. On a machine without the pack (a fresh clone), the missing prefab is skipped and the placeholder shows.

- **Animated witch:** give her slot a rigged character with an Animator. It is driven with Crewfall's locomotion parameters (`ForwardVelocity`, `StrafeVelocity`, `Velocity`, `IsGrounded`, `Gait`, `Direction`), so Crewfall's Synty locomotion controller works, plus `IsFlying`, `VerticalVelocity` and a `Jump` trigger. Parameters the controller doesn't have are skipped.
- **Animated Sable:** **Witch Craft > Art > Set Up Casual Cat** (also run by the batch build) turns the Casual Cat - Black pack into her art: URP materials, an Animator Controller with a state per clip and a prefab variant with a `CatRigProfile` saying which state does what. Any cat with a `CatRigProfile` is crossfaded by state name; a plain Animator just gets `Speed` and `IsSitting`.
- **Standard materials** render pink in URP: right-click a material or folder and choose **Witch Craft > Convert Materials to URP Lit**.
- **Paint** only colours placeholder surfaces for now, not swapped-in art.
- Slots are added to the Art Set automatically whenever the builders run; your assignments are never changed.
