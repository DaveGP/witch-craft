# Third-party assets

Asset packs that aren't ours to redistribute live here and stay out of git. Only these folders and this README are committed.

| Folder | For |
| --- | --- |
| `Synty/` | Synty packs (POLYGON and others) |
| `AssetStore/` | Anything else from the Unity Asset Store |

Asset Store packages import into their own folder under `Assets/`. After importing, drag that folder in here **inside the Unity Editor** (not in Finder), so Unity keeps every reference to it.

Anything in here is missing from a fresh clone. Before game content committed elsewhere (a prefab, a material, the scene) relies on one of these assets, either copy the parts you use and have changed out into the project, as Crewfall does, or note which pack is needed in the root README.
