using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy;
using Audio;
using Debugging;
using Camera;
using Character;
using Character.Actions;
using Character.Animation;
using Character.Movement;
using Companions;
using Data.ScriptableObjects;
using Home;
using Interaction;
using LocalPlayer.Targeting;
using Potions;
using Spells;
using Spells.Effects;
using SystemManagement;
using Sky;
using Tutorial;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UserInterface;
using World;
using static WitchCraftEditor.WitchCraftBuildKit;
using Object = UnityEngine.Object;
using Random = System.Random;


namespace WitchCraftEditor
{
	// Generates Assets/Scenes/WitchCraft.unity: a meadow island with the witch's home plot at its heart, a forest
	// to the north east, a pond to the west, rolling hills and a crystal cave to the south, a platforming spire
	// to the east topped by an updraft to the sky island, and a hidden path to a secret isle.
	public static class WorldSceneBuilder
	{
		public const string ScenePath = "Assets/Scenes/WitchCraft.unity";

		public static readonly Vector3 SpawnPoint = new(0f, 0.1f, -14f);

		public static readonly Vector3 HomeCentre = Vector3.zero;

		public static readonly Vector3 ForestCentre = new(32f, 0f, 38f);

		public static readonly Vector3 PondCentre = new(-40f, 0f, 12f);

		public static readonly Vector3 HillsCentre = new(-5f, 0f, -45f);

		public static readonly Vector3 CaveCentre = new(34f, 0f, -46f);

		public static readonly Vector3 SpireCentre = new(50f, 0f, -8f);

		public static readonly Vector3 SkyIsleCentre = new(44f, 34f, 14f);

		public static readonly Vector3 SecretIsleCentre = new(4f, 12f, -72f);

		public const float SpireTop = 21.5f;

		public static readonly Vector3 TidewaterCentre = new(10f, 0f, 150f);

		public static readonly Vector3 StarfallCentre = TidewaterCentre + new Vector3(9f, 46f, 14f);

		public static readonly Vector3 AstraIsleCentre = TidewaterCentre + new Vector3(9f, 55f, -6f);

		public static readonly Vector3 CometNookCentre = AstraIsleCentre + new Vector3(-14f, 0f, -5f);

		private static Random random;

		private static GameDatabase database;

		private static Transform world;

		[MenuItem("Witch Craft/Build World Scene")]
		public static void BuildMenu()
		{
			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
				return;

			Build();
		}

		[MenuItem("Witch Craft/Build Everything")]
		public static void BuildEverything()
		{
			if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
				return;

			ContentBuilder.Build();
			Build();
		}

		// Entry point for -batchmode -executeMethod: builds content and scene, captures review shots and quits
		public static void BuildEverythingBatch()
		{
			try
			{
				ContentBuilder.Build();
				Build();

				// Local-only art packs, when this machine has them
				CasualCatSetup.Run();

				EditorApplication.Exit(0);
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				EditorApplication.Exit(1);
			}
		}

		public static void Build()
		{
			random = new Random(42);

			database = AssetDatabase.LoadAssetAtPath<GameDatabase>(ContentBuilder.DatabasePath);

			if (database == null)
				throw new InvalidOperationException("No GameDatabase. Run Witch Craft > Build Content first.");

			var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

			BuildLighting();
			BuildSystems();

			world = new GameObject("World").transform;

			BuildIsland();
			BuildHills();

			Physics.SyncTransforms();

			BuildHome();
			BuildForest();
			BuildPond();
			BuildHillLife();
			BuildCave();
			BuildSpire();
			BuildSkyIsle();
			BuildSecretIsle();
			BuildSeaCrossing();
			BuildTidewater();
			BuildStarfall();
			BuildStorybooks();
			BuildPaths();
			BuildScatter();

			var player = BuildPlayer();
			BuildCat(player);
			BuildCamera();

			SyncArtSet();

			EnsureFolder("Assets/Scenes");

			EditorSceneManager.SaveScene(scene, ScenePath);

			EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

			Debug.Log($"WorldSceneBuilder: Built {ScenePath}.");
		}

		#region Materials

		private static Material Grass => Lit("Grass", new Color(0.56f, 0.76f, 0.42f));
		private static Material GrassDark => Lit("GrassDark", new Color(0.46f, 0.68f, 0.4f));
		private static Material Sand => Lit("Sand", new Color(0.93f, 0.86f, 0.68f));
		private static Material PathMat => Lit("Path", new Color(0.78f, 0.66f, 0.5f));
		private static Material Rock => Lit("Rock", new Color(0.6f, 0.58f, 0.64f));
		private static Material RockDark => Lit("RockDark", new Color(0.4f, 0.38f, 0.46f));
		private static Material Trunk => Lit("Trunk", new Color(0.47f, 0.33f, 0.24f));
		private static Material Water => Transparent("Water", new Color(0.35f, 0.62f, 0.85f, 0.65f), 0.92f);
		private static Material Leaf(int index) => index switch
		{
			0 => Lit("Leaf0", new Color(0.36f, 0.62f, 0.38f)),
			1 => Lit("Leaf1", new Color(0.46f, 0.7f, 0.36f)),
			2 => Lit("Leaf2", new Color(0.62f, 0.78f, 0.42f)),
			_ => Lit("Leaf3", new Color(0.9f, 0.62f, 0.38f)),
		};
		private static Material Wood => Lit("Wood", new Color(0.6f, 0.42f, 0.27f));
		private static Material DarkWood => Lit("DarkWood", new Color(0.4f, 0.27f, 0.18f));
		private static Material Iron => Lit("Iron", new Color(0.18f, 0.17f, 0.2f), 0.5f);
		private static Material Flame => Lit("Flame", new Color(1f, 0.45f, 0.12f), 0.2f, new Color(1f, 0.35f, 0.05f) * 1.6f);
		private static Material ShrineStone => Lit("ShrineStone", new Color(0.7f, 0.68f, 0.74f));
		private static Material Crystal => Lit("CrystalGlow", new Color(0.35f, 0.75f, 0.95f), 0.9f, new Color(0.15f, 0.55f, 0.9f) * 1.1f);
		private static Material HiddenStone => Lit("HiddenStone", new Color(0.7f, 1f, 0.95f), 0.6f, new Color(0.3f, 0.9f, 0.8f) * 1.5f);

		#endregion

		#region EnvironmentLayer

		private static void BuildLighting()
		{
			var sun = new GameObject("Sun").AddComponent<Light>();
			sun.type = LightType.Directional;
			sun.color = new Color(1f, 0.93f, 0.82f);
			sun.intensity = 1.4f;
			sun.shadows = LightShadows.Soft;
			sun.shadowStrength = 0.75f;
			sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

			var moon = new GameObject("Moon").AddComponent<Light>();
			moon.type = LightType.Directional;
			moon.color = new Color(0.62f, 0.7f, 1f);
			moon.intensity = 0f;
			moon.shadows = LightShadows.Soft;
			moon.shadowStrength = 0.5f;
			moon.enabled = false;

			var sky = new GameObject("Sky");
			var dayCycle = sky.AddComponent<DayCycle>();
			Set(dayCycle, "sun", sun);
			Set(dayCycle, "moon", moon);
			sky.AddComponent<WeatherSystem>();

			RenderSettings.sun = sun;
			RenderSettings.skybox = Skybox("Sky");
			RenderSettings.ambientMode = AmbientMode.Trilight;
			RenderSettings.ambientSkyColor = new Color(0.66f, 0.66f, 0.85f);
			RenderSettings.ambientEquatorColor = new Color(0.62f, 0.57f, 0.62f);
			RenderSettings.ambientGroundColor = new Color(0.38f, 0.34f, 0.32f);
			RenderSettings.fog = true;
			RenderSettings.fogMode = FogMode.Linear;
			RenderSettings.fogColor = new Color(0.78f, 0.75f, 0.9f);
			RenderSettings.fogStartDistance = 70f;
			RenderSettings.fogEndDistance = 260f;

			var volume = new GameObject("PostProcessing").AddComponent<Volume>();
			volume.isGlobal = true;
			volume.sharedProfile = BuildVolumeProfile();
		}

		private static VolumeProfile BuildVolumeProfile()
		{
			const string path = "Assets/Settings/WitchCraftVolume.asset";

			var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);

			if (profile == null)
			{
				profile = ScriptableObject.CreateInstance<VolumeProfile>();
				AssetDatabase.CreateAsset(profile, path);
			}

			foreach (var component in profile.components.ToList())
			{
				profile.Remove(component.GetType());
				Object.DestroyImmediate(component, true);
			}

			var bloom = profile.Add<Bloom>(true);
			bloom.intensity.Override(0.7f);
			bloom.threshold.Override(0.95f);
			bloom.scatter.Override(0.65f);

			var tonemapping = profile.Add<Tonemapping>(true);
			tonemapping.mode.Override(TonemappingMode.Neutral);

			var colour = profile.Add<ColorAdjustments>(true);
			colour.postExposure.Override(0.15f);
			colour.saturation.Override(14f);
			colour.contrast.Override(6f);

			var vignette = profile.Add<Vignette>(true);
			vignette.intensity.Override(0.22f);

			foreach (var component in profile.components)
				if (!AssetDatabase.IsSubAsset(component))
					AssetDatabase.AddObjectToAsset(component, profile);

			EditorUtility.SetDirty(profile);
			AssetDatabase.SaveAssets();

			return profile;
		}

		private static void BuildSystems()
		{
			new GameObject("Systems").AddComponent<SystemsRunner>();
			new GameObject("UI").AddComponent<UIRoot>();
			new GameObject("Audio").AddComponent<AudioDirector>();
			new GameObject("Tutorial").AddComponent<TutorialGuide>();
			new GameObject("FeelTuner").AddComponent<FeelTuner>();
		}

		private static void BuildIsland()
		{
			var island = Prim(PrimitiveType.Cylinder, "Island", world, new Vector3(0, -2f, 0), new Vector3(170f, 2f, 170f), Grass);
			island.layer = Ground;
			ReplaceWithMeshCollider(island);

			var beach = Prim(PrimitiveType.Cylinder, "Beach", world, new Vector3(0, -2.08f, 0), new Vector3(180f, 2f, 180f), Sand);
			beach.layer = Ground;
			ReplaceWithMeshCollider(beach);

			var sea = Prim(PrimitiveType.Plane, "Sea", world, new Vector3(0, -0.6f, 0), new Vector3(70f, 1f, 70f), Water, false);
			sea.layer = 4;

			// Solid only to a witch who has drunk Seafoam Draught (see PotionAura)
			var surface = Node("SeaSurface", world, new Vector3(0, -0.7f, 0));
			surface.layer = 4;
			surface.AddComponent<BoxCollider>().size = new Vector3(700f, 0.2f, 700f);

			var seabed = Prim(PrimitiveType.Cylinder, "Seabed", world, new Vector3(0, -4f, 0), new Vector3(700f, 1f, 700f), Lit("Seabed", new Color(0.3f, 0.45f, 0.55f)), false);
			seabed.layer = 4;
		}

		private static readonly (Vector3 centre, float radius, float height, int material)[] hills =
		{
			(new Vector3(-14f, 0, -44f), 14f, 5f, 0),
			(new Vector3(4f, 0, -50f), 12f, 7f, 1),
			(new Vector3(-2f, 0, -33f), 10f, 3f, 0),
			(new Vector3(17f, 0, -36f), 9f, 4f, 1),
			(new Vector3(-26f, 0, -58f), 12f, 6f, 0),
			(new Vector3(-55f, 0, -20f), 11f, 4f, 1),
			(new Vector3(60f, 0, 40f), 14f, 5f, 0),
			(new Vector3(-20f, 0, 50f), 12f, 4f, 1),
		};

		private static void BuildHills()
		{
			var parent = Node("Hills", world).transform;

			foreach (var (centre, radius, height, material) in hills)
				Hill(parent, centre, radius, height, material == 0 ? GrassDark : Grass);
		}

