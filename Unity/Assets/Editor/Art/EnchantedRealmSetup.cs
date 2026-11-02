using System.Collections.Generic;
using System.Linq;
using Art;
using Data.ScriptableObjects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static WitchCraftEditor.WitchCraftBuildKit;
using Object = UnityEngine.Object;


namespace WitchCraftEditor
{
	// Dresses the island as an enchanted elven realm from Synty's POLYGON Elven Realm and Nature Biomes (Enchanted
	// Forest) packs, through the Art Set. Each slot gets a prefab fitted to its placeholder's size (so a 15 m pack
	// tree stands as tall as the 5 m placeholder whose trunk you bump into), surfaces get the packs' triplanar
	// materials, and a few kits (the alchemy bench's dressing, lanterns, glowing flora) are assembled from several
	// prefabs and saved beside the packs. Only empty slots are filled, so hand-picked art is never replaced.
	public static class EnchantedRealmSetup
	{
		private const string Synty = "Assets/ThirdParty/Synty";

		private const string Elven = Synty + "/PolygonElvenRealm/Prefabs";

		private const string Forest = Synty + "/PolygonNatureBiomes/PNB_Enchanted_Forest";

		private const string KitFolder = Synty + "/WitchCraft";

		public static bool PackPresent => AssetDatabase.IsValidFolder(Elven) && AssetDatabase.IsValidFolder(Forest);

		private enum Fit
		{
			// As tall as the placeholder
			Height,

			// As big as fits inside the placeholder's footprint
			Footprint,

			// Its longest side as long as the placeholder's
			Length,

			// As big as fits inside the placeholder in every direction, centred on it
			Box,
		}

		private readonly struct Pick
		{
			public readonly string Key;
			public readonly string Path;
			public readonly Fit Fit;
			public readonly float Bias;
			public readonly float Yaw;

			public Pick(string key, string path, Fit fit, float bias = 1f, float yaw = 0f)
			{
				Key = key;
				Path = path;
				Fit = fit;
				Bias = bias;
				Yaw = yaw;
			}
		}

		private static readonly Pick[] picks =
		{
			// Trees stand a little taller than the placeholders, whose trunks are all that collide
			new("tree:pine", $"{Elven}/Environment/SM_Env_Tree_02.prefab", Fit.Height, 1.3f),
			new("tree:broadleaf", $"{Forest}/Prefabs/SM_Env_Tree_Small_01.prefab", Fit.Height, 1.15f),
			new("tree:autumn", $"{Elven}/Environment/SM_Env_Tree_Glowing_01.prefab", Fit.Height, 1.3f),
			new("rock:boulder", $"{Elven}/Environment/SM_Env_Rock_Round_01.prefab", Fit.Box, 1.05f),
			new("prop:crate", $"{Elven}/Props/SM_Prop_Crate_01.prefab", Fit.Box),
			new("prop:signpost", $"{Elven}/Props/SM_Prop_Flag_01.prefab", Fit.Height),
			new("prop:shrine", $"{Elven}/Props/SM_Prop_OfferingShrine_02.prefab", Fit.Height),

			new("gather:BerryBush", $"{Elven}/Environment/SM_Env_Bush_02.prefab", Fit.Height),
			new("gather:Boulder", $"{Elven}/Environment/SM_Env_Rock_01.prefab", Fit.Box),
			new("gather:CrystalShard", $"{Forest}/Prefabs/Props/SM_Prop_Crystal_03.prefab", Fit.Box, 1.2f),
			new("gather:Driftwood", $"{Forest}/Prefabs/SM_Env_Log_02.prefab", Fit.Length),
			new("gather:FallenLog", $"{Forest}/Prefabs/SM_Env_Log_02.prefab", Fit.Length),
			new("gather:Glowcap", $"{Forest}/Prefabs/SM_Env_Mushroom_Small_Group_01.prefab", Fit.Footprint, 1.2f),
			new("gather:Lavender", $"{Elven}/Environment/Flowers/SM_Env_Flowers_02.prefab", Fit.Footprint, 1.2f),
			new("gather:Moonpetal", $"{Elven}/Environment/Flowers/SM_Env_Flowers_03.prefab", Fit.Height),
			new("gather:Nettle", $"{Forest}/Prefabs/SM_Env_Fern_02.prefab", Fit.Footprint, 1.2f),
			new("gather:Silverbells", $"{Elven}/Environment/Flowers/SM_Env_Flowers_04.prefab", Fit.Footprint, 1.2f),

			new("piece:bed", $"{Elven}/Props/SM_Prop_Bed_01.prefab", Fit.Footprint),
			new("piece:table", $"{Elven}/Props/SM_Prop_Table_02.prefab", Fit.Footprint),
			new("piece:chair", $"{Forest}/Prefabs/Props/SM_Prop_Chair_01.prefab", Fit.Footprint),
			new("piece:armchair", $"{Elven}/Props/SM_Prop_Couch_01.prefab", Fit.Footprint),
			new("piece:bookshelf", $"{Elven}/Props/SM_Prop_Shelf_01.prefab", Fit.Footprint),
			new("piece:potion_shelf", $"{Elven}/Props/SM_Prop_Cabinet_01.prefab", Fit.Footprint),
			new("piece:lamp", $"{Elven}/Props/SM_Prop_Lamp_01.prefab", Fit.Height),
			new("piece:glowcap_lamp", $"{Forest}/Prefabs/Props/SM_Prop_Lantern_Stand_02.prefab", Fit.Height),
			new("piece:shell_lamp", $"{Elven}/Props/SM_Prop_Lantern_01.prefab", Fit.Height),
			new("piece:telescope", $"{Elven}/Props/SM_Prop_Telescope_01.prefab", Fit.Height),
			new("piece:rug", $"{Elven}/Props/SM_Prop_Rug_01.prefab", Fit.Footprint),
			new("piece:planter", $"{Elven}/Props/SM_Prop_Flowerbed_01.prefab", Fit.Footprint),
			new("piece:fence", $"{Elven}/Buildings/SM_Bld_Railing_01.prefab", Fit.Length),
			new("piece:wall_timber", $"{Elven}/Base/SM_Bld_Base_Wall_01.prefab", Fit.Length),
			new("piece:wall_stone", $"{Elven}/Buildings/SM_Bld_House_Base_Wall_02.prefab", Fit.Length),
			new("piece:wall_window", $"{Elven}/Base/SM_Bld_Base_Wall_Window_01.prefab", Fit.Length),
			new("piece:wall_door", $"{Elven}/Base/SM_Bld_Base_Wall_Door_01.prefab", Fit.Length),
			new("piece:floor_timber", $"{Elven}/Buildings/SM_Bld_Floor_01.prefab", Fit.Footprint),
			new("piece:floor_stone", $"{Elven}/Base/SM_Bld_Base_Floor_01.prefab", Fit.Footprint),
			// Concave roofs rise towards the cell's back edge, the placeholders towards the front. They have no gable
			// ends, so the placeholder roof keeps its shell (gables and ceiling) under them
			new("piece:roof_thatch", $"{Elven}/Buildings/SM_Bld_Roof_Concave_01.prefab", Fit.Footprint, 1f, 180f),
			new("piece:roof_slate", $"{Elven}/Buildings/SM_Bld_Roof_Concave_Small_01.prefab", Fit.Footprint, 1f, 180f),
			new("piece:stairs", $"{Elven}/Base/SM_Bld_Base_Stairs_02.prefab", Fit.Footprint),
		};

		// Surfaces: the packs' triplanar materials paint the island's primitive shapes without needing their UVs
		private static readonly (string key, string path)[] surfaces =
		{
			("surface:grass", $"{Forest}/Materials/Moss_Enchanted_Triplanar_01.mat"),
			("surface:grass-dark", $"{Forest}/Materials/Moss_Enchanted_Triplanar_02.mat"),
			("surface:path", $"{Forest}/Materials/Dirt_Leaves_Triplanar_01.mat"),
			("surface:rock", $"{Synty}/PolygonElvenRealm/Materials/Environment/RockTriplanar_Mat_01.mat"),
		};

		[MenuItem("Witch Craft/Art/Set Up Enchanted Realm", false, 41)]
		private static void Menu()
		{
			if (!PackPresent)
			{
				EditorUtility.DisplayDialog("Enchanted Realm", $"POLYGON Elven Realm and Nature Biomes weren't found in {Synty}.", "OK");
				return;
			}

			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
				return;

			EditorSceneManager.OpenScene(WorldSceneBuilder.ScenePath);
			Run();
		}