		private static void Hill(Transform parent, Vector3 centre, float radius, float height, Material material)
		{
			// A squashed sphere sunk into the ground so its sides slope gently
			var depth = height * 2.5f;

			var hill = Prim(PrimitiveType.Sphere, "Hill", parent, new Vector3(centre.x, height - depth, centre.z), new Vector3(radius * 2.5f, depth * 2f, radius * 2.5f), material);
			hill.layer = Ground;
			ReplaceWithMeshCollider(hill);
		}

		private static void ReplaceWithMeshCollider(GameObject target)
		{
			Object.DestroyImmediate(target.GetComponent<Collider>());

			target.AddComponent<MeshCollider>().sharedMesh = target.GetComponent<MeshFilter>().sharedMesh;
		}

		private static float GroundY(float x, float z, float from = 120f)
		{
			Physics.SyncTransforms();

			return Physics.Raycast(new Vector3(x, from, z), Vector3.down, out var hit, from + 50f, LayerMask.GetMask("Ground"), QueryTriggerInteraction.Ignore)
				? hit.point.y
				: 0f;
		}

		private static Vector3 OnGround(float x, float z) => new(x, GroundY(x, z), z);

		private static float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

		private static Vector3 Around(Vector3 centre, float minRadius, float maxRadius)
		{
			var angle = Range(0f, Mathf.PI * 2f);
			var distance = Range(minRadius, maxRadius);

			return OnGround(centre.x + Mathf.Cos(angle) * distance, centre.z + Mathf.Sin(angle) * distance);
		}

		#endregion

		#region Props

		private static GameObject Tree(Transform parent, Vector3 position, float scale, int style)
		{
			var tree = Node("Tree", parent, position, Range(0, 360)).transform;
			tree.gameObject.layer = EnvironmentLayer;
			tree.localScale = Vector3.one * scale;

			Prim(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0, 1.5f, 0), new Vector3(0.45f, 1.5f, 0.45f), Trunk);

			if (style == 0)
			{
				// Pine
				for (var i = 0; i < 3; i++)
					MeshObject("Needles", tree, Cone(), new Vector3(0, 1.8f + i * 1.3f, 0), new Vector3(3.2f - i * 0.8f, 2.4f, 3.2f - i * 0.8f), Leaf(0));
			}
			else
			{
				// Round broadleaf, a few overlapping puffs
				var leaf = Leaf(style);

				Prim(PrimitiveType.Sphere, "Crown", tree, new Vector3(0, 3.8f, 0), new Vector3(3.2f, 2.8f, 3.2f), leaf, false);
				Prim(PrimitiveType.Sphere, "Crown", tree, new Vector3(0.9f, 3.3f, 0.4f), new Vector3(2f, 1.8f, 2f), leaf, false);
				Prim(PrimitiveType.Sphere, "Crown", tree, new Vector3(-0.8f, 3.4f, -0.5f), new Vector3(2.1f, 1.9f, 2.1f), leaf, false);
			}

			var kind = style switch { 0 => "pine", 3 => "autumn", _ => "broadleaf" };
			ArtSlot(tree.gameObject, $"tree:{kind}", $"Tree ({kind})");

			return tree.gameObject;
		}

		private static void Boulder(Transform parent, Vector3 position, float size)
		{
			var boulder = Prim(PrimitiveType.Sphere, "Boulder", parent, position, new Vector3(size * Range(0.9f, 1.3f), size * Range(0.6f, 0.9f), size * Range(0.9f, 1.3f)), Rock, true,
				new Vector3(Range(-10, 10), Range(0, 360), Range(-10, 10)));
			boulder.layer = Ground;
			ReplaceWithMeshCollider(boulder);

			ArtSlot(boulder, "rock:boulder", "Boulder (art stretches with the placeholder's shape)");
		}

		private static GameObject Gatherable(Transform parent, string name, Vector3 position, string itemId, int amount, float respawn, Action<Transform> visual, bool touch = false, string verb = "Gather", bool nightOnly = false)
		{
			var root = Node(name, parent, position, Range(0, 360));
			root.layer = Interactable;

			Trigger(root, touch ? 0.7f : 0.9f, new Vector3(0, 0.4f, 0));

			var look = Node("Visual", root.transform);
			visual(look.transform);
			SetLayer(look, Interactable);

			ArtSlot(look, $"gather:{name}", $"{name} ({ContentBuilder.Item(itemId).DisplayName})");

			foreach (var collider in look.GetComponentsInChildren<Collider>())
				Object.DestroyImmediate(collider);

			var gatherable = root.AddComponent<Gatherable>();
			Set(gatherable, "item", ContentBuilder.Item(itemId));
			SetValue(gatherable, "amount", amount);
			SetValue(gatherable, "respawnSeconds", respawn);
			SetBool(gatherable, "collectOnTouch", touch);
			SetString(gatherable, "verb", verb);
			Set(gatherable, "visual", look);
			SetBool(gatherable, "nightOnly", nightOnly);

			return root;
		}

		private static void Flowers(Transform visual, Color petal, int count, float height = 0.35f)
		{
			var stem = Lit("Stem", new Color(0.35f, 0.6f, 0.3f));
			var bloom = Lit($"Petal_{UIFactoryHex(petal)}", petal, 0.3f, petal * 0.15f);

			for (var i = 0; i < count; i++)
			{
				var offset = new Vector3(Range(-0.35f, 0.35f), 0, Range(-0.35f, 0.35f));
				var h = height * Range(0.8f, 1.2f);

				Prim(PrimitiveType.Cylinder, "Stem", visual, offset + Vector3.up * h * 0.5f, new Vector3(0.04f, h * 0.5f, 0.04f), stem, false);
				Prim(PrimitiveType.Sphere, "Bloom", visual, offset + Vector3.up * h, new Vector3(0.14f, 0.18f, 0.14f), bloom, false);
			}
		}

		private static string UIFactoryHex(Color colour) => ColorUtility.ToHtmlStringRGB(colour);

		private static void Mushrooms(Transform visual, Material cap, int count, float size = 0.25f)
		{
			var stem = Lit("MushroomStem", new Color(0.95f, 0.92f, 0.82f));

			for (var i = 0; i < count; i++)
			{
				var offset = new Vector3(Range(-0.3f, 0.3f), 0, Range(-0.3f, 0.3f));
				var s = size * Range(0.7f, 1.2f);

				Prim(PrimitiveType.Cylinder, "Stem", visual, offset + Vector3.up * s * 0.6f, new Vector3(s * 0.35f, s * 0.6f, s * 0.35f), stem, false);
				Prim(PrimitiveType.Sphere, "Cap", visual, offset + Vector3.up * s * 1.2f, new Vector3(s * 1.4f, s * 0.7f, s * 1.4f), cap, false);
			}
		}

		private static Material GlowcapMat => Lit("GlowcapCap", new Color(0.35f, 1f, 0.85f), 0.5f, new Color(0.2f, 0.9f, 0.7f) * 2f);

		private static void Glowcaps(Transform parent, Vector3 position)
			=> Gatherable(parent, "Glowcap", position, "glowcap", 1, 120f, visual =>
			{
				Mushrooms(visual, GlowcapMat, 3);
				PointLight(visual, new Vector3(0, 0.4f, 0), new Color(0.4f, 1f, 0.85f), 3f, 0.8f);
			});

		private static void Shrine(Transform parent, Vector3 position, string glyphId, float yaw = 0f)
		{
			var glyph = ContentBuilder.Glyph(glyphId);

			var root = Node($"Shrine_{glyph.DisplayName}", parent, position, yaw);
			root.layer = EnvironmentLayer;

			Prim(PrimitiveType.Cylinder, "Plinth", root.transform, new Vector3(0, 0.15f, 0), new Vector3(2.2f, 0.15f, 2.2f), ShrineStone);
			Prim(PrimitiveType.Cube, "Stone", root.transform, new Vector3(0, 1.3f, 0), new Vector3(1.1f, 2.4f, 0.45f), ShrineStone);
			Prim(PrimitiveType.Cube, "Cap", root.transform, new Vector3(0, 2.6f, 0), new Vector3(1.3f, 0.2f, 0.6f), RockDark);

			var runeMaterial = Lit($"Rune_{glyph.Id}", glyph.Color, 0.5f, glyph.Color * 3f);

			var rune = Node("Rune", root.transform, new Vector3(0, 3.4f, 0));
			Prim(PrimitiveType.Cube, "Rune", rune.transform, Vector3.zero, new Vector3(0.5f, 0.5f, 0.5f), runeMaterial, false, new Vector3(45, 0, 45));
			rune.AddComponent<Bobbing>();

			Prim(PrimitiveType.Cube, "Carving", root.transform, new Vector3(0, 1.5f, 0.23f), new Vector3(0.5f, 0.5f, 0.02f), runeMaterial, false, new Vector3(0, 0, 45));

			var glow = PointLight(root.transform, new Vector3(0, 3f, 0), glyph.Color, 8f, 3f);

			var zone = Node("Interact", root.transform, new Vector3(0, 1f, 0));
			zone.layer = Interactable;
			Trigger(zone, 1.8f);

			// The floating rune and its carving stay visible over any art
			ArtSlot(root, "prop:shrine", "Glyph shrine (rune and carving always show)", rune.transform, root.transform.Find("Carving"));

			var shrine = root.AddComponent<GlyphShrine>();
			Set(shrine, "glyph", glyph);
			Set(shrine, "rune", rune);
			Set(shrine, "glow", glow);
		}

		private static void Scroll(Transform parent, Vector3 position, string recipeId)
		{
			var root = Node($"Scroll_{recipeId}", parent, position);
			root.layer = Interactable;
			Trigger(root, 1f, new Vector3(0, 0.6f, 0));

			var visual = Node("Visual", root.transform, new Vector3(0, 0.6f, 0));
			Prim(PrimitiveType.Cylinder, "Paper", visual.transform, Vector3.zero, new Vector3(0.18f, 0.3f, 0.18f), Lit("Parchment", new Color(0.97f, 0.9f, 0.72f), 0.1f, new Color(1f, 0.9f, 0.6f) * 0.4f), false, new Vector3(0, 0, 90));
			Prim(PrimitiveType.Cylinder, "Ribbon", visual.transform, Vector3.zero, new Vector3(0.2f, 0.03f, 0.2f), Lit("Ribbon", new Color(0.8f, 0.25f, 0.3f)), false, new Vector3(0, 0, 90));
			visual.AddComponent<Bobbing>();
			PointLight(visual.transform, Vector3.zero, new Color(1f, 0.9f, 0.6f), 3f, 1f);

			Set(root.AddComponent<RecipeScroll>(), "recipe", ContentBuilder.Recipe(recipeId));
		}

		// One storybook in every corner of the world, each on its own little lectern
		private static void BuildStorybooks()
		{
			var books = Node("Storybooks", world).transform;

			Storybook(books, OnGround(ForestCentre.x - 2f, ForestCentre.z - 3f), "silver_spellbook", 30f);
			Storybook(books, OnGround(PondCentre.x - 8f, PondCentre.z + 14f), "eye_of_newt", 150f);
			Storybook(books, TidewaterCentre + new Vector3(50f, 2.8f, 3.6f), "bubble_witch", -60f);
			Storybook(books, SkyIsleCentre + new Vector3(3f, 0f, -2.5f), "phoenix_claw", 210f);
			Storybook(books, StarfallCentre + new Vector3(-4f, 0f, -2.5f), "counted_stars", 160f);

			// The cave roof is ground too, so the cave floor height is given rather than found
			Storybook(books, CaveCentre + new Vector3(3.5f, 0.06f, -2.4f), "lantern_hill", 20f);
		}

		private static void Storybook(Transform parent, Vector3 position, string bookId, float yaw)
		{
			var book = database.FindBook(bookId);

			var root = Node($"Storybook_{bookId}", parent, position);
			root.transform.rotation = Quaternion.Euler(0, yaw, 0);
			root.layer = Interactable;
			Trigger(root, 1.2f, new Vector3(0, 0.9f, 0));

			var lectern = Node("Lectern", root.transform, Vector3.zero);
			Prim(PrimitiveType.Cylinder, "Foot", lectern.transform, new Vector3(0, 0.05f, 0), new Vector3(0.5f, 0.05f, 0.5f), Wood, false);
			Prim(PrimitiveType.Cube, "Post", lectern.transform, new Vector3(0, 0.45f, 0), new Vector3(0.12f, 0.8f, 0.12f), Wood);
			Prim(PrimitiveType.Cube, "Rest", lectern.transform, new Vector3(0, 0.88f, 0), new Vector3(0.55f, 0.05f, 0.42f), Wood, false, new Vector3(-20, 0, 0));

			// Open and floating just above the rest, in the book's own colour
			var visual = Node("Book", root.transform, new Vector3(0, 1.08f, 0));
			var cover = Lit($"BookCover_{bookId}", book.Color, 0.2f, book.Color * 0.6f);
			var pages = Lit("BookPages", new Color(0.97f, 0.93f, 0.82f), 0.1f, new Color(1f, 0.95f, 0.8f) * 0.3f);
			Prim(PrimitiveType.Cube, "CoverL", visual.transform, new Vector3(-0.15f, 0, 0), new Vector3(0.3f, 0.03f, 0.4f), cover, false, new Vector3(0, 0, 10));
			Prim(PrimitiveType.Cube, "CoverR", visual.transform, new Vector3(0.15f, 0, 0), new Vector3(0.3f, 0.03f, 0.4f), cover, false, new Vector3(0, 0, -10));
			Prim(PrimitiveType.Cube, "PagesL", visual.transform, new Vector3(-0.14f, 0.025f, 0), new Vector3(0.27f, 0.03f, 0.37f), pages, false, new Vector3(0, 0, 10));
			Prim(PrimitiveType.Cube, "PagesR", visual.transform, new Vector3(0.14f, 0.025f, 0), new Vector3(0.27f, 0.03f, 0.37f), pages, false, new Vector3(0, 0, -10));
			visual.AddComponent<Bobbing>();
			PointLight(visual.transform, new Vector3(0, 0.3f, 0), Color.Lerp(book.Color, Color.white, 0.5f), 3f, 1.2f);

			// The floating book always shows, so it can still be found under a lectern from an art pack
			ArtSlot(root, "prop:storybook", "Storybook lectern (the floating book always shows)", visual.transform);

			var pickup = root.AddComponent<StoryBookPickup>();
			Set(pickup, "book", book);
			Set(pickup, "visual", visual);
		}

		private static void Recruit(Transform parent, Vector3 position, string companionId, float yaw)
		{
			var companion = database.FindCompanion(companionId);

			var instance = (GameObject)PrefabUtility.InstantiatePrefab(companion.Prefab, parent);
			instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
			instance.name = $"{companion.DisplayName} (Recruit)";

			Set(instance.AddComponent<CompanionRecruit>(), "companion", companion);
		}

		private static void BounceMushroom(Transform parent, Vector3 position, float size = 1f, float bounce = 16f)
		{
			var root = Node("BounceMushroom", parent, position);
			root.layer = Ground;
			root.transform.localScale = Vector3.one * size;

			Prim(PrimitiveType.Cylinder, "Stem", root.transform, new Vector3(0, 0.6f, 0), new Vector3(0.7f, 0.6f, 0.7f), Lit("MushroomStem", new Color(0.95f, 0.92f, 0.82f)));

			var cap = Node("CapPivot", root.transform, new Vector3(0, 1.2f, 0));
			var capMesh = Prim(PrimitiveType.Sphere, "Cap", cap.transform, Vector3.zero, new Vector3(2.6f, 1f, 2.6f), Lit("BounceCap", new Color(0.92f, 0.32f, 0.42f), 0.4f));
			ReplaceWithMeshCollider(capMesh);

			for (var i = 0; i < 5; i++)
			{
				var angle = i * 72f * Mathf.Deg2Rad;
				Prim(PrimitiveType.Sphere, "Spot", cap.transform, new Vector3(Mathf.Cos(angle) * 0.7f, 0.38f, Mathf.Sin(angle) * 0.7f), new Vector3(0.35f, 0.15f, 0.35f), Lit("CapSpot", Color.white), false);
			}

			var trigger = root.AddComponent<BoxCollider>();
			trigger.isTrigger = true;
			trigger.center = new Vector3(0, 1.85f, 0);
			trigger.size = new Vector3(2.2f, 0.5f, 2.2f);

			ArtSlot(root, "prop:bounce-mushroom", "Bounce mushroom");

			var pad = root.AddComponent<BouncePad>();
			SetValue(pad, "bounceSpeed", bounce);
			Set(pad, "cap", cap.transform);
		}

		private static GameObject Platform(Transform parent, string name, Vector3 topCentre, Vector3 size, Material material)
		{
			var platform = Prim(PrimitiveType.Cube, name, parent, topCentre - Vector3.up * size.y * 0.5f, size, material);
			platform.layer = Ground;

			return platform;
		}

		private static GameObject Pillar(Transform parent, Vector3 topCentre, float radius, Material material)
		{
			var height = topCentre.y + 1f;

			var pillar = Prim(PrimitiveType.Cylinder, "Pillar", parent, new Vector3(topCentre.x, topCentre.y - height * 0.5f, topCentre.z), new Vector3(radius * 2f, height * 0.5f, radius * 2f), material);
			pillar.layer = Ground;

			return pillar;
		}

		private static void FloatingIsle(Transform parent, Vector3 topCentre, float radius)
		{
			var top = Prim(PrimitiveType.Cylinder, "IsleTop", parent, topCentre - Vector3.up * 0.75f, new Vector3(radius * 2f, 0.75f, radius * 2f), Grass);
			top.layer = Ground;

			MeshObject("IsleRoots", parent, Cone(), topCentre - Vector3.up * 1.4f, new Vector3(radius * 1.9f, radius * 1.4f, radius * 1.9f), Rock, false, new Vector3(180, 0, 0));
		}

		#endregion

		#region Areas

		private static void BuildHome()
		{
			var home = new GameObject("Home");
			home.transform.position = HomeCentre;

			var service = home.AddComponent<HomeService>();
			home.AddComponent<CompanionSpawner>();

			var plot = Node("Plot", home.transform);
			var plotComponent = plot.AddComponent<HomePlot>();

			var border = plot.AddComponent<LineRenderer>();
			border.sharedMaterial = Particle("Line", null);
			border.widthMultiplier = 0.08f;
			border.startColor = new Color(1f, 0.85f, 0.55f, 0.8f);
			border.endColor = new Color(1f, 0.85f, 0.55f, 0.8f);
			border.shadowCastingMode = ShadowCastingMode.Off;

			Set(plotComponent, "border", border);
			Set(service, "plot", plotComponent);

			SetStarterHome(service);

			BuildAlchemyBench(home.transform, new Vector3(-5.5f, 0f, -5.5f), 180f);
			BuildHearth(home.transform, new Vector3(5.5f, 0f, -5f));

			var crates = Node("Crates", world).transform;

			foreach (var position in new[] { new Vector3(-2.5f, 0f, -9.5f), new Vector3(-1.4f, 0f, -10.2f), new Vector3(2.8f, 0f, -9.8f), new Vector3(-2f, 0.9f, -9.8f) })
				Crate(crates, position);

			// A garden plot of flowers and a few trees around the meadow so it feels lived in
			for (var i = 0; i < 10; i++)
				Tree(world, Around(HomeCentre, 19f, 26f), Range(0.8f, 1.2f), random.Next(1, 4));
		}

		private static void SetStarterHome(HomeService service)
		{
			var pieces = new List<(string id, Vector3 offset, float yaw)>
			{
				("floor_timber", new Vector3(-1, 0, -1), 0), ("floor_timber", new Vector3(1, 0, -1), 0),
				("floor_timber", new Vector3(-1, 0, 1), 0), ("floor_timber", new Vector3(1, 0, 1), 0),
				("wall_timber", new Vector3(-1, 0, 2), 0), ("wall_window", new Vector3(1, 0, 2), 0),
				("wall_door", new Vector3(-1, 0, -2), 0), ("wall_window", new Vector3(1, 0, -2), 0),
				("wall_timber", new Vector3(-2, 0, -1), 90), ("wall_window", new Vector3(-2, 0, 1), 90),
				("wall_timber", new Vector3(2, 0, -1), 90), ("wall_timber", new Vector3(2, 0, 1), 90),
				("roof_thatch", new Vector3(-1, 2.5f, -1), 0), ("roof_thatch", new Vector3(1, 2.5f, -1), 0),
				("roof_thatch", new Vector3(-1, 2.5f, 1), 180), ("roof_thatch", new Vector3(1, 2.5f, 1), 180),
				("bed", new Vector3(-1.2f, 0.2f, 1f), 90), ("table", new Vector3(1f, 0.2f, 1.1f), 0),
				("chair", new Vector3(1f, 0.2f, 0.35f), 180), ("rug", new Vector3(0f, 0.2f, -0.2f), 0),
				("cat_bed", new Vector3(1.3f, 0.2f, -1.3f), 0), ("lamp", new Vector3(-1.5f, 0.2f, -1.5f), 0),
				("planter", new Vector3(-1f, 0f, 3.2f), 0), ("planter", new Vector3(1f, 0f, 3.2f), 0),
			};

			var serialized = new SerializedObject(service);
			var list = serialized.FindProperty("starterPieces");
			list.arraySize = pieces.Count;

			for (var i = 0; i < pieces.Count; i++)
			{
				var element = list.GetArrayElementAtIndex(i);
				element.FindPropertyRelative("Piece").objectReferenceValue = database.FindBuildPiece(pieces[i].id);
				element.FindPropertyRelative("Offset").vector3Value = pieces[i].offset;
				element.FindPropertyRelative("Yaw").floatValue = pieces[i].yaw;
			}

			serialized.ApplyModifiedPropertiesWithoutUndo();
		}

		private static void Crate(Transform parent, Vector3 position)
		{
			var crate = Prim(PrimitiveType.Cube, "Crate", parent, position + Vector3.up * 0.45f, Vector3.one * 0.9f, Wood, true, new Vector3(0, Range(0, 40), 0));
			crate.layer = EnvironmentLayer;

			Prim(PrimitiveType.Cube, "Band", crate.transform, Vector3.zero, new Vector3(1.02f, 0.15f, 1.02f), DarkWood, false);

			var body = crate.AddComponent<Rigidbody>();
			body.mass = 2f;

			crate.AddComponent<Movable>();

			ArtSlot(crate, "prop:crate", "Crate (levitate practice by home)");
		}

		private static void BuildAlchemyBench(Transform parent, Vector3 position, float yaw)
		{
			var root = Node("AlchemyBench", parent, position, yaw);
			root.layer = EnvironmentLayer;
			var t = root.transform;

			// Fire pit and cauldron
			for (var i = 0; i < 8; i++)
			{
				var angle = i * 45f * Mathf.Deg2Rad;
				Prim(PrimitiveType.Sphere, "PitStone", t, new Vector3(Mathf.Cos(angle) * 0.75f, 0.1f, Mathf.Sin(angle) * 0.75f), new Vector3(0.35f, 0.22f, 0.3f), Rock, false);
			}

			var flames = Node("Flames", t, new Vector3(0, 0.05f, 0));
			MeshObject("Flame", flames.transform, Cone(), Vector3.zero, new Vector3(0.6f, 0.55f, 0.6f), Flame);
			MeshObject("Flame", flames.transform, Cone(), new Vector3(0.15f, 0, 0.1f), new Vector3(0.35f, 0.4f, 0.35f), Flame);

			var fireLight = PointLight(t, new Vector3(0, 0.5f, 0), new Color(1f, 0.6f, 0.3f), 6f, 1f);

			Prim(PrimitiveType.Sphere, "Belly", t, new Vector3(0, 0.62f, 0), new Vector3(1.2f, 0.75f, 1.2f), Iron);
			Prim(PrimitiveType.Cylinder, "Neck", t, new Vector3(0, 0.88f, 0), new Vector3(0.98f, 0.08f, 0.98f), Iron, false);
			Prim(PrimitiveType.Cylinder, "Rim", t, new Vector3(0, 0.97f, 0), new Vector3(1.08f, 0.03f, 1.08f), Lit("IronRim", new Color(0.3f, 0.28f, 0.32f), 0.6f), false);

			foreach (var side in new[] { -1f, 1f })
				Prim(PrimitiveType.Cube, "Leg", t, new Vector3(side * 0.45f, 0.2f, 0), new Vector3(0.08f, 0.4f, 0.08f), Iron, false);

			var liquid = Prim(PrimitiveType.Cylinder, "Brew", t, new Vector3(0, 1.005f, 0), new Vector3(0.92f, 0.01f, 0.92f), Lit("Brew", new Color(0.55f, 0.8f, 1f), 0.9f, new Color(0.2f, 0.3f, 0.4f)), false);

			// Work table with mortar and pestle, and a shelf of jars behind
			Prim(PrimitiveType.Cube, "Table", t, new Vector3(1.55f, 0.78f, 0f), new Vector3(1.3f, 0.08f, 0.8f), Wood);

			foreach (var x in new[] { 1.0f, 2.1f })
			foreach (var z in new[] { -0.32f, 0.32f })
				Prim(PrimitiveType.Cube, "Leg", t, new Vector3(x, 0.37f, z), new Vector3(0.08f, 0.74f, 0.08f), DarkWood, false);

			Prim(PrimitiveType.Cylinder, "Mortar", t, new Vector3(1.35f, 0.92f, 0.05f), new Vector3(0.36f, 0.12f, 0.36f), StoneMat(), false);

			var contents = Prim(PrimitiveType.Sphere, "MortarContents", t, new Vector3(1.35f, 1.02f, 0.05f), Vector3.one * 0.3f, Lit("Herb", new Color(0.6f, 0.5f, 0.9f)), false);

			var pestle = Node("Pestle", t, new Vector3(1.45f, 1.0f, 0.05f));
			Prim(PrimitiveType.Cylinder, "Pestle", pestle.transform, new Vector3(0, 0.18f, 0), new Vector3(0.07f, 0.2f, 0.07f), StoneMat(), false);

			Prim(PrimitiveType.Cube, "Shelf", t, new Vector3(0.8f, 1.6f, -0.9f), new Vector3(2.6f, 0.06f, 0.35f), Wood, false);

			foreach (var x in new[] { -0.6f, -0.2f })
				Prim(PrimitiveType.Cube, "Post", t, new Vector3(x + 0.1f, 0.85f, -0.9f), new Vector3(0.08f, 1.7f, 0.08f), DarkWood);

			Prim(PrimitiveType.Cube, "Post", t, new Vector3(2.05f, 0.85f, -0.9f), new Vector3(0.08f, 1.7f, 0.08f), DarkWood);

			var jarColours = database.Items.Where(item => item.Category == Data.Enums.ItemCategory.Ingredient).Select(item => item.Color).ToList();

			for (var i = 0; i < 7; i++)
				Prim(PrimitiveType.Cylinder, "Jar", t, new Vector3(-0.35f + i * 0.33f, 1.75f, -0.9f), new Vector3(0.18f, 0.12f, 0.18f),
					Lit($"Jar{i}", jarColours[i % jarColours.Count], 0.8f, jarColours[i % jarColours.Count] * 0.3f), false);

			// Little awning to frame it
			Prim(PrimitiveType.Cube, "Awning", t, new Vector3(0.8f, 2.6f, -0.3f), new Vector3(3.4f, 0.08f, 1.8f), Lit("Thatch", new Color(0.85f, 0.7f, 0.4f)), false, new Vector3(-15f, 0, 0));
			Prim(PrimitiveType.Cube, "AwningPost", t, new Vector3(-0.85f, 1.3f, -1f), new Vector3(0.1f, 2.6f, 0.1f), DarkWood);
			Prim(PrimitiveType.Cube, "AwningPost", t, new Vector3(2.45f, 1.3f, -1f), new Vector3(0.1f, 2.6f, 0.1f), DarkWood);

			var viewpoint = Node("Viewpoint", t, new Vector3(0.6f, 2.7f, 2.3f));
			viewpoint.transform.LookAt(t.TransformPoint(new Vector3(0.55f, 0.7f, -0.1f)));

			var zone = Node("Interact", t, new Vector3(0.6f, 0.8f, 0.8f));
			zone.layer = Interactable;
			Trigger(zone, 1.6f);

			var station = root.AddComponent<AlchemyStation>();
			Set(station, "viewpoint", viewpoint.transform);
			Set(station, "liquid", liquid.GetComponent<Renderer>());
			Set(station, "liquidSurface", liquid.transform);
			Set(station, "fireLight", fireLight);
			Set(station, "flames", flames.transform);
			Set(station, "mortarContents", contents.GetComponent<Renderer>());
			Set(station, "pestle", pestle.transform);
		}

		private static Material StoneMat() => Lit("Stone", new Color(0.62f, 0.62f, 0.66f));

		private static void BuildHearth(Transform parent, Vector3 position)
		{
			var root = Node("Hearth", parent, position);
			root.layer = EnvironmentLayer;
			var t = root.transform;

			Prim(PrimitiveType.Cylinder, "Hearthstone", t, new Vector3(0, 0.08f, 0), new Vector3(2.2f, 0.08f, 2.2f), ShrineStone);

			for (var i = 0; i < 10; i++)
			{
				var angle = i * 36f * Mathf.Deg2Rad;
				Prim(PrimitiveType.Sphere, "Stone", t, new Vector3(Mathf.Cos(angle) * 0.85f, 0.22f, Mathf.Sin(angle) * 0.85f), new Vector3(0.4f, 0.3f, 0.35f), Rock, false);
			}

			Prim(PrimitiveType.Cylinder, "Log", t, new Vector3(0, 0.25f, 0), new Vector3(0.2f, 0.45f, 0.2f), Trunk, false, new Vector3(0, 30, 80));
			Prim(PrimitiveType.Cylinder, "Log", t, new Vector3(0, 0.28f, 0), new Vector3(0.2f, 0.45f, 0.2f), Trunk, false, new Vector3(0, -40, 80));

			var flames = Node("Flames", t, new Vector3(0, 0.2f, 0));
			MeshObject("Flame", flames.transform, Cone(), Vector3.zero, new Vector3(0.7f, 0.9f, 0.7f), Flame);
			MeshObject("Flame", flames.transform, Cone(), new Vector3(0.12f, 0, -0.1f), new Vector3(0.4f, 0.6f, 0.4f), Flame);

			var light = PointLight(t, new Vector3(0, 0.9f, 0), new Color(1f, 0.62f, 0.3f), 9f, 2.5f);

			var fire = root.AddComponent<Campfire>();
			Set(fire, "fireLight", light);
			Set(fire, "flames", flames.transform);

			// Seats around the fire
			foreach (var angle in new[] { 200f, 340f })
			{
				var rad = angle * Mathf.Deg2Rad;
				Prim(PrimitiveType.Cylinder, "Stump", t, new Vector3(Mathf.Cos(rad) * 2f, 0.25f, Mathf.Sin(rad) * 2f), new Vector3(0.6f, 0.25f, 0.6f), Trunk);
			}

			var zone = Node("Interact", t, new Vector3(0, 0.8f, 0));
			zone.layer = Interactable;
			Trigger(zone, 1.7f);

			root.AddComponent<Hearth>();
		}

		private static void BuildForest()
		{
			var forest = Node("Forest", world).transform;

			for (var i = 0; i < 46; i++)
			{
				var position = Around(ForestCentre, 4f, 24f);
				Tree(forest, position, Range(0.9f, 1.5f), random.NextDouble() < 0.5 ? 0 : random.Next(1, 4));
			}

			var lavender = ContentBuilder.Item("lavender").Color;
			var nettle = ContentBuilder.Item("nettle").Color;

			for (var i = 0; i < 7; i++)
				Gatherable(forest, "Lavender", Around(ForestCentre, 0f, 9f), "lavender", 1, 90f, visual => Flowers(visual, lavender, 6));

			for (var i = 0; i < 6; i++)
				Gatherable(forest, "Nettle", Around(ForestCentre, 8f, 20f), "nettle", 1, 90f, visual => Leaves(visual, nettle));

			for (var i = 0; i < 6; i++)
				Gatherable(forest, "BerryBush", Around(ForestCentre, 5f, 18f), "wild_berry", 2, 120f, BerryBush);

			for (var i = 0; i < 6; i++)
				Gatherable(forest, "FallenLog", Around(ForestCentre, 6f, 22f), "timber", 2, 120f, FallenLog, false, "Collect");

			for (var i = 0; i < 5; i++)
				Glowcaps(forest, Around(ForestCentre, 10f, 22f));

			Recruit(forest, OnGround(ForestCentre.x + 4f, ForestCentre.z + 6f), "bramble", 200f);

			BounceMushroom(forest, OnGround(22f, 26f));
			BounceMushroom(forest, OnGround(40f, 30f), 0.8f, 13f);

			// A clearing campfire stump circle
			var camp = Node("Clearing", forest, OnGround(ForestCentre.x - 2f, ForestCentre.z - 3f));
			for (var i = 0; i < 5; i++)
			{
				var angle = i * 72f * Mathf.Deg2Rad;
				Prim(PrimitiveType.Cylinder, "Stump", camp.transform, new Vector3(Mathf.Cos(angle) * 2.5f, 0.3f, Mathf.Sin(angle) * 2.5f), new Vector3(0.7f, 0.3f, 0.7f), Trunk).layer = EnvironmentLayer;
			}
		}

		private static void Leaves(Transform visual, Color colour)
		{
			var material = Lit("NettleLeaf", colour);

			for (var i = 0; i < 6; i++)
				Prim(PrimitiveType.Sphere, "Leaf", visual, new Vector3(Range(-0.3f, 0.3f), Range(0.15f, 0.45f), Range(-0.3f, 0.3f)), new Vector3(0.25f, 0.06f, 0.4f), material, false,
					new Vector3(Range(-30, 30), Range(0, 360), 0));
		}

		private static void BerryBush(Transform visual)
		{
			Prim(PrimitiveType.Sphere, "Bush", visual, new Vector3(0, 0.45f, 0), new Vector3(1.2f, 0.9f, 1.2f), Leaf(1), false);

			var berry = Lit("Berry", ContentBuilder.Item("wild_berry").Color, 0.7f);

			for (var i = 0; i < 9; i++)
			{
				var direction = new Vector3(Range(-1f, 1f), Range(0.1f, 1f), Range(-1f, 1f)).normalized;
				Prim(PrimitiveType.Sphere, "Berry", visual, new Vector3(0, 0.45f, 0) + Vector3.Scale(direction, new Vector3(0.6f, 0.45f, 0.6f)), Vector3.one * 0.12f, berry, false);
			}
		}

		private static void FallenLog(Transform visual)
		{
			Prim(PrimitiveType.Cylinder, "Log", visual, new Vector3(0, 0.25f, 0), new Vector3(0.5f, 1f, 0.5f), Trunk, false, new Vector3(0, 0, 90));
			Prim(PrimitiveType.Cylinder, "Ring", visual, new Vector3(1.0f, 0.25f, 0), new Vector3(0.42f, 0.01f, 0.42f), Lit("WoodRing", new Color(0.85f, 0.7f, 0.5f)), false, new Vector3(0, 0, 90));
			Prim(PrimitiveType.Sphere, "Moss", visual, new Vector3(-0.2f, 0.48f, 0), new Vector3(0.6f, 0.15f, 0.4f), Leaf(0), false);
		}

		private static void BuildPond()
		{
			var pond = Node("Pond", world).transform;

			var water = Prim(PrimitiveType.Cylinder, "PondWater", pond, PondCentre + new Vector3(0, 0.05f, 0), new Vector3(26f, 0.02f, 26f), Water, false);
			water.layer = 4;

			Prim(PrimitiveType.Cylinder, "PondBed", pond, PondCentre + new Vector3(0, 0.01f, 0), new Vector3(26.5f, 0.01f, 26.5f), Lit("PondBed", new Color(0.36f, 0.5f, 0.42f)), false);

			var pad = Lit("LilyPad", new Color(0.4f, 0.72f, 0.35f));

			for (var i = 0; i < 14; i++)
			{
				var position = PondCentre + new Vector3(Range(-10f, 10f), 0.08f, Range(-10f, 10f));

				if (Vector3.Distance(position, PondCentre) > 11f || Vector3.Distance(position, PondCentre + new Vector3(-2, 0, 2)) < 4f)
					continue;

				Prim(PrimitiveType.Cylinder, "LilyPad", pond, position, new Vector3(Range(1.2f, 2f), 0.03f, Range(1.2f, 2f)), pad).layer = Ground;
			}

			var pipPad = Prim(PrimitiveType.Cylinder, "PipsPad", pond, PondCentre + new Vector3(6f, 0.08f, 6f), new Vector3(2.2f, 0.03f, 2.2f), pad);
			pipPad.layer = Ground;

			Recruit(pond, PondCentre + new Vector3(6f, 0.12f, 6f), "pip", 220f);

			// Stone column in the middle with stumps spiralling up to the Vita shrine
			var columnCentre = PondCentre + new Vector3(-2f, 0, 2f);

			Pillar(pond, columnCentre + Vector3.up * 6f, 2.3f, Rock);

			var angles = new[] { 200f, 248f, 296f, 344f, 32f };

			for (var i = 0; i < angles.Length; i++)
			{
				var rad = angles[i] * Mathf.Deg2Rad;
				Pillar(pond, columnCentre + new Vector3(Mathf.Cos(rad) * 4.6f, 1f + i * 1.05f, Mathf.Sin(rad) * 4.6f), 0.85f, Lit("Stump", new Color(0.5f, 0.38f, 0.28f)));
			}

			Shrine(pond, columnCentre + Vector3.up * 6f, "vita", 120f);

			// Spring, reeds and clay round the edge
			BuildSpring(pond, OnGround(PondCentre.x + 12.5f, PondCentre.z - 4f));

			var reed = ContentBuilder.Item("reed").Color;

			for (var i = 0; i < 8; i++)
			{
				var angle = Range(0, Mathf.PI * 2f);
				var position = OnGround(PondCentre.x + Mathf.Cos(angle) * 13.5f, PondCentre.z + Mathf.Sin(angle) * 13.5f);
				Gatherable(pond, "Reeds", position, "reed", 2, 100f, visual => Reeds(visual, reed), false, "Cut");
			}

			for (var i = 0; i < 4; i++)
			{
				var angle = Range(Mathf.PI * 0.8f, Mathf.PI * 1.3f);
				var position = OnGround(PondCentre.x + Mathf.Cos(angle) * 14.5f, PondCentre.z + Mathf.Sin(angle) * 14.5f);
				Gatherable(pond, "ClayBank", position, "clay", 1, 120f, ClayLump, false, "Dig");
			}

			for (var i = 0; i < 3; i++)
				Glowcaps(pond, Around(PondCentre, 15f, 18f));

			Tree(pond, OnGround(PondCentre.x - 15f, PondCentre.z + 8f), 1.4f, 2);
			Tree(pond, OnGround(PondCentre.x + 10f, PondCentre.z + 13f), 1.2f, 1);
		}

		private static void BuildSpring(Transform parent, Vector3 position)
		{
			Gatherable(parent, "Spring", position, "spring_water", 1, 0f, visual =>
			{
				for (var i = 0; i < 7; i++)
				{
					var angle = i * (360f / 7f) * Mathf.Deg2Rad;
					Prim(PrimitiveType.Sphere, "Rock", visual, new Vector3(Mathf.Cos(angle) * 0.7f, 0.15f, Mathf.Sin(angle) * 0.7f), new Vector3(0.45f, 0.35f, 0.4f), Rock, false);
				}

				Prim(PrimitiveType.Cylinder, "Water", visual, new Vector3(0, 0.18f, 0), new Vector3(1.1f, 0.02f, 1.1f), Lit("SpringWater", new Color(0.55f, 0.85f, 1f), 0.95f, new Color(0.3f, 0.6f, 0.9f) * 0.5f), false);
				PointLight(visual, new Vector3(0, 0.5f, 0), new Color(0.6f, 0.85f, 1f), 3f, 0.8f);
			}, false, "Fill a flask with");
		}

		private static void Reeds(Transform visual, Color colour)
		{
			var stalk = Lit("ReedStalk", colour);
			var head = Lit("ReedHead", new Color(0.5f, 0.35f, 0.22f));

			for (var i = 0; i < 7; i++)
			{
				var offset = new Vector3(Range(-0.35f, 0.35f), 0, Range(-0.35f, 0.35f));
				var height = Range(0.9f, 1.4f);

				Prim(PrimitiveType.Cylinder, "Stalk", visual, offset + Vector3.up * height * 0.5f, new Vector3(0.04f, height * 0.5f, 0.04f), stalk, false);
				Prim(PrimitiveType.Cylinder, "Head", visual, offset + Vector3.up * height, new Vector3(0.08f, 0.12f, 0.08f), head, false);
			}
		}

		private static void ClayLump(Transform visual)
		{
			var clay = Lit("Clay", new Color(0.78f, 0.46f, 0.35f));

			Prim(PrimitiveType.Sphere, "Clay", visual, new Vector3(0, 0.1f, 0), new Vector3(0.9f, 0.35f, 0.7f), clay, false);
			Prim(PrimitiveType.Sphere, "Clay", visual, new Vector3(0.3f, 0.12f, 0.2f), new Vector3(0.5f, 0.3f, 0.5f), clay, false);
		}

		private static void BuildHillLife()
		{
			var hillLife = Node("HillLife", world).transform;

			for (var i = 0; i < 8; i++)
			{
				var (centre, radius, _, _) = hills[random.Next(0, 5)];

				var position = Around(centre, radius * 0.2f, radius * 0.9f);

				Gatherable(hillLife, "Boulder", position, "stone", 2, 120f, visual =>
				{
					Prim(PrimitiveType.Sphere, "Rock", visual, new Vector3(0, 0.35f, 0), new Vector3(1f, 0.75f, 0.9f), Rock, false, new Vector3(0, Range(0, 360), 0));
					Prim(PrimitiveType.Sphere, "Rock", visual, new Vector3(0.45f, 0.2f, 0.2f), new Vector3(0.5f, 0.4f, 0.5f), Rock, false);
				}, false, "Chip");
			}

			var dew = ContentBuilder.Item("moon_dew").Color;

			foreach (var index in new[] { 0, 1, 1, 3, 4, 4 })
			{
				var (centre, radius, _, _) = hills[index];
				var position = Around(centre, 0f, radius * 0.35f);

				Gatherable(hillLife, "Silverbells", position, "moon_dew", 1, 90f, visual =>
				{
					Flowers(visual, new Color(0.88f, 0.86f, 1f), 7, 0.45f);
					PointLight(visual, new Vector3(0, 0.4f, 0), dew, 2.5f, 0.7f);
				}, false, "Shake the dew from", true);
			}

			var moonpetal = ContentBuilder.Item("moonpetal").Color;

			for (var i = 0; i < 2; i++)
				Gatherable(hillLife, "Moonpetal", Around(hills[1].centre, 0f, 2.5f), "moonpetal", 1, 150f, visual => Flowers(visual, moonpetal, 4, 0.3f), false, "Gather", true);

			for (var i = 0; i < 6; i++)
				Boulder(hillLife, Around(HillsCentre, 8f, 26f), Range(1.5f, 3.5f));
		}

		private static void BuildCave()
		{
			var cave = Node("CrystalCave", world).transform;

			var c = CaveCentre;

			// A tunnel of rock opening west towards home
			Platform(cave, "WallSouth", c + new Vector3(0, 6f, -4.5f), new Vector3(18f, 6f, 2.4f), RockDark);
			Platform(cave, "WallNorth", c + new Vector3(0, 6f, 4.5f), new Vector3(18f, 6f, 2.4f), RockDark);
			Platform(cave, "WallBack", c + new Vector3(9f, 6f, 0), new Vector3(2.4f, 6f, 11.4f), RockDark);
			Platform(cave, "Roof", c + new Vector3(0.5f, 7.5f, 0), new Vector3(19f, 1.6f, 11.4f), RockDark);
			Platform(cave, "Floor", c + new Vector3(0.5f, 0.06f, 0), new Vector3(19f, 0.2f, 7f), Lit("CaveFloor", new Color(0.36f, 0.33f, 0.4f)));

			for (var i = 0; i < 10; i++)
				Boulder(cave, c + new Vector3(Range(-9f, 10f), Range(2f, 7f), Range(-1f, 1f) > 0 ? Range(5f, 7.5f) : Range(-7.5f, -5f)), Range(3f, 5f));

			for (var i = 0; i < 4; i++)
				Boulder(cave, c + new Vector3(Range(-6f, 10f), 8.5f, Range(-4f, 4f)), Range(4f, 6f));

			// Crystals glittering inside
			for (var i = 0; i < 10; i++)
			{
				var position = c + new Vector3(Range(-6f, 7.5f), 0f, Range(-1f, 1f) > 0 ? Range(2.2f, 3.2f) : Range(-3.2f, -2.2f));

				var cluster = Node("Crystals", cave, position);

				for (var j = 0; j < 3; j++)
					Prim(PrimitiveType.Cube, "Crystal", cluster.transform, new Vector3(Range(-0.3f, 0.3f), 0.4f, Range(-0.3f, 0.3f)), new Vector3(0.25f, Range(0.6f, 1.3f), 0.25f), Crystal, false,
						new Vector3(Range(-25, 25), Range(0, 90), Range(-25, 25)));

				if (i % 3 == 0)
					PointLight(cluster.transform, new Vector3(0, 0.8f, 0), new Color(0.5f, 0.85f, 1f), 7f, 2f);
			}

			for (var i = 0; i < 4; i++)
				Gatherable(cave, "CrystalShard", c + new Vector3(-4f + i * 3f, 0f, i % 2 == 0 ? 1.8f : -1.8f), "crystal", 1, 150f, visual =>
				{
					Prim(PrimitiveType.Cube, "Shard", visual, new Vector3(0, 0.35f, 0), new Vector3(0.22f, 0.7f, 0.22f), Crystal, false, new Vector3(10, 30, 15));
					Prim(PrimitiveType.Cube, "Shard", visual, new Vector3(0.15f, 0.25f, 0.1f), new Vector3(0.15f, 0.45f, 0.15f), Crystal, false, new Vector3(-20, 0, -10));
				}, false, "Prise loose a");

			var moss = Lit("EmberMoss", ContentBuilder.Item("ember_moss").Color, 0.2f, new Color(1f, 0.4f, 0.1f) * 1.2f);

			for (var i = 0; i < 3; i++)
				Gatherable(cave, "EmberMoss", c + new Vector3(-2f + i * 3.5f, 0f, i % 2 == 0 ? -2.6f : 2.6f), "ember_moss", 1, 120f, visual =>
				{
					Prim(PrimitiveType.Sphere, "Moss", visual, new Vector3(0, 0.15f, 0), new Vector3(0.8f, 0.3f, 0.6f), moss, false);
					PointLight(visual, new Vector3(0, 0.4f, 0), new Color(1f, 0.5f, 0.2f), 2.5f, 0.8f);
				});

			Glowcaps(cave, c + new Vector3(2f, 0, 0.5f));

			Shrine(cave, c + new Vector3(6.5f, 0, 0), "nihil", -90f);

			Recruit(cave, c + new Vector3(4.5f, 0, 2.2f), "ember", -120f);
		}

		private static void BuildSpire()
		{
			var spire = Node("SkySpire", world).transform;

			var c = SpireCentre;

			var column = Prim(PrimitiveType.Cylinder, "Spire", spire, c + Vector3.up * (SpireTop * 0.5f), new Vector3(5f, SpireTop * 0.5f, 5f), Rock);
			column.layer = Ground;

			var stone = Lit("PlatformStone", new Color(0.7f, 0.66f, 0.72f));
			var moss = Lit("PlatformMoss", new Color(0.5f, 0.72f, 0.42f));

			const int steps = 16;

			var positions = new List<Vector3>();

			for (var i = 0; i < steps; i++)
			{
				var angle = (180f + i * 30f) * Mathf.Deg2Rad;
				positions.Add(c + new Vector3(Mathf.Cos(angle) * 7f, 1f + i * 1.3f, Mathf.Sin(angle) * 7f));
			}

			for (var i = 0; i < steps; i++)
			{
				// Steps 6 and 7 are replaced by a platform gliding between them
				if (i is 6 or 7)
					continue;

				var platform = Platform(spire, $"Step{i}", positions[i], new Vector3(2.8f, 0.5f, 2.8f), i % 3 == 0 ? moss : stone);
				platform.transform.rotation = Quaternion.Euler(0, -(180f + i * 30f), 0);
			}

			var mover = Platform(spire, "MovingStep", positions[6], new Vector3(2.6f, 0.5f, 2.6f), Lit("MovingStone", new Color(0.8f, 0.7f, 0.9f), 0.3f, new Color(0.5f, 0.3f, 0.8f) * 0.4f));
			mover.AddComponent<Rigidbody>();
			var moving = mover.AddComponent<MovingPlatform>();
			SetVector(moving, "offset", positions[7] - positions[6]);
			SetValue(moving, "period", 5f);

			BounceMushroom(spire, positions[10] + Vector3.down * 0.05f, 0.55f, 14f);

			foreach (var index in new[] { 3, 9, 13 })
				Gatherable(spire, "FeatherDown", positions[index] + Vector3.up * 0.4f, "feather_down", 1, 120f, Feather, true);

			// The summit
			var top = Prim(PrimitiveType.Cylinder, "Summit", spire, c + Vector3.up * (SpireTop - 0.4f), new Vector3(9f, 0.4f, 9f), moss);
			top.layer = Ground;

			Shrine(spire, c + new Vector3(1.5f, SpireTop, 0.5f), "motus", -60f);
			Scroll(spire, c + new Vector3(-2f, SpireTop, -1.5f), "featherfall");

			// Updraft from beside the summit up to the sky island
			var updraft = Node("Updraft", spire, new Vector3(c.x - 2f, SpireTop - 1f, c.z + 11f));
			var box = updraft.AddComponent<BoxCollider>();
			box.isTrigger = true;
			box.center = new Vector3(0, 9f, 0);
			box.size = new Vector3(5f, 18f, 5f);
			updraft.AddComponent<Updraft>();

			Prim(PrimitiveType.Cylinder, "Swirl", updraft.transform, Vector3.zero, new Vector3(4.5f, 0.05f, 4.5f), Lit("Swirl", new Color(0.8f, 0.95f, 1f), 0.5f, new Color(0.6f, 0.85f, 1f) * 1.2f), false);
		}

		private static void Feather(Transform visual)
		{
			var feather = Lit("Feather", new Color(1f, 0.97f, 0.9f), 0.2f, new Color(1f, 0.95f, 0.85f) * 0.4f);

			Prim(PrimitiveType.Sphere, "Feather", visual, new Vector3(0, 0.3f, 0), new Vector3(0.12f, 0.45f, 0.06f), feather, false, new Vector3(0, 0, 25));
			visual.gameObject.AddComponent<Bobbing>();
		}

		private static void BuildSkyIsle()
		{
			var isle = Node("SkyIsle", world).transform;

			FloatingIsle(isle, SkyIsleCentre, 7f);

			Shrine(isle, SkyIsleCentre + new Vector3(2.5f, 0, 2f), "lux", 200f);

			// Hoot's nest on a stump
			var nest = Node("Nest", isle, SkyIsleCentre + new Vector3(-3f, 0, 1f));
			Prim(PrimitiveType.Cylinder, "Stump", nest.transform, new Vector3(0, 0.6f, 0), new Vector3(1.2f, 0.6f, 1.2f), Trunk).layer = Ground;
			Prim(PrimitiveType.Cylinder, "Nest", nest.transform, new Vector3(0, 1.3f, 0), new Vector3(1.4f, 0.12f, 1.4f), Lit("Thatch", new Color(0.85f, 0.7f, 0.4f))).layer = Ground;

			Recruit(isle, SkyIsleCentre + new Vector3(-3f, 1.42f, 1f), "hoot", 160f);

			Tree(isle, SkyIsleCentre + new Vector3(-1f, 0, -4.5f), 0.9f, 2);

			for (var i = 0; i < 4; i++)
				Gatherable(isle, "FeatherDown", SkyIsleCentre + new Vector3(Range(-4f, 4f), 0.4f, Range(-4f, 4f)), "feather_down", 1, 120f, Feather, true);

			var moonpetal = ContentBuilder.Item("moonpetal").Color;

			for (var i = 0; i < 3; i++)
				Gatherable(isle, "Moonpetal", SkyIsleCentre + new Vector3(Range(-4f, 4f), 0f, Range(-4f, 4f)), "moonpetal", 1, 150f, visual => Flowers(visual, moonpetal, 4, 0.3f), false, "Gather", true);
		}

		private static void BuildSecretIsle()
		{
			var secret = Node("SecretIsle", world).transform;

			var start = OnGround(hills[1].centre.x, hills[1].centre.z - 4f);

			FloatingIsle(secret, SecretIsleCentre, 5f);

			const int stones = 6;

			for (var i = 0; i < stones; i++)
			{
				var t = (i + 1f) / (stones + 1f);
				var position = Vector3.Lerp(start, SecretIsleCentre, t) + Vector3.up * 0.3f;

				var stone = Platform(secret, $"HiddenStone{i}", position, new Vector3(2.2f, 0.4f, 2.2f), HiddenStone);
				stone.AddComponent<HiddenPath>();
			}

			// A faint shimmer marks where the path begins
			var hint = Node("Shimmer", secret, start + Vector3.up * 0.5f);
			PointLight(hint.transform, Vector3.zero, new Color(0.5f, 1f, 0.9f), 4f, 1.2f);

			var moonpetal = ContentBuilder.Item("moonpetal").Color;

			for (var i = 0; i < 4; i++)
				Gatherable(secret, "Moonpetal", SecretIsleCentre + new Vector3(Range(-3f, 3f), 0f, Range(-3f, 3f)), "moonpetal", 1, 150f, visual => Flowers(visual, moonpetal, 4, 0.3f), false, "Gather", true);

			for (var i = 0; i < 2; i++)
				Gatherable(secret, "CrystalShard", SecretIsleCentre + new Vector3(Range(-3f, 3f), 0f, Range(-3f, 3f)), "crystal", 2, 200f, visual =>
					Prim(PrimitiveType.Cube, "Shard", visual, new Vector3(0, 0.4f, 0), new Vector3(0.25f, 0.8f, 0.25f), Crystal, false, new Vector3(10, 30, 15)), false, "Prise loose a");

			Tree(secret, SecretIsleCentre + new Vector3(1.5f, 0, -2f), 0.8f, 3);
		}

		private static void BuildPaths()
		{
			var paths = Node("Paths", world).transform;

			Path(paths, SpawnPoint, new Vector3(0, 0, -2.5f));
			Path(paths, new Vector3(0, 0, -6f), ForestCentre + new Vector3(-8f, 0, -10f));
			Path(paths, new Vector3(-4f, 0, -4f), PondCentre + new Vector3(13f, 0, -5f));
			Path(paths, new Vector3(0, 0, -16f), new Vector3(-2f, 0, -28f));
			Path(paths, new Vector3(4f, 0, -12f), CaveCentre + new Vector3(-10f, 0, 0f));
			Path(paths, new Vector3(6f, 0, -6f), SpireCentre + new Vector3(-8f, 0, 0f));
		}

		private static void Path(Transform parent, Vector3 from, Vector3 to)
		{
			const float segmentLength = 3f;

			var distance = Vector3.Distance(new Vector3(from.x, 0, from.z), new Vector3(to.x, 0, to.z));

			var segments = Mathf.CeilToInt(distance / segmentLength);

			for (var i = 0; i < segments; i++)
			{
				var a = Vector3.Lerp(from, to, i / (float)segments);
				var b = Vector3.Lerp(from, to, (i + 1f) / segments);

				var mid = (a + b) * 0.5f;
				mid.y = GroundY(mid.x, mid.z) + 0.02f;

				var wobble = Mathf.Sin(i * 0.9f) * 0.6f;
				var direction = (b - a).normalized;
				var side = Vector3.Cross(Vector3.up, direction);

				var stone = Prim(PrimitiveType.Cylinder, "PathStone", parent, mid + side * wobble, new Vector3(Range(1.6f, 2.2f), 0.02f, Range(1.6f, 2.2f)), PathMat, false);
				stone.layer = Ground;
			}
		}

		private static void BuildScatter()
		{
			var scatter = Node("Scatter", world).transform;

			var colours = new[] { new Color(1f, 0.85f, 0.4f), new Color(1f, 0.6f, 0.7f), new Color(0.75f, 0.65f, 1f), Color.white };

			for (var i = 0; i < 140; i++)
			{
				var position = OnGround(Range(-75f, 75f), Range(-75f, 75f));

				if (new Vector2(position.x, position.z).magnitude > 78f || Vector3.Distance(position, HomeCentre) < 9f || Vector3.Distance(position, PondCentre) < 14f)
					continue;

				var flowers = Node("Wildflowers", scatter, position);
				Flowers(flowers.transform, colours[random.Next(colours.Length)], random.Next(3, 7), 0.25f);
			}

			for (var i = 0; i < 40; i++)
			{
				var position = OnGround(Range(-75f, 75f), Range(-75f, 75f));

				var radius = new Vector2(position.x, position.z).magnitude;

				var nearArea = Vector3.Distance(position, HomeCentre) < 24f || Vector3.Distance(position, PondCentre) < 18f
				               || Vector3.Distance(position, SpireCentre) < 12f || Vector3.Distance(position, CaveCentre) < 14f
				               || Vector3.Distance(position, ForestCentre) < 26f;

				if (radius > 76f || nearArea)
					continue;

				if (random.NextDouble() < 0.6)
					Tree(scatter, position, Range(0.8f, 1.4f), random.Next(0, 4));
				else
					Boulder(scatter, position, Range(1f, 2.5f));
			}

			for (var i = 0; i < 5; i++)
				Gatherable(scatter, "FallenLog", Around(HomeCentre, 14f, 30f), "timber", 2, 120f, FallenLog, false, "Collect");

			for (var i = 0; i < 4; i++)
				Gatherable(scatter, "Boulder", Around(HomeCentre, 14f, 30f), "stone", 1, 120f, visual =>
					Prim(PrimitiveType.Sphere, "Rock", visual, new Vector3(0, 0.3f, 0), new Vector3(0.8f, 0.6f, 0.7f), Rock, false), false, "Chip");
		}

		#endregion

		#region Tidewater and Starfall

		private static Material Seashell => Lit("Seashell", new Color(0.98f, 0.82f, 0.78f), 0.6f);
		private static Material Kelp => Lit("KelpLeaf", new Color(0.3f, 0.58f, 0.38f));
		private static Material StarGlow => Lit("StardustGlow", new Color(1f, 0.93f, 0.65f), 0.6f, new Color(1f, 0.85f, 0.45f) * 2f);

		// Rocky stacks rising out of the sea between the beaches: places for a broom to touch down and recharge
		private static void BuildSeaCrossing()
		{
			var crossing = Node("SeaStacks", world).transform;

			var stacks = new[] { new Vector3(4f, 1.8f, 95f), new Vector3(-2f, 2.4f, 103f), new Vector3(5f, 2.0f, 110f) };

			foreach (var stack in stacks)
			{
				Pillar(crossing, stack, 2.2f, Rock);
				Prim(PrimitiveType.Cylinder, "Moss", crossing, stack + Vector3.up * 0.02f, new Vector3(3.6f, 0.02f, 3.6f), Lit("PlatformMoss", new Color(0.5f, 0.72f, 0.42f)), false);
			}

			Signpost(crossing, OnGround(1f, 83f), "Tidewater Isle\nacross the sea stacks", 0f);
		}

		private static void Signpost(Transform parent, Vector3 position, string text, float yaw)
		{
			var post = Node("Signpost", parent, position, yaw);
			post.layer = EnvironmentLayer;

			Prim(PrimitiveType.Cube, "Post", post.transform, new Vector3(0, 1f, 0), new Vector3(0.15f, 2f, 0.15f), Wood);
			Prim(PrimitiveType.Cube, "Board", post.transform, new Vector3(0, 1.75f, 0.1f), new Vector3(1.9f, 0.7f, 0.08f), Lit("SignBoard", new Color(0.78f, 0.6f, 0.4f)), false);

			// Readable from the approach (south) side
			var label = new GameObject("Text");
			label.transform.SetParent(post.transform, false);
			label.transform.localPosition = new Vector3(0, 1.75f, 0.04f);
			label.transform.localRotation = Quaternion.identity;

			var mesh = label.AddComponent<TextMesh>();
			mesh.text = text;
			mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			mesh.fontSize = 48;
			mesh.characterSize = 0.035f;
			mesh.anchor = TextAnchor.MiddleCenter;
			mesh.alignment = TextAlignment.Center;
			mesh.color = new Color(0.25f, 0.15f, 0.12f);
			label.GetComponent<MeshRenderer>().sharedMaterial = mesh.font.material;

			// The sign keeps its words over any art
			ArtSlot(post, "prop:signpost", "Signpost", label.transform);
		}

		private static void BuildTidewater()
		{
			var isle = Node("TidewaterIsle", world).transform;

			var t = TidewaterCentre;

			var ground = Prim(PrimitiveType.Cylinder, "Island", isle, t + new Vector3(0, -2f, 0), new Vector3(64f, 2f, 64f), Grass);
			ground.layer = Ground;
			ReplaceWithMeshCollider(ground);

			var beach = Prim(PrimitiveType.Cylinder, "Beach", isle, t + new Vector3(0, -2.08f, 0), new Vector3(72f, 2f, 72f), Sand);
			beach.layer = Ground;
			ReplaceWithMeshCollider(beach);

			Hill(isle, t + new Vector3(-12f, 0, 8f), 9f, 3f, GrassDark);
			Hill(isle, t + new Vector3(14f, 0, -8f), 7f, 2f, Grass);

			Physics.SyncTransforms();

			for (var i = 0; i < 9; i++)
				Palm(isle, Around(t, 10f, 29f));

			Recruit(isle, OnGround(t.x - 6f, t.z - 28f), "tilly", 180f);

			for (var i = 0; i < 7; i++)
				Gatherable(isle, "Seashell", Around(t, 30f, 33.5f), "seashell", 1, 100f, visual =>
				{
					Prim(PrimitiveType.Sphere, "Shell", visual, new Vector3(0, 0.06f, 0), new Vector3(0.3f, 0.12f, 0.36f), Seashell, false, new Vector3(0, Range(0, 360), 0));
					Prim(PrimitiveType.Sphere, "Shell", visual, new Vector3(0.25f, 0.05f, 0.1f), new Vector3(0.2f, 0.1f, 0.24f), Seashell, false, new Vector3(0, Range(0, 360), 0));
				}, false, "Pick up a");

			for (var i = 0; i < 5; i++)
				Gatherable(isle, "Kelp", Around(t, 33.5f, 35f), "kelp", 1, 100f, visual =>
				{
					for (var k = 0; k < 5; k++)
						Prim(PrimitiveType.Cylinder, "Frond", visual, new Vector3(Range(-0.3f, 0.3f), 0.3f, Range(-0.3f, 0.3f)), new Vector3(0.08f, 0.3f, 0.03f), Kelp, false,
							new Vector3(Range(-20, 20), Range(0, 360), Range(-20, 20)));
				}, false, "Gather");

			for (var i = 0; i < 3; i++)
				Clam(isle, Around(t, 27f, 31f));

			for (var i = 0; i < 4; i++)
				Gatherable(isle, "Driftwood", Around(t, 26f, 32f), "timber", 2, 120f, FallenLog, false, "Collect");

			BuildReef(isle);

			BuildGeyser(isle, t + new Vector3(-4f, 0f, 14f));
		}

		private static void Palm(Transform parent, Vector3 position)
		{
			var palm = Node("Palm", parent, position, Range(0, 360)).transform;
			palm.gameObject.layer = EnvironmentLayer;

			var lean = Range(8f, 18f);

			Prim(PrimitiveType.Cylinder, "Trunk", palm, new Vector3(0, 2.2f, 0.35f), new Vector3(0.35f, 2.3f, 0.35f), Lit("PalmTrunk", new Color(0.62f, 0.5f, 0.36f)), true, new Vector3(lean, 0, 0));

			// The top of the leaning trunk
			var crown = new Vector3(0, 2.2f, 0.35f) + Quaternion.Euler(lean, 0, 0) * Vector3.up * 2.3f;

			for (var i = 0; i < 6; i++)
				Prim(PrimitiveType.Sphere, "Frond", palm, crown + Quaternion.Euler(0, i * 60f, 0) * new Vector3(0, -0.1f, 0.9f), new Vector3(0.5f, 0.12f, 2f), Leaf(1), false,
					new Vector3(15f, i * 60f, 0));

			ArtSlot(palm.gameObject, "tree:palm", "Palm tree (Tidewater)");
		}

		private static void Clam(Transform parent, Vector3 position)
		{
			Gatherable(parent, "GiantClam", position, "pearl", 1, 240f, visual =>
			{
				var shell = Lit("ClamShell", new Color(0.75f, 0.7f, 0.85f), 0.5f);

				Prim(PrimitiveType.Sphere, "Lower", visual, new Vector3(0, 0.12f, 0), new Vector3(0.9f, 0.25f, 0.7f), shell, false);
				Prim(PrimitiveType.Sphere, "Upper", visual, new Vector3(0, 0.3f, -0.18f), new Vector3(0.9f, 0.22f, 0.7f), shell, false, new Vector3(-25f, 0, 0));
				Prim(PrimitiveType.Sphere, "Pearl", visual, new Vector3(0, 0.25f, 0.05f), Vector3.one * 0.16f, Lit("PearlGlow", new Color(0.95f, 0.95f, 1f), 0.9f, new Color(0.9f, 0.9f, 1f) * 1.5f), false);
			}, false, "Open the clam for a");
		}

		// Stepping rocks out to a reef where the Aqua glyph waits
		private static void BuildReef(Transform parent)
		{
			var reef = Node("Reef", parent).transform;

			var t = TidewaterCentre;

			var rocks = new[] { new Vector3(38.5f, 0.7f, 4f), new Vector3(42.5f, 1.4f, 1.5f), new Vector3(46.5f, 2.1f, 4.5f) };

			foreach (var rock in rocks)
				Pillar(reef, t + rock, 1.1f, Rock);

			var islet = t + new Vector3(51.5f, 2.8f, 2f);

			Pillar(reef, islet, 3.2f, Rock);
			Prim(PrimitiveType.Cylinder, "Sand", reef, islet + Vector3.up * 0.02f, new Vector3(5.6f, 0.02f, 5.6f), Sand, false);

			Shrine(reef, islet, "aqua", -90f);
		}

		// A sea-spray geyser whose updraft carries a broom up to Starfall Ridge
		private static void BuildGeyser(Transform parent, Vector3 position)
		{
			var geyser = Node("Geyser", parent, OnGround(position.x, position.z));

			for (var i = 0; i < 8; i++)
			{
				var angle = i * 45f * Mathf.Deg2Rad;
				Prim(PrimitiveType.Sphere, "Rock", geyser.transform, new Vector3(Mathf.Cos(angle) * 2.4f, 0.25f, Mathf.Sin(angle) * 2.4f), new Vector3(0.9f, 0.6f, 0.8f), Rock, false);
			}

			Prim(PrimitiveType.Cylinder, "Spray", geyser.transform, new Vector3(0, 0.05f, 0), new Vector3(3.5f, 0.05f, 3.5f), Lit("Swirl", new Color(0.8f, 0.95f, 1f), 0.5f, new Color(0.6f, 0.85f, 1f) * 1.2f), false);

			var updraft = Node("Updraft", geyser.transform);
			var box = updraft.AddComponent<BoxCollider>();
			box.isTrigger = true;
			box.center = new Vector3(0, 25f, 0);
			box.size = new Vector3(5f, 50f, 5f);
			updraft.AddComponent<Updraft>();
		}

		private static void BuildStarfall()
		{
			var ridge = Node("StarfallRidge", world).transform;

			FloatingIsle(ridge, StarfallCentre, 8f);
			FloatingIsle(ridge, AstraIsleCentre, 5f);
			FloatingIsle(ridge, CometNookCentre, 3.5f);

			Recruit(ridge, StarfallCentre + new Vector3(2f, 0f, -2f), "nova", 200f);

			Shrine(ridge, AstraIsleCentre + new Vector3(0.5f, 0f, -1f), "astra", 20f);

			foreach (var offset in new[] { new Vector3(-4f, 0, 2f), new Vector3(3f, 0, 4f), new Vector3(-1f, 0, -5f) })
				Stardust(ridge, StarfallCentre + offset);

			foreach (var offset in new[] { new Vector3(-2.5f, 0, 2f), new Vector3(2.5f, 0, 2.5f) })
				Stardust(ridge, AstraIsleCentre + offset);

			// The Comet Nook: only Blink crosses the gap
			Clam(ridge, CometNookCentre + new Vector3(-1f, 0, 0.5f));
			Clam(ridge, CometNookCentre + new Vector3(1.2f, 0, -0.8f));

			var moonpetal = ContentBuilder.Item("moonpetal").Color;
			Gatherable(ridge, "Moonpetal", CometNookCentre + new Vector3(0f, 0, 1.8f), "moonpetal", 1, 150f, visual => Flowers(visual, moonpetal, 4, 0.3f), false, "Gather", true);

			// A few twinkles hanging in the air around the ridge
			for (var i = 0; i < 12; i++)
			{
				var sparkle = Node("Twinkle", ridge, StarfallCentre + new Vector3(Range(-14f, 14f), Range(2f, 14f), Range(-20f, 12f)));
				Prim(PrimitiveType.Sphere, "Star", sparkle.transform, Vector3.zero, Vector3.one * 0.15f, StarGlow, false);
				sparkle.AddComponent<Bobbing>();
			}
		}

		private static void Stardust(Transform parent, Vector3 position)
		{
			Gatherable(parent, "Stardust", position, "stardust", 1, 150f, visual =>
			{
				for (var i = 0; i < 6; i++)
					Prim(PrimitiveType.Sphere, "Mote", visual, new Vector3(Range(-0.3f, 0.3f), Range(0.1f, 0.5f), Range(-0.3f, 0.3f)), Vector3.one * 0.09f, StarGlow, false);

				PointLight(visual, new Vector3(0, 0.3f, 0), new Color(1f, 0.9f, 0.6f), 2.5f, 1f);
			}, false, "Scoop up", true);
		}

		#endregion

		#region Characters

		private static GameObject BuildPlayer()
		{
			var player = new GameObject("Witch");
			player.tag = "Player";
			player.transform.SetPositionAndRotation(SpawnPoint, Quaternion.identity);

			var body = player.AddComponent<Rigidbody>();
			body.mass = 1f;
			body.interpolation = RigidbodyInterpolation.Interpolate;
			body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			body.constraints = RigidbodyConstraints.FreezeRotation;
			body.useGravity = false;

			var capsule = player.AddComponent<CapsuleCollider>();
			capsule.height = 1.6f;
			capsule.radius = 0.35f;
			capsule.center = new Vector3(0, 0.8f, 0);
			capsule.sharedMaterial = Slippery();

			var bankPivot = Node("BankPivot", player.transform);
			var visual = Node("Visual", bankPivot.transform);
			var figure = Node("Body", visual.transform);

			BuildWitchFigure(figure.transform, out var hat);

			var broom = Node("Broom", bankPivot.transform, new Vector3(0, 0.55f, 0));
			Prim(PrimitiveType.Cylinder, "Handle", broom.transform, new Vector3(0, 0, 0.1f), new Vector3(0.07f, 1f, 0.07f), Wood, false, new Vector3(90, 0, 0));
			MeshObject("Bristles", broom.transform, Cone(), new Vector3(0, 0, -0.75f), new Vector3(0.45f, 0.65f, 0.45f), Lit("Bristles", new Color(0.85f, 0.7f, 0.4f)), false, new Vector3(90, 0, 0));
			Prim(PrimitiveType.Cylinder, "Binding", broom.transform, new Vector3(0, 0, -0.8f), new Vector3(0.16f, 0.05f, 0.16f), Lit("Ribbon", new Color(0.8f, 0.25f, 0.3f)), false, new Vector3(90, 0, 0));

			// The seat lives outside the broom so the cat is never parented under a hidden object
			var seat = Node("CatSeat", bankPivot.transform, new Vector3(0, 0.6f, -0.65f));

			var cameraTarget = Node("CameraTarget", player.transform, new Vector3(0, 1.6f, 0));

			var glimmer = PointLight(player.transform, new Vector3(0, 1.2f, 0), new Color(0.5f, 1f, 0.9f), 9f, 2f);

			SetLayer(player, Player);

			var stats = AssetDatabase.LoadAssetAtPath<CharacterStats>(ContentBuilder.CharacterStatsPath);
			var broomStats = AssetDatabase.LoadAssetAtPath<BroomStats>(ContentBuilder.BroomStatsPath);

			Set(player.AddComponent<MovementController>(), "stats", stats);

			var broomController = player.AddComponent<BroomController>();
			Set(broomController, "stats", broomStats);
			Set(broomController, "broomVisual", broom);
			Set(broomController, "bankPivot", bankPivot.transform);

			player.AddComponent<CharacterMotor>();

			player.AddComponent<LevitateSpell>();
			player.AddComponent<ConjureSpell>();
			player.AddComponent<UnmakeSpell>();
			player.AddComponent<GustSpell>();
			player.AddComponent<BloomSpell>();
			player.AddComponent<WispSpell>();
			player.AddComponent<BubbleSpell>();
			player.AddComponent<RainCallSpell>();
			player.AddComponent<BlinkSpell>();
			player.AddComponent<SpellCaster>();

			player.AddComponent<ActionController>();

			var cameraTargetComponent = player.AddComponent<CharacterCameraTarget>();
			Set(cameraTargetComponent, "cameraTarget", cameraTarget.transform);

			var observer = player.AddComponent<ObserverTarget>();
			Set(observer, "cameraTarget", cameraTargetComponent);
			Set(observer, "broomSeat", seat.transform);

			player.AddComponent<PlayerCharacter>();
			player.AddComponent<FallRecovery>();

			Set(player.AddComponent<PotionAura>(), "glimmerLight", glimmer);

			ArtSlot(visual, "witch", "The witch (an Animator using Crewfall's locomotion parameters is driven)");
			ArtSlot(broom, "broom", "The broom (shown while flying)");

			var animator = visual.AddComponent<CharacterAnimator>();
			Set(animator, "body", figure.transform);
			Set(animator, "hat", hat);

			return player;
		}

		private static PhysicsMaterial Slippery()
		{
			const string path = "Assets/Data/Slippery.physicMaterial";

			var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);

			if (material == null)
			{
				material = new PhysicsMaterial("Slippery");
				AssetDatabase.CreateAsset(material, path);
			}

			material.dynamicFriction = 0f;
			material.staticFriction = 0f;
			material.frictionCombine = PhysicsMaterialCombine.Minimum;
			material.bounciness = 0f;

			EditorUtility.SetDirty(material);

			return material;
		}

		private static void BuildWitchFigure(Transform figure, out Transform hat)
		{
			var robe = Lit("Robe", new Color(0.36f, 0.24f, 0.52f));
			var robeTrim = Lit("RobeTrim", new Color(0.95f, 0.75f, 0.4f));
			var skin = Lit("Skin", new Color(0.98f, 0.82f, 0.7f));
			var hair = Lit("Hair", new Color(0.85f, 0.42f, 0.3f));
			var hatMat = Lit("Hat", new Color(0.24f, 0.16f, 0.36f));

			MeshObject("Robe", figure, Cone(), Vector3.zero, new Vector3(0.85f, 1.35f, 0.85f), robe);
			Prim(PrimitiveType.Cylinder, "Hem", figure, new Vector3(0, 0.04f, 0), new Vector3(0.82f, 0.04f, 0.82f), robeTrim, false);
			Prim(PrimitiveType.Sphere, "Shoulders", figure, new Vector3(0, 0.95f, 0), new Vector3(0.45f, 0.3f, 0.38f), robe, false);

			foreach (var side in new[] { -1f, 1f })
			{
				Prim(PrimitiveType.Capsule, "Arm", figure, new Vector3(side * 0.26f, 0.8f, 0.04f), new Vector3(0.13f, 0.22f, 0.13f), robe, false, new Vector3(10, 0, side * 18f));
				Prim(PrimitiveType.Sphere, "Hand", figure, new Vector3(side * 0.33f, 0.6f, 0.08f), Vector3.one * 0.1f, skin, false);
			}

			Prim(PrimitiveType.Sphere, "Head", figure, new Vector3(0, 1.22f, 0), Vector3.one * 0.38f, skin, false);
			Prim(PrimitiveType.Sphere, "Hair", figure, new Vector3(0, 1.24f, -0.06f), new Vector3(0.42f, 0.4f, 0.38f), hair, false);
			Prim(PrimitiveType.Sphere, "Braid", figure, new Vector3(0.14f, 1.0f, -0.12f), new Vector3(0.12f, 0.25f, 0.12f), hair, false);

			var eye = Lit("EyePupil", new Color(0.08f, 0.06f, 0.1f), 0.9f);
			var cheek = Lit("Cheek", new Color(1f, 0.6f, 0.6f));

			foreach (var side in new[] { -1f, 1f })
			{
				Prim(PrimitiveType.Sphere, "Eye", figure, new Vector3(side * 0.07f, 1.25f, 0.17f), new Vector3(0.05f, 0.07f, 0.03f), eye, false);
				Prim(PrimitiveType.Sphere, "Cheek", figure, new Vector3(side * 0.11f, 1.17f, 0.15f), new Vector3(0.06f, 0.03f, 0.02f), cheek, false);
			}

			hat = Node("Hat", figure, new Vector3(0, 1.36f, 0)).transform;
			Prim(PrimitiveType.Cylinder, "Brim", hat, Vector3.zero, new Vector3(0.75f, 0.015f, 0.75f), hatMat, false);
			MeshObject("Crown", hat, Cone(), Vector3.zero, new Vector3(0.38f, 0.65f, 0.38f), hatMat, false, new Vector3(-8, 0, 0));
			Prim(PrimitiveType.Cylinder, "Band", hat, new Vector3(0, 0.06f, 0), new Vector3(0.36f, 0.04f, 0.36f), robeTrim, false);
			Prim(PrimitiveType.Sphere, "Star", hat, new Vector3(0.12f, 0.12f, 0.12f), Vector3.one * 0.06f, Lit("Star", new Color(1f, 0.9f, 0.5f), 0.5f, new Color(1f, 0.85f, 0.4f) * 2f), false);
		}

		private static void BuildCat(GameObject player)
		{
			var cat = new GameObject("Sable");
			cat.transform.position = player.transform.position + new Vector3(1f, 0, -1.5f);
			cat.layer = Interactable;

			Trigger(cat, 0.6f, new Vector3(0, 0.3f, 0));

			var fur = Lit("CatFur", new Color(0.12f, 0.11f, 0.15f), 0.35f);
			var eye = Lit("CatEye", new Color(0.7f, 1f, 0.4f), 0.8f, new Color(0.5f, 0.9f, 0.3f) * 1.5f);

			var body = Prim(PrimitiveType.Sphere, "Body", cat.transform, new Vector3(0, 0.17f, 0), new Vector3(0.32f, 0.34f, 0.58f), fur, false);

			var head = Node("Head", cat.transform, new Vector3(0, 0.38f, 0.3f));
			Prim(PrimitiveType.Sphere, "Skull", head.transform, Vector3.zero, Vector3.one * 0.28f, fur, false);

			foreach (var side in new[] { -1f, 1f })
			{
				MeshObject("Ear", head.transform, Cone(), new Vector3(side * 0.08f, 0.1f, -0.01f), new Vector3(0.09f, 0.13f, 0.06f), fur, false, new Vector3(0, 0, -side * 15f));
				Prim(PrimitiveType.Sphere, "Eye", head.transform, new Vector3(side * 0.06f, 0.03f, 0.12f), new Vector3(0.05f, 0.06f, 0.03f), eye, false);
			}

			Prim(PrimitiveType.Sphere, "Nose", head.transform, new Vector3(0, -0.02f, 0.14f), Vector3.one * 0.025f, Lit("CatNose", new Color(1f, 0.6f, 0.65f)), false);

			var tail = Node("Tail", cat.transform, new Vector3(0, 0.25f, -0.28f));
			Prim(PrimitiveType.Capsule, "Tail", tail.transform, new Vector3(0, 0.2f, 0), new Vector3(0.06f, 0.22f, 0.06f), fur, false);

			// Collar with a bell
			Prim(PrimitiveType.Cylinder, "Collar", head.transform, new Vector3(0, -0.1f, -0.02f), new Vector3(0.2f, 0.02f, 0.2f), Lit("Ribbon", new Color(0.8f, 0.25f, 0.3f)), false);
			Prim(PrimitiveType.Sphere, "Bell", head.transform, new Vector3(0, -0.13f, 0.08f), Vector3.one * 0.04f, Lit("Gold", new Color(1f, 0.8f, 0.35f), 0.9f), false);

			SetLayer(cat, Interactable);

			ArtSlot(cat, "cat", "Sable the cat (a rig with a CatRigProfile is crossfaded by state name)");

			var companion = cat.AddComponent<CatCompanion>();
			Set(companion, "body", body.transform);
			Set(companion, "tail", tail.transform);
			Set(companion, "head", head.transform);
		}

		private static void BuildCamera()
		{
			var rig = new GameObject("CameraRig");
			rig.transform.position = SpawnPoint + Vector3.up * 1.6f;

			var yaw = Node("YawPivot", rig.transform);
			var pitch = Node("PitchPivot", yaw.transform);

			var cameraObject = new GameObject("Main Camera", typeof(UnityEngine.Camera), typeof(AudioListener));
			cameraObject.tag = "MainCamera";
			cameraObject.transform.SetParent(pitch.transform, false);
			cameraObject.transform.localPosition = new Vector3(0, 0, -6f);

			var camera = cameraObject.GetComponent<UnityEngine.Camera>();
			camera.nearClipPlane = 0.1f;
			camera.farClipPlane = 600f;
			camera.fieldOfView = 55f;

			var data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
			data.renderPostProcessing = true;
			data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

			var controller = rig.AddComponent<CameraController>();
			Set(controller, "yawPivot", yaw.transform);
			Set(controller, "pitchPivot", pitch.transform);
			Set(controller, "cameraTransform", cameraObject.transform);

			var serialized = new SerializedObject(controller);
			serialized.FindProperty("collisionMask").intValue = LayerMask.GetMask("Ground", "Environment");
			serialized.ApplyModifiedPropertiesWithoutUndo();
		}

		#endregion
	}
}