		/// <summary>
		/// Fills the empty slots from the packs. Needs the world scene open, to measure the placeholders in it.
		/// False when the packs aren't on this machine.
		/// </summary>
		public static bool Run()
		{
			if (!PackPresent)
				return false;

			EnsureFolder(KitFolder);

			var converted = UrpMaterialConverter.ConvertFolder(Synty);
			var lowered = LowerShaderTargets(Synty);

			var set = LoadOrCreate<ArtSet>(ArtSetPath);
			var placeholders = FindPlaceholders();
			var filled = 0;

			foreach (var pick in picks)
			{
				var entry = Entry(set, pick.Key);
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pick.Path);

				if (entry.Prefab != null || prefab == null)
					continue;

				if (!placeholders.TryGetValue(pick.Key, out var bounds))
				{
					Debug.LogWarning($"{nameof(EnchantedRealmSetup)}: no placeholder found for {pick.Key}");
					continue;
				}

				FitInto(entry, prefab, bounds, pick.Fit, pick.Bias, pick.Yaw);
				filled++;
			}

			// The bounce mushroom's cap is the springy part, so the art is squashed to the cap's width and height
			filled += FillKit(set, "prop:bounce-mushroom", BounceMushroomKit());
			filled += FillKit(set, "prop:alchemy-bench", AlchemyBenchKit());
			filled += FillKit(set, "prop:hearth", HearthKit());
			filled += FillKit(set, "scatter:lantern", LanternKit());
			filled += FillKit(set, "scatter:glowflora", GlowFloraKit());
			filled += FillKit(set, "scatter:crystals", CrystalKit());

			foreach (var (key, kit) in SceneryKits())
				filled += FillKit(set, key, kit);

			// The island's ground is one splat-blended surface: the packs' ground textures on its shader
			filled += FillIslandGround(set);

			foreach (var (key, path) in surfaces)
			{
				var entry = Entry(set, key);

				if (entry.Material == null)
				{
					entry.Material = AssetDatabase.LoadAssetAtPath<Material>(path);
					filled += entry.Material != null ? 1 : 0;
				}
			}

			set.Entries = set.Entries.OrderBy(entry => entry.Key).ToList();
			set.NotifyChanged();

			EditorUtility.SetDirty(set);
			AssetDatabase.SaveAssets();

			Debug.Log($"{nameof(EnchantedRealmSetup)}: filled {filled} slots, converted {converted} materials, lowered {lowered} shaders to target 3.5.");

			return true;
		}

		// Synty's URP shaders ask for shader model 4.5, which WebGL 2 can't give, so the web build drops their only
		// subshader and draws them magenta. They use nothing beyond 3.5, so asking for 3.5 lets them run everywhere
		private static int LowerShaderTargets(string folder)
		{
			var lowered = 0;

			foreach (var guid in AssetDatabase.FindAssets("t:Shader", new[] { folder }))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);

				if (!path.EndsWith(".shader"))
					continue;

				var source = System.IO.File.ReadAllText(path);

				if (!source.Contains("#pragma target 4.5"))
					continue;

				System.IO.File.WriteAllText(path, source.Replace("#pragma target 4.5", "#pragma target 3.5"));
				AssetDatabase.ImportAsset(path);
				lowered++;
			}

			return lowered;
		}

		#region Fitting

		// Each slot's placeholder size, in the slot's own space: from build pieces' prefabs and the open scene
		private static Dictionary<string, Bounds> FindPlaceholders()
		{
			var found = new Dictionary<string, Bounds>();

			var swaps = Object.FindObjectsByType<ArtSwap>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToList();

			foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
			{
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
				swaps.AddRange(prefab.GetComponentsInChildren<ArtSwap>(true));
			}

			foreach (var swap in swaps)
			{
				if (string.IsNullOrEmpty(swap.Key) || found.ContainsKey(swap.Key))
					continue;

				var property = new SerializedObject(swap).FindProperty("placeholder");
				var renderers = Enumerable.Range(0, property.arraySize).Select(i => property.GetArrayElementAtIndex(i).objectReferenceValue as Renderer).Where(renderer => renderer != null);

				if (LocalBounds(swap.transform, renderers, out var bounds))
					found[swap.Key] = bounds;
			}

			return found;
		}

		private static bool LocalBounds(Transform space, IEnumerable<Renderer> renderers, out Bounds bounds)
		{
			bounds = default;
			var any = false;

			foreach (var renderer in renderers)
			{
				var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;

				if (mesh == null)
					continue;

				var toSpace = space.worldToLocalMatrix * renderer.transform.localToWorldMatrix;

				foreach (var corner in Corners(mesh.bounds))
				{
					var point = toSpace.MultiplyPoint3x4(corner);

					if (!any)
						bounds = new Bounds(point, Vector3.zero);
					else
						bounds.Encapsulate(point);

					any = true;
				}
			}

			return any;
		}

		private static IEnumerable<Vector3> Corners(Bounds bounds)
		{
			for (var i = 0; i < 8; i++)
				yield return new Vector3(
					(i & 1) == 0 ? bounds.min.x : bounds.max.x,
					(i & 2) == 0 ? bounds.min.y : bounds.max.y,
					(i & 4) == 0 ? bounds.min.z : bounds.max.z);
		}

		// The art's bounds turned by a yaw, at scale 1
		private static Bounds ArtBounds(GameObject prefab, float yaw)
		{
			var instance = Object.Instantiate(prefab);
			instance.hideFlags = HideFlags.HideAndDontSave;
			instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, yaw, 0f));

			try
			{
				var renderers = instance.GetComponentsInChildren<Renderer>().Where(renderer => renderer is MeshRenderer or SkinnedMeshRenderer).ToArray();

				if (renderers.Length == 0)
					return new Bounds(Vector3.zero, Vector3.one);

				var bounds = renderers[0].bounds;

				foreach (var renderer in renderers.Skip(1))
					bounds.Encapsulate(renderer.bounds);

				return bounds;
			}
			finally
			{
				Object.DestroyImmediate(instance);
			}
		}

		private static void FitInto(ArtEntry entry, GameObject prefab, Bounds placeholder, Fit fit, float bias, float yaw)
		{
			// Long things lie along the placeholder's long side
			var art = ArtBounds(prefab, yaw);

			if (fit is Fit.Length or Fit.Footprint && Elongated(placeholder.size) != Elongated(art.size) && Elongated(art.size) != 0 && Elongated(placeholder.size) != 0)
			{
				yaw += 90f;
				art = ArtBounds(prefab, yaw);
			}

			var size = art.size;
			var target = placeholder.size;

			var scale = fit switch
			{
				Fit.Height => target.y / Mathf.Max(0.01f, size.y),
				Fit.Footprint => Mathf.Min(target.x / Mathf.Max(0.01f, size.x), target.z / Mathf.Max(0.01f, size.z)),
				Fit.Length => Mathf.Max(target.x, target.z) / Mathf.Max(0.01f, Mathf.Max(size.x, size.z)),
				_ => Mathf.Min(target.x / Mathf.Max(0.01f, size.x), target.y / Mathf.Max(0.01f, size.y), target.z / Mathf.Max(0.01f, size.z)),
			} * bias;

			// Stand on the placeholder's base, centred over it (a box fit centres in every direction)
			var anchor = fit == Fit.Box ? placeholder.center : new Vector3(placeholder.center.x, placeholder.min.y, placeholder.center.z);
			var artAnchor = fit == Fit.Box ? art.center : new Vector3(art.center.x, art.min.y, art.center.z);

			entry.Prefab = prefab;
			entry.Scale = scale;
			entry.Rotation = new Vector3(0f, yaw, 0f);
			entry.Offset = anchor - artAnchor * scale;
		}

		// 1 when longer along x, -1 along z, 0 when roughly square
		private static int Elongated(Vector3 size)
		{
			if (size.x > size.z * 1.3f)
				return 1;

			return size.z > size.x * 1.3f ? -1 : 0;
		}

		#endregion

		#region Kits

		// Several pack prefabs arranged into one, saved beside the packs. Positions are in the slot's own space
		private static GameObject Kit(string name, params (string path, Vector3 position, float yaw, Vector3 scale)[] parts)
		{
			var root = new GameObject(name);

			try
			{
				foreach (var (path, position, yaw, scale) in parts)
				{
					var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

					if (prefab == null)
					{
						Debug.LogWarning($"{nameof(EnchantedRealmSetup)}: {path} is missing");
						continue;
					}

					var part = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
					part.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
					part.transform.localScale = scale;
				}

				return PrefabUtility.SaveAsPrefabAsset(root, $"{KitFolder}/{name}.prefab");
			}
			finally
			{
				Object.DestroyImmediate(root);
			}
		}

		private static int FillKit(ArtSet set, string key, GameObject kit)
		{
			var entry = Entry(set, key);

			if (entry.Prefab != null || kit == null)
				return 0;

			entry.Prefab = kit;
			entry.Scale = 1f;
			entry.Rotation = Vector3.zero;
			entry.Offset = Vector3.zero;

			return 1;
		}

		private static Vector3 Uniform(float scale) => Vector3.one * scale;

		private static GameObject BounceMushroomKit()
		{
			// The placeholder cap is 2.6 m across with its top 1.7 m up
			return Kit("BounceMushroom", ($"{Forest}/Prefabs/SM_Env_Mushroom_02.prefab", Vector3.zero, 0f, new Vector3(2.8f / 6.2f, 1.75f / 6.4f, 2.8f / 5.9f)));
		}

		// Dresses the bench around the cauldron, mortar and pestle, which stay as they are
		private static GameObject AlchemyBenchKit()
		{
			return Kit("AlchemyBench",
				($"{Elven}/Props/SM_Prop_Desk_01.prefab", new Vector3(1.55f, 0f, 0f), 0f, new Vector3(0.45f, 1f, 0.6f)),
				($"{Elven}/Props/SM_Prop_Shelf_01.prefab", new Vector3(0.8f, 0f, -1f), 0f, Uniform(0.85f)),
				($"{Elven}/Props/SM_Prop_Distiller_01.prefab", new Vector3(1.9f, 0.8f, -0.15f), 200f, Uniform(0.8f)),
				($"{Elven}/Props/SM_Prop_Potion_Stand_01.prefab", new Vector3(2.0f, 0.8f, 0.2f), 0f, Uniform(0.7f)),
				($"{Elven}/Buildings/SM_Bld_Awning_01.prefab", new Vector3(0.8f, 1.15f, -0.6f), 180f, new Vector3(1.25f, 0.8f, 1f)),
				($"{Elven}/Props/SM_Prop_Lantern_03.prefab", new Vector3(-0.85f, 0f, -1f), 0f, Uniform(1.6f)));
		}

		// Round the fire: mossy stones and two benches where the stumps are
		private static GameObject HearthKit()
		{
			var parts = new List<(string, Vector3, float, Vector3)>();

			for (var i = 0; i < 10; i++)
			{
				var angle = i * 36f;
				var rad = angle * Mathf.Deg2Rad;
				parts.Add(($"{Forest}/Prefabs/SM_Env_Rock_Pebbles_0{1 + i % 2}.prefab", new Vector3(Mathf.Cos(rad) * 0.85f, 0.05f, Mathf.Sin(rad) * 0.85f), 90f - angle, Uniform(1.6f)));
			}

			foreach (var angle in new[] { 200f, 340f })
			{
				var rad = angle * Mathf.Deg2Rad;
				parts.Add(($"{Elven}/Props/SM_Prop_Bench_Seat_01.prefab", new Vector3(Mathf.Cos(rad) * 2f, 0f, Mathf.Sin(rad) * 2f), 270f - angle, Uniform(0.75f)));
			}

			return Kit("Hearth", parts.ToArray());
		}

		private static GameObject LanternKit()
		{
			return Kit("Lantern", ($"{Elven}/Props/SM_Prop_LanternStand_01.prefab", Vector3.zero, 0f, Uniform(0.9f)));
		}

		private static GameObject GlowFloraKit()
		{
			const string mushrooms = "SM_Env_Mushroom_Small_Group_02";

			var kit = Kit("GlowFlora",
				($"{Forest}/Prefabs/{mushrooms}.prefab", Vector3.zero, 0f, Uniform(0.7f)),
				($"{Forest}/Prefabs/SM_Env_Moss_Lumps_02.prefab", new Vector3(0.3f, 0f, -0.9f), 200f, Uniform(0.6f)),
				($"{Elven}/Environment/Flowers/SM_Env_Flowers_01.prefab", new Vector3(0.9f, 0f, 0.4f), 40f, Uniform(0.8f)),
				($"{Forest}/Prefabs/SM_Env_Undergrowth_Fern_01.prefab", new Vector3(-0.6f, 0f, -0.5f), 120f, Uniform(1.6f)));

			// Only Glowcaps (which can be picked) glow: these mushrooms are just scenery, so they wear a muted brown
			// copy of the forest atlas with no glow
			if (kit != null)
				Remap(kit, mushrooms, MutedMushrooms());

			return kit;
		}

		private const string ForestAtlasMaterial = Forest + "/Materials/PolygonNatureBiomes_EnchantedForest_Mat_01_A.mat";

		// The forest atlas with its blue and cyan (the glowing mushroom caps) turned to a woodland brown, on a material
		// with emission off. Made from the pack's own texture, saved next to the kits
		private static Material MutedMushrooms()
		{
			var source = AssetDatabase.LoadAssetAtPath<Material>(ForestAtlasMaterial);
			var atlas = source != null ? source.GetTexture("_BaseMap") as Texture2D : null;

			if (atlas == null)
				return null;

			var texturePath = $"{KitFolder}/ForestAtlas_Muted.png";

			// Read the pixels through a render texture, as the pack's texture isn't marked readable
			var target = RenderTexture.GetTemporary(atlas.width, atlas.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
			Graphics.Blit(atlas, target);

			var previous = RenderTexture.active;
			RenderTexture.active = target;

			var pixels = new Texture2D(atlas.width, atlas.height, TextureFormat.RGBA32, false);
			pixels.ReadPixels(new Rect(0, 0, atlas.width, atlas.height), 0, 0);

			RenderTexture.active = previous;
			RenderTexture.ReleaseTemporary(target);

			var colours = pixels.GetPixels();

			for (var i = 0; i < colours.Length; i++)
			{
				Color.RGBToHSV(colours[i], out var hue, out var saturation, out var value);

				if (hue < 0.42f || hue > 0.72f || saturation < 0.2f)
					continue;

				var alpha = colours[i].a;
				colours[i] = Color.HSVToRGB(0.07f, saturation * 0.6f, value * 0.75f);
				colours[i].a = alpha;
			}

			pixels.SetPixels(colours);
			System.IO.File.WriteAllBytes(texturePath, pixels.EncodeToPNG());
			Object.DestroyImmediate(pixels);

			AssetDatabase.ImportAsset(texturePath);
			var muted = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

			var materialPath = $"{KitFolder}/Mushroom_Muted.mat";
			var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

			if (material == null)
			{
				material = new Material(source);
				AssetDatabase.CreateAsset(material, materialPath);
			}

			material.CopyPropertiesFromMaterial(source);
			material.SetTexture("_BaseMap", muted);
			material.SetTexture("_MainTex", muted);
			material.DisableKeyword("_EMISSION");
			material.SetTexture("_EmissionMap", null);
			material.SetColor("_EmissionColor", Color.black);
			material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;

			EditorUtility.SetDirty(material);
			AssetDatabase.SaveAssets();

			return material;
		}

		// Swaps the material on one part of a saved kit
		private static void Remap(GameObject kit, string partName, Material material)
		{
			if (material == null)
				return;

			var path = AssetDatabase.GetAssetPath(kit);
			var contents = PrefabUtility.LoadPrefabContents(path);

			try
			{
				foreach (var part in contents.GetComponentsInChildren<Transform>(true).Where(child => child.name.StartsWith(partName)))
				foreach (var renderer in part.GetComponentsInChildren<Renderer>(true))
					renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();

				PrefabUtility.SaveAsPrefabAsset(contents, path);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(contents);
			}
		}

		// Scenery: nothing here may look like something the witch gathers, so no glowing mushrooms, logs, the berry bush
		// (Bush_02), the gather flowers (Flowers_02 to _04), ferns like the nettle, or the boulder rock
		private static IEnumerable<(string key, GameObject kit)> SceneryKits()
		{
			var flowers = $"{Elven}/Environment/Flowers";
			var environment = $"{Elven}/Environment";

			yield return ("scenery:flowers-1", Kit("Flowers1",
				($"{environment}/SM_Env_Flowers_Clump_01.prefab", Vector3.zero, 0f, Uniform(1f)),
				($"{flowers}/SM_Env_Grass_01.prefab", new Vector3(0.6f, 0f, 0.3f), 60f, Uniform(1f))));
			yield return ("scenery:flowers-2", Kit("Flowers2",
				($"{environment}/SM_Env_Flowers_Clump_02.prefab", Vector3.zero, 0f, Uniform(1f)),
				($"{flowers}/SM_Env_Flowers_06.prefab", new Vector3(-0.5f, 0f, 0.4f), 120f, Uniform(1f))));
			yield return ("scenery:flowers-3", Kit("Flowers3",
				($"{environment}/SM_Env_Flowers_Clump_03.prefab", Vector3.zero, 0f, Uniform(1f)),
				($"{flowers}/SM_Env_Flowers_07.prefab", new Vector3(0.4f, 0f, -0.5f), 200f, Uniform(1f)),
				($"{flowers}/SM_Env_Grass_03.prefab", new Vector3(-0.5f, 0f, -0.2f), 30f, Uniform(1f))));

			yield return ("scenery:tuft-1", Kit("Tuft1", ($"{flowers}/SM_Generic_Grass_Patch_01.prefab", Vector3.zero, 0f, Uniform(1f))));
			yield return ("scenery:tuft-2", Kit("Tuft2", ($"{flowers}/SM_Generic_Grass_Patch_02.prefab", Vector3.zero, 0f, Uniform(1f))));
			yield return ("scenery:tuft-3", Kit("Tuft3", ($"{flowers}/SM_Env_Grass_02.prefab", Vector3.zero, 0f, Uniform(1f))));

			yield return ("scenery:leaves-1", Kit("Leaves1", ($"{environment}/SM_Env_Leaves_Clump_01.prefab", Vector3.zero, 0f, Uniform(1f))));
			yield return ("scenery:leaves-2", Kit("Leaves2",
				($"{environment}/SM_Env_Leaves_Clump_03.prefab", Vector3.zero, 0f, Uniform(1f)),
				($"{Forest}/Prefabs/SM_Env_Fern_Koru_02.prefab", new Vector3(0.6f, 0f, 0.2f), 90f, Uniform(0.8f))));
			yield return ("scenery:leaves-3", Kit("Leaves3", ($"{environment}/SM_Env_Leaves_Clump_05.prefab", Vector3.zero, 0f, Uniform(1f))));

			yield return ("scenery:pebbles-1", Kit("Pebbles1", ($"{flowers}/SM_Env_Pebbles_01.prefab", Vector3.zero, 0f, Uniform(1f))));
			yield return ("scenery:pebbles-2", Kit("Pebbles2", ($"{flowers}/SM_Env_Pebbles_02.prefab", Vector3.zero, 0f, Uniform(1f))));
			yield return ("scenery:pebbles-3", Kit("Pebbles3", ($"{flowers}/SM_Env_Pebbles_03.prefab", Vector3.zero, 0f, Uniform(1f))));

			yield return ("scenery:shrub-1", Kit("Shrub1", ($"{environment}/SM_Env_Hedge_Shrub_01.prefab", Vector3.zero, 0f, Uniform(0.8f))));
			yield return ("scenery:shrub-2", Kit("Shrub2", ($"{environment}/SM_Env_Hedge_Shrub_02.prefab", Vector3.zero, 0f, Uniform(0.8f))));
			yield return ("scenery:shrub-3", Kit("Shrub3", ($"{environment}/SM_Env_Hedge_Shrub_03.prefab", Vector3.zero, 0f, Uniform(0.8f))));

			// Sized by height rather than a guess at the model's scale
			yield return ("scenery:mountain", Kit("Mountain",
				($"{environment}/SM_Env_Mountains_Grass_01.prefab", Vector3.zero, 0f, Tall($"{environment}/SM_Env_Mountains_Grass_01.prefab", 70f)),
				($"{environment}/SM_Env_Mountains_Grass_02.prefab", new Vector3(45f, 0f, 20f), 140f, Tall($"{environment}/SM_Env_Mountains_Grass_02.prefab", 50f))));
			yield return ("scenery:mountain-snow", Kit("MountainSnow",
				($"{environment}/SM_Env_Mountains_Snow_01.prefab", Vector3.zero, 0f, Tall($"{environment}/SM_Env_Mountains_Snow_01.prefab", 95f))));
		}

		// Grass first, then the control map's layers in channel order: forest floor, meadow, moss, earth
		private static readonly (string path, Color tint)[] islandLayers =
		{
			($"{Forest}/Materials/Moss_Enchanted_Triplanar_01.mat", Color.white),
			($"{Forest}/Materials/Dirt_Leaves_Triplanar_02.mat", Color.white),
			($"{Forest}/Materials/Dirt_Grass_Triplanar_01.mat", Color.white),
			($"{Forest}/Materials/Moss_Enchanted_Triplanar_02.mat", Color.white),
			($"{Synty}/PolygonElvenRealm/Materials/Environment/Dirt_Triplanar.mat", new Color(0.85f, 0.78f, 0.72f)),
		};

		private const string IslandGroundPath = "Assets/Materials/Generated/IslandGround.mat";

		// The island's art material: WitchCraft/SplatGround with each pack ground's top texture and tiling, sharing the
		// builder's control map. Replaced each run unless something else was picked by hand
		private static int FillIslandGround(ArtSet set)
		{
			var entry = Entry(set, "surface:island");
			var placeholder = AssetDatabase.LoadAssetAtPath<Material>(IslandGroundPath);

			if (placeholder == null || entry.Material != null && !entry.Material.name.StartsWith("IslandGround_Art"))
				return 0;

			var path = $"{KitFolder}/IslandGround_Art.mat";
			var material = AssetDatabase.LoadAssetAtPath<Material>(path);

			if (material == null)
			{
				material = new Material(placeholder.shader);
				AssetDatabase.CreateAsset(material, path);
			}

			material.shader = placeholder.shader;
			material.SetTexture("_Control", placeholder.GetTexture("_Control"));
			material.SetVector("_ControlRect", placeholder.GetVector("_ControlRect"));

			for (var i = 0; i < islandLayers.Length; i++)
			{
				var (sourcePath, tint) = islandLayers[i];
				var source = AssetDatabase.LoadAssetAtPath<Material>(sourcePath);

				if (source == null)
					continue;

				Texture top = null;

				foreach (var name in new[] { "_Top", "_TopAlbedo", "_TriplanarAlbedo", "_BaseMap" })
					if (top == null && source.HasProperty(name))
						top = source.GetTexture(name);

				var tiling = source.HasProperty("_Tiling") ? source.GetFloat("_Tiling") : source.HasProperty("_TilingTop") ? source.GetFloat("_TilingTop") : 0.5f;

				var texture = i == 0 ? "_BaseMap" : $"_Layer{i}";
				var colour = i == 0 ? "_BaseColor" : $"_Tint{i}";
				var scale = i == 0 ? "_Tiling" : $"_Tiling{i}";

				material.SetTexture(texture, top);
				material.SetColor(colour, tint);
				material.SetFloat(scale, tiling);
			}

			EditorUtility.SetDirty(material);
			entry.Material = material;

			return 1;
		}

		private static Vector3 Tall(string path, float height)
		{
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

			return prefab == null ? Vector3.one : Uniform(height / Mathf.Max(0.01f, ArtBounds(prefab, 0f).size.y));
		}

		private static GameObject CrystalKit()
		{
			return Kit("Crystals",
				($"{Forest}/Prefabs/Props/SM_Prop_Crystal_02.prefab", Vector3.zero, 0f, Uniform(0.8f)),
				($"{Forest}/Prefabs/Props/SM_Prop_Crystal_03.prefab", new Vector3(0.8f, 0f, 0.3f), 70f, Uniform(0.7f)),
				($"{Forest}/Prefabs/SM_Env_Moss_Lumps_01.prefab", new Vector3(-0.3f, 0f, -0.4f), 0f, Uniform(0.5f)));
		}

		#endregion

		private static ArtEntry Entry(ArtSet set, string key)
		{
			var entry = set.Entries.FirstOrDefault(existing => existing.Key == key);

			if (entry == null)
			{
				entry = new ArtEntry { Key = key };
				set.Entries.Add(entry);
			}

			return entry;
		}
	}
}
