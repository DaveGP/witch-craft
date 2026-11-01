using System.Collections.Generic;
using System.Linq;
using Data.Enums;
using Data.ScriptableObjects;
using Home;
using Interaction;
using UnityEditor;
using UnityEngine;
using World;
using static WitchCraftEditor.WitchCraftBuildKit;


namespace WitchCraftEditor
{
	// Generates every data asset and prefab: items, glyphs, spells, recipes, build pieces, furniture and companions,
	// then gathers them into the GameDatabase in Resources. Safe to re-run: existing assets are updated in place.
	public static class ContentBuilder
	{
		private const string DataFolder = "Assets/Data";

		private const string PrefabFolder = "Assets/Prefabs";

		public const string DatabasePath = "Assets/Resources/GameDatabase.asset";

		public const string CharacterStatsPath = DataFolder + "/CharacterStats.asset";

		public const string BroomStatsPath = DataFolder + "/BroomStats.asset";

		private static readonly Dictionary<string, ItemDefinition> items = new();

		private static readonly Dictionary<string, GlyphDefinition> glyphs = new();

		private static readonly Dictionary<string, RecipeDefinition> recipes = new();

		public static ItemDefinition Item(string id) => items[id];

		public static GlyphDefinition Glyph(string id) => glyphs[id];

		public static RecipeDefinition Recipe(string id) => recipes[id];

		public static GameDatabase Database { get; private set; }

		[MenuItem("Witch Craft/Build Content")]
		public static void Build()
		{
			ClearCache();
			items.Clear();
			glyphs.Clear();
			recipes.Clear();

			Database = LoadOrCreate<GameDatabase>(DatabasePath);

			BuildStats();
			BuildItems();
			BuildGlyphs();
			BuildSpells();
			BuildRecipes();
			BuildMaterials();
			BuildPieces();
			BuildCompanions();
			BuildHomeLevels();
			BuildNewGame();

			EditorUtility.SetDirty(Database);
			AssetDatabase.SaveAssets();

			Debug.Log($"ContentBuilder: {Database.Items.Count} items, {Database.Glyphs.Count} glyphs, {Database.Spells.Count} spells, " +
			          $"{Database.Recipes.Count} recipes, {Database.BuildPieces.Count} build pieces, {Database.Companions.Count} companions.");
		}

		private static void BuildStats()
		{
			var character = LoadOrCreate<CharacterStats>(CharacterStatsPath);
			EditorUtility.SetDirty(character);

			var broom = LoadOrCreate<BroomStats>(BroomStatsPath);
			EditorUtility.SetDirty(broom);
		}

		#region Items

		private static void BuildItems()
		{
			Database.Items.Clear();

			AddItem("spring_water", "Spring Water", ItemCategory.Base, new Color(0.55f, 0.8f, 1f), "Cold, clear water from the spring by the pond. A brewing base.");
			AddItem("moon_dew", "Moon Dew", ItemCategory.Base, new Color(0.78f, 0.72f, 1f), "Dew shaken from the silverbells on the southern hills. A brewing base.");

			AddItem("lavender", "Lavender", ItemCategory.Ingredient, new Color(0.72f, 0.55f, 0.95f), "Calming and fragrant. Grows in the forest clearings.");
			AddItem("nettle", "Nettle", ItemCategory.Ingredient, new Color(0.35f, 0.65f, 0.3f), "Stings a little. Gets the blood moving.");
			AddItem("glowcap", "Glowcap", ItemCategory.Ingredient, new Color(0.35f, 1f, 0.85f), "A softly luminous mushroom from shady places.");
			AddItem("wild_berry", "Wild Berry", ItemCategory.Ingredient, new Color(0.9f, 0.22f, 0.38f), "Tart little berries. Everybody's favourite snack.");
			AddItem("moonpetal", "Moonpetal", ItemCategory.Ingredient, new Color(0.92f, 0.92f, 1f), "A pale flower that only grows in high, quiet places.");
			AddItem("feather_down", "Feather Down", ItemCategory.Ingredient, new Color(1f, 0.97f, 0.9f), "Impossibly light. Drifts about the sky islands.");
			AddItem("ember_moss", "Ember Moss", ItemCategory.Ingredient, new Color(1f, 0.45f, 0.2f), "Warm to the touch. Clings to the walls of the crystal cave.");

			AddItem("timber", "Timber", ItemCategory.Material, new Color(0.62f, 0.43f, 0.26f), "Fallen branches and logs. Builds walls, floors and furniture.");
			AddItem("stone", "Stone", ItemCategory.Material, new Color(0.62f, 0.62f, 0.66f), "Smooth grey stone from the hills.");
			AddItem("reed", "Reed", ItemCategory.Material, new Color(0.8f, 0.72f, 0.42f), "Pond reeds, good for thatch and weaving.");
			AddItem("clay", "Clay", ItemCategory.Material, new Color(0.78f, 0.46f, 0.35f), "Sticky red clay from the pond bank.");
			AddItem("crystal", "Crystal", ItemCategory.Material, new Color(0.6f, 0.9f, 1f), "Humming crystal from the cave. Makes glass and lamps.");

			AddPotion("swiftness_brew", "Swiftness Brew", new Color(0.4f, 0.85f, 1f), PotionEffect.Swiftness, 60f, "Your feet feel feather light. Run faster for a minute.");
			AddPotion("leaping_tonic", "Leaping Tonic", new Color(0.5f, 1f, 0.4f), PotionEffect.Leaping, 60f, "Springy! Jump higher, with an extra air jump.");
			AddPotion("featherfall_elixir", "Featherfall Elixir", new Color(1f, 0.95f, 0.8f), PotionEffect.Featherfall, 60f, "You drift down like a dandelion seed.");
			AddPotion("glimmer_draught", "Glimmer Draught", new Color(0.55f, 1f, 0.9f), PotionEffect.Glimmer, 90f, "You glow softly, and hidden paths shimmer into being.");
			AddPotion("skyward_draught", "Skyward Draught", new Color(0.65f, 0.7f, 1f), PotionEffect.Skyward, 30f, "Your broom will not tire for a while.");
			AddPotion("murky_sludge", "Murky Sludge", new Color(0.42f, 0.42f, 0.36f), PotionEffect.None, 0f, "A failed brew. It smells like regret.");
		}

		private static void AddItem(string id, string name, ItemCategory category, Color colour, string description)
		{
			var item = LoadOrCreate<ItemDefinition>($"{DataFolder}/Items/{id}.asset");

			item.Id = id;
			item.DisplayName = name;
			item.Category = category;
			item.Color = colour;
			item.Description = description;

			EditorUtility.SetDirty(item);

			items[id] = item;
			Database.Items.Add(item);
		}

		private static void AddPotion(string id, string name, Color colour, PotionEffect effect, float duration, string description)
		{
			AddItem(id, name, ItemCategory.Potion, colour, description);

			items[id].PotionEffect = effect;
			items[id].PotionDuration = duration;
		}

		#endregion

		#region Magic

		private static void BuildGlyphs()
		{
			Database.Glyphs.Clear();

			AddGlyph("forma", "Forma", "□", new Color(0.95f, 0.75f, 0.45f), "The glyph of shape and structure.");
			AddGlyph("terra", "Terra", "▼", new Color(0.65f, 0.5f, 0.3f), "The glyph of earth and stone.");
			AddGlyph("aer", "Aer", "▲", new Color(0.65f, 0.85f, 1f), "The glyph of air and lightness.");
			AddGlyph("motus", "Motus", "►", new Color(0.55f, 0.95f, 0.9f), "The glyph of motion.");
			AddGlyph("vita", "Vita", "♣", new Color(0.45f, 0.9f, 0.45f), "The glyph of life and growth.");
			AddGlyph("lux", "Lux", "☼", new Color(1f, 0.92f, 0.5f), "The glyph of light.");
			AddGlyph("nihil", "Nihil", "○", new Color(0.55f, 0.45f, 0.75f), "The glyph of emptiness and undoing.");
		}

		private static void AddGlyph(string id, string name, string rune, Color colour, string description)
		{
			var glyph = LoadOrCreate<GlyphDefinition>($"{DataFolder}/Glyphs/{id}.asset");

			glyph.Id = id;
			glyph.DisplayName = name;
			glyph.Rune = rune;
			glyph.Color = colour;
			glyph.Description = description;

			EditorUtility.SetDirty(glyph);

			glyphs[id] = glyph;
			Database.Glyphs.Add(glyph);
		}

		private static void BuildSpells()
		{
			Database.Spells.Clear();

			AddSpell("conjure", "Conjure", SpellEffectType.Conjure, new Color(1f, 0.75f, 0.4f), new[] { "terra", "forma" },
				"Shape materials into floors, walls, roofs and furniture on your home plot.",
				"Earth, given a shape, becomes a home.");

			AddSpell("levitate", "Levitate", SpellEffectType.Levitate, new Color(0.78f, 0.62f, 1f), new[] { "aer", "forma" },
				"Lift furniture and crates, and set them down wherever you like.",
				"A shape made as light as air.");

			AddSpell("unmake", "Unmake", SpellEffectType.Unmake, new Color(0.62f, 0.5f, 0.85f), new[] { "nihil", "forma" },
				"Unravel something you built and get its materials back.",
				"Pour emptiness into a shape and the shape lets go.");

			AddSpell("gust", "Gust", SpellEffectType.Gust, new Color(0.7f, 0.95f, 1f), new[] { "aer", "motus" },
				"Ride a burst of wind: a long leap, an air dash, or a burst of broom speed.",
				"Air that refuses to sit still.");

			AddSpell("bloom", "Bloom", SpellEffectType.Bloom, new Color(0.5f, 1f, 0.5f), new[] { "vita", "terra" },
				"Make enchanted soil burst into growth. Vines to climb, herbs to harvest.",
				"Life sown into the earth.");

			AddSpell("wisp", "Wisp", SpellEffectType.Wisp, new Color(1f, 0.95f, 0.6f), new[] { "lux", "vita" },
				"Call a little living light that reveals hidden paths.",
				"A light that is alive.");
		}

		private static void AddSpell(string id, string name, SpellEffectType effect, Color colour, string[] glyphIds, string description, string hint)
		{
			var spell = LoadOrCreate<SpellDefinition>($"{DataFolder}/Spells/{id}.asset");

			spell.Id = id;
			spell.DisplayName = name;
			spell.Effect = effect;
			spell.Color = colour;
			spell.Glyphs = glyphIds.Select(glyphId => glyphs[glyphId]).ToList();
			spell.Description = description;
			spell.Hint = hint;

			EditorUtility.SetDirty(spell);

			Database.Spells.Add(spell);
		}

		private static void BuildRecipes()
		{
			Database.Recipes.Clear();

			AddRecipe("swiftness", "Swiftness Brew", "swiftness_brew", "For errands that simply cannot wait.",
				Pour("spring_water"), Whole("nettle", 2), Boil(1), Ground("lavender"), Boil(1), Bottle());

			AddRecipe("leaping", "Leaping Tonic", "leaping_tonic", "Grandmother swore by it for reaching the top shelf.",
				Pour("spring_water"), Ground("glowcap"), Boil(2), Whole("wild_berry"), Bottle());

			AddRecipe("featherfall", "Featherfall Elixir", "featherfall_elixir", "Scribbled on a page found high above the world.",
				Pour("moon_dew"), Whole("feather_down", 2), Ground("lavender"), Boil(1), Bottle());

			AddRecipe("glimmer", "Glimmer Draught", "glimmer_draught", "Pip's family secret. Makes you sparkle.",
				Pour("moon_dew"), Ground("glowcap", 2), Boil(1), Whole("moonpetal"), Boil(1), Bottle());

			AddRecipe("skyward", "Skyward Draught", "skyward_draught", "Hoot's recipe for tireless wings and tireless brooms.",
				Pour("spring_water"), Whole("moonpetal"), Ground("feather_down"), Whole("ember_moss"), Boil(3), Bottle());

			Database.FailedBrew = items["murky_sludge"];
		}

		private static AlchemyStep Pour(string id) => new(AlchemyStepType.PourBase, items[id]);

		private static AlchemyStep Whole(string id, int amount = 1) => new(AlchemyStepType.AddWhole, items[id], amount);

		private static AlchemyStep Ground(string id, int amount = 1) => new(AlchemyStepType.AddGround, items[id], amount);

		private static AlchemyStep Boil(int turns) => new(AlchemyStepType.Boil, null, turns);

		private static AlchemyStep Bottle() => new(AlchemyStepType.Bottle, null);

		private static void AddRecipe(string id, string name, string output, string flavour, params AlchemyStep[] steps)
		{
			var recipe = LoadOrCreate<RecipeDefinition>($"{DataFolder}/Recipes/{id}.asset");

			recipe.Id = id;
			recipe.DisplayName = name;
			recipe.Output = items[output];
			recipe.Flavour = flavour;
			recipe.Steps = steps.ToList();

			EditorUtility.SetDirty(recipe);

			recipes[id] = recipe;
			Database.Recipes.Add(recipe);
		}

		private static void BuildMaterials()
		{
			Database.ParticleMaterial = Particle("Particle", SoftDot());
			Database.GhostMaterial = Unlit("Ghost", new Color(0.6f, 1f, 0.7f, 0.45f), true);
			Database.GlowMaterial = Unlit("Glow", Color.white);
		}

		#endregion

		#region Build pieces

		private static Material Wood => Lit("Wood", new Color(0.6f, 0.42f, 0.27f));
		private static Material DarkWood => Lit("DarkWood", new Color(0.4f, 0.27f, 0.18f));
		private static Material Plaster => Lit("Plaster", new Color(0.95f, 0.9f, 0.8f));
		private static Material StoneMat => Lit("Stone", new Color(0.62f, 0.62f, 0.66f));
		private static Material Thatch => Lit("Thatch", new Color(0.85f, 0.7f, 0.4f));
		private static Material Slate => Lit("Slate", new Color(0.38f, 0.42f, 0.52f));
		private static Material Glass => Transparent("Glass", new Color(0.7f, 0.9f, 1f, 0.35f));
		private static Material Cloth(string name, Color colour) => Lit($"Cloth_{name}", colour, 0.05f);

		private static void BuildPieces()
		{
			Database.BuildPieces.Clear();

			Structure("floor_timber", "Timber Floor", BuildSnap.Floor, 1, Floor("TimberFloor", Wood), ("timber", 2));
			Structure("floor_stone", "Flagstone Floor", BuildSnap.Floor, 1, Floor("StoneFloor", StoneMat), ("stone", 2));
			Structure("wall_timber", "Timber Wall", BuildSnap.Wall, 1, Wall("TimberWall", Plaster, Wood), ("timber", 2));
			Structure("wall_window", "Window Wall", BuildSnap.Wall, 1, WindowWall("WindowWall", Plaster, Wood), ("timber", 2), ("crystal", 1));
			Structure("wall_door", "Doorway", BuildSnap.Wall, 1, DoorWall("Doorway", Plaster, Wood), ("timber", 2));
			Structure("fence", "Garden Fence", BuildSnap.Wall, 1, Fence(), ("timber", 1));
			Structure("roof_thatch", "Thatch Roof", BuildSnap.Roof, 1, Roof("ThatchRoof", Thatch), ("reed", 2), ("timber", 1));
			Structure("wall_stone", "Stone Wall", BuildSnap.Wall, 2, Wall("StoneWall", StoneMat, StoneMat), ("stone", 3));
			Structure("roof_slate", "Slate Roof", BuildSnap.Roof, 2, Roof("SlateRoof", Slate), ("stone", 2), ("timber", 1));
			Structure("stairs", "Stairs", BuildSnap.Floor, 2, Stairs(), ("timber", 4));

			Furniture("bed", "Cosy Bed", 1, Bed(), ("timber", 3), ("reed", 2));
			Furniture("table", "Table", 1, Table(), ("timber", 2));
			Furniture("chair", "Chair", 1, Chair(), ("timber", 1));
			Furniture("rug", "Woven Rug", 1, Rug(), ("reed", 2));
			Furniture("cat_bed", "Cat Bed", 1, CatBed(), ("timber", 1), ("reed", 1));
			Furniture("planter", "Herb Planter", 1, Planter(), ("clay", 2));
			Furniture("lamp", "Crystal Lamp", 1, Lamp(), ("crystal", 1), ("stone", 1));
			Furniture("bookshelf", "Bookshelf", 2, Bookshelf(), ("timber", 3));
			Furniture("potion_shelf", "Potion Shelf", 2, PotionShelf(), ("timber", 2), ("crystal", 1));
			Furniture("armchair", "Armchair", 2, Armchair(), ("timber", 2), ("reed", 3));
			Furniture("glowcap_lamp", "Glowcap Lamp", 3, GlowcapLamp(), ("crystal", 2), ("clay", 1));
		}

		private static void Structure(string id, string name, BuildSnap snap, int level, GameObject prefab, params (string item, int amount)[] cost)
			=> AddPiece(id, name, BuildCategory.Structure, snap, level, prefab, cost);

		private static void Furniture(string id, string name, int level, GameObject prefab, params (string item, int amount)[] cost)
			=> AddPiece(id, name, BuildCategory.Furniture, BuildSnap.Free, level, prefab, cost);

		private static void AddPiece(string id, string name, BuildCategory category, BuildSnap snap, int level, GameObject prefab, (string item, int amount)[] cost)
		{
			var piece = LoadOrCreate<BuildPieceDefinition>($"{DataFolder}/BuildPieces/{id}.asset");

			piece.Id = id;
			piece.DisplayName = name;
			piece.Category = category;
			piece.Snap = snap;
			piece.RequiredHomeLevel = level;
			piece.Prefab = prefab;
			piece.Cost = cost.Select(entry => new ItemStack(items[entry.item], entry.amount)).ToList();

			EditorUtility.SetDirty(piece);

			Database.BuildPieces.Add(piece);
		}

		private static GameObject Root(string name)
		{
			var root = new GameObject(name);
			root.layer = EnvironmentLayer;

			return root;
		}

		private static GameObject Save(GameObject root, string folder)
		{
			SetLayer(root, EnvironmentLayer);

			return SavePrefab(root, $"{PrefabFolder}/{folder}/{root.name}.prefab");
		}

		private static GameObject Floor(string name, Material material)
		{
			var root = Root(name);

			Prim(PrimitiveType.Cube, "Boards", root.transform, new Vector3(0, 0.1f, 0), new Vector3(2f, 0.2f, 2f), material);

			for (var i = 0; i < 4; i++)
				Prim(PrimitiveType.Cube, "Seam", root.transform, new Vector3(-0.75f + i * 0.5f, 0.201f, 0), new Vector3(0.03f, 0.005f, 2f), DarkWood, false);

			return Save(root, "Building");
		}

		private static GameObject Wall(string name, Material face, Material trim)
		{
			var root = Root(name);

			Prim(PrimitiveType.Cube, "Wall", root.transform, new Vector3(0, 1.25f, 0), new Vector3(2f, 2.5f, 0.2f), face);
			Prim(PrimitiveType.Cube, "Beam", root.transform, new Vector3(0, 2.4f, 0), new Vector3(2.04f, 0.2f, 0.24f), trim, false);
			Prim(PrimitiveType.Cube, "Sill", root.transform, new Vector3(0, 0.1f, 0), new Vector3(2.04f, 0.2f, 0.24f), trim, false);

			return Save(root, "Building");
		}

		private static GameObject WindowWall(string name, Material face, Material trim)
		{
			var root = Root(name);

			Prim(PrimitiveType.Cube, "Lower", root.transform, new Vector3(0, 0.45f, 0), new Vector3(2f, 0.9f, 0.2f), face);
			Prim(PrimitiveType.Cube, "Upper", root.transform, new Vector3(0, 2.2f, 0), new Vector3(2f, 0.6f, 0.2f), face);
			Prim(PrimitiveType.Cube, "Left", root.transform, new Vector3(-0.75f, 1.4f, 0), new Vector3(0.5f, 1.0f, 0.2f), face);
			Prim(PrimitiveType.Cube, "Right", root.transform, new Vector3(0.75f, 1.4f, 0), new Vector3(0.5f, 1.0f, 0.2f), face);
			Prim(PrimitiveType.Cube, "Pane", root.transform, new Vector3(0, 1.4f, 0), new Vector3(1f, 1f, 0.04f), Glass);
			Prim(PrimitiveType.Cube, "MullionV", root.transform, new Vector3(0, 1.4f, 0), new Vector3(0.06f, 1f, 0.08f), trim, false);
			Prim(PrimitiveType.Cube, "MullionH", root.transform, new Vector3(0, 1.4f, 0), new Vector3(1f, 0.06f, 0.08f), trim, false);
			Prim(PrimitiveType.Cube, "Beam", root.transform, new Vector3(0, 2.4f, 0), new Vector3(2.04f, 0.2f, 0.24f), trim, false);

			return Save(root, "Building");
		}

		private static GameObject DoorWall(string name, Material face, Material trim)
		{
			var root = Root(name);

			Prim(PrimitiveType.Cube, "Left", root.transform, new Vector3(-0.725f, 1.25f, 0), new Vector3(0.55f, 2.5f, 0.2f), face);
			Prim(PrimitiveType.Cube, "Right", root.transform, new Vector3(0.725f, 1.25f, 0), new Vector3(0.55f, 2.5f, 0.2f), face);
			Prim(PrimitiveType.Cube, "Lintel", root.transform, new Vector3(0, 2.25f, 0), new Vector3(0.9f, 0.5f, 0.2f), face);
			Prim(PrimitiveType.Cube, "Frame", root.transform, new Vector3(0, 2.0f, 0), new Vector3(1.0f, 0.08f, 0.26f), trim, false);
			Prim(PrimitiveType.Cube, "Beam", root.transform, new Vector3(0, 2.4f, 0), new Vector3(2.04f, 0.2f, 0.24f), trim, false);

			return Save(root, "Building");
		}

		private static GameObject Fence()
		{
			var root = Root("Fence");

			for (var i = 0; i < 3; i++)
				Prim(PrimitiveType.Cube, "Post", root.transform, new Vector3(-0.9f + i * 0.9f, 0.5f, 0), new Vector3(0.12f, 1f, 0.12f), Wood);

			Prim(PrimitiveType.Cube, "RailTop", root.transform, new Vector3(0, 0.8f, 0), new Vector3(2f, 0.08f, 0.06f), Wood, false);
			Prim(PrimitiveType.Cube, "RailLow", root.transform, new Vector3(0, 0.4f, 0), new Vector3(2f, 0.08f, 0.06f), Wood, false);

			return Save(root, "Building");
		}

		private static GameObject Roof(string name, Material material)
		{
			var root = Root(name);

			MeshObject("Slope", root.transform, Wedge(), Vector3.zero, Vector3.one, material, true);

			return Save(root, "Building");
		}

		private static GameObject Stairs()
		{
			var root = Root("Stairs");

			const int steps = 8;

			for (var i = 0; i < steps; i++)
			{
				var height = (i + 1) * (2.5f / steps);

				Prim(PrimitiveType.Cube, $"Step{i}", root.transform, new Vector3(0, height * 0.5f, -1f + (i + 0.5f) * (2f / steps)), new Vector3(1.2f, height, 2f / steps), Wood);
			}

			return Save(root, "Building");
		}

		#endregion

		#region Furniture

		private static GameObject Bed()
		{
			var root = Root("Bed");
			var t = root.transform;

			Prim(PrimitiveType.Cube, "Frame", t, new Vector3(0, 0.18f, 0), new Vector3(1.05f, 0.36f, 2f), Wood);
			Prim(PrimitiveType.Cube, "Mattress", t, new Vector3(0, 0.42f, -0.02f), new Vector3(0.95f, 0.16f, 1.85f), Cloth("Cream", new Color(0.97f, 0.93f, 0.85f)), false);
			Prim(PrimitiveType.Cube, "Quilt", t, new Vector3(0, 0.52f, -0.3f), new Vector3(0.98f, 0.06f, 1.25f), Cloth("Plum", new Color(0.55f, 0.32f, 0.6f)), false);
			Prim(PrimitiveType.Cube, "Pillow", t, new Vector3(0, 0.56f, 0.68f), new Vector3(0.65f, 0.14f, 0.35f), Cloth("White", Color.white), false);
			Prim(PrimitiveType.Cube, "Headboard", t, new Vector3(0, 0.55f, 0.97f), new Vector3(1.05f, 1.1f, 0.08f), Wood, false);

			root.AddComponent<Bed>();

			var zone = Node("Interact", t, new Vector3(0, 0.6f, 0));
			Trigger(zone, 1.1f);

			SetLayer(root, EnvironmentLayer);
			zone.layer = Interactable;

			return SavePrefab(root, $"{PrefabFolder}/Furniture/{root.name}.prefab");
		}

		private static GameObject Table()
		{
			var root = Root("Table");
			var t = root.transform;

			Prim(PrimitiveType.Cube, "Top", t, new Vector3(0, 0.76f, 0), new Vector3(1.4f, 0.08f, 0.9f), Wood);

			foreach (var x in new[] { -0.62f, 0.62f })
			foreach (var z in new[] { -0.37f, 0.37f })
				Prim(PrimitiveType.Cube, "Leg", t, new Vector3(x, 0.36f, z), new Vector3(0.08f, 0.72f, 0.08f), DarkWood, false);

			Prim(PrimitiveType.Cylinder, "Candle", t, new Vector3(0.3f, 0.88f, 0.1f), new Vector3(0.06f, 0.08f, 0.06f), Cloth("Cream", new Color(0.97f, 0.93f, 0.85f)), false);
			Prim(PrimitiveType.Cylinder, "Mug", t, new Vector3(-0.3f, 0.86f, -0.1f), new Vector3(0.12f, 0.06f, 0.12f), Lit("Teal", new Color(0.3f, 0.6f, 0.6f)), false);

			return Save(root, "Furniture");
		}

		private static GameObject Chair()
		{
			var root = Root("Chair");
			var t = root.transform;

			Prim(PrimitiveType.Cube, "Seat", t, new Vector3(0, 0.45f, 0), new Vector3(0.5f, 0.06f, 0.5f), Wood);
			Prim(PrimitiveType.Cube, "Back", t, new Vector3(0, 0.78f, -0.22f), new Vector3(0.5f, 0.6f, 0.06f), Wood, false);

			foreach (var x in new[] { -0.2f, 0.2f })
			foreach (var z in new[] { -0.2f, 0.2f })
				Prim(PrimitiveType.Cube, "Leg", t, new Vector3(x, 0.21f, z), new Vector3(0.05f, 0.42f, 0.05f), DarkWood, false);

			return Save(root, "Furniture");
		}

		private static GameObject Rug()
		{
			var root = Root("Rug");
			var t = root.transform;

			Prim(PrimitiveType.Cylinder, "Rug", t, new Vector3(0, 0.01f, 0), new Vector3(2.2f, 0.01f, 1.6f), Cloth("Rose", new Color(0.8f, 0.45f, 0.5f)));
			Prim(PrimitiveType.Cylinder, "Ring", t, new Vector3(0, 0.015f, 0), new Vector3(1.6f, 0.01f, 1.1f), Cloth("Gold", new Color(0.95f, 0.8f, 0.45f)), false);
			Prim(PrimitiveType.Cylinder, "Centre", t, new Vector3(0, 0.02f, 0), new Vector3(1.1f, 0.01f, 0.7f), Cloth("Rose", new Color(0.8f, 0.45f, 0.5f)), false);

			return Save(root, "Furniture");
		}

		private static GameObject CatBed()
		{
			var root = Root("CatBed");
			var t = root.transform;

			Prim(PrimitiveType.Cylinder, "Basket", t, new Vector3(0, 0.1f, 0), new Vector3(0.8f, 0.1f, 0.8f), Thatch);
			Prim(PrimitiveType.Cylinder, "Cushion", t, new Vector3(0, 0.2f, 0), new Vector3(0.62f, 0.04f, 0.62f), Cloth("Plum", new Color(0.55f, 0.32f, 0.6f)), false);

			return Save(root, "Furniture");
		}

		private static GameObject Planter()
		{
			var root = Root("Planter");
			var t = root.transform;

			var clay = Lit("Clay", new Color(0.78f, 0.46f, 0.35f));

			Prim(PrimitiveType.Cube, "Box", t, new Vector3(0, 0.25f, 0), new Vector3(1.3f, 0.5f, 0.6f), clay);
			Prim(PrimitiveType.Cube, "Soil", t, new Vector3(0, 0.49f, 0), new Vector3(1.18f, 0.04f, 0.48f), Lit("Soil", new Color(0.3f, 0.2f, 0.15f)), false);

			var glow = PointLight(t, new Vector3(0, 0.7f, 0), new Color(0.5f, 1f, 0.5f), 2.5f, 1.5f);

			var growth = Node("Growth", t, new Vector3(0, 0.5f, 0));

			var harvest = Node("Harvest", growth.transform);
			harvest.layer = Interactable;
			Trigger(harvest, 0.8f, new Vector3(0, 0.3f, 0));

			var visual = Node("Visual", harvest.transform);

			for (var i = 0; i < 4; i++)
			{
				var x = -0.45f + i * 0.3f;
				Prim(PrimitiveType.Cylinder, "Stem", visual.transform, new Vector3(x, 0.2f, 0), new Vector3(0.04f, 0.2f, 0.04f), Lit("Leaf", new Color(0.4f, 0.65f, 0.35f)), false);
				Prim(PrimitiveType.Sphere, "Bloom", visual.transform, new Vector3(x, 0.45f, 0), new Vector3(0.14f, 0.22f, 0.14f), Lit("LavenderBloom", new Color(0.72f, 0.55f, 0.95f)), false);
			}

			var gatherable = harvest.AddComponent<Gatherable>();
			Set(gatherable, "item", items["lavender"]);
			SetValue(gatherable, "amount", 2);
			SetValue(gatherable, "respawnSeconds", -1f);
			SetString(gatherable, "verb", "Harvest");
			Set(gatherable, "visual", visual);

			var spot = root.AddComponent<BloomSpot>();
			Set(spot, "growth", growth.transform);
			Set(spot, "soilGlow", glow);

			SetLayer(root, EnvironmentLayer);
			SetLayer(harvest, Interactable);

			return SavePrefab(root, $"{PrefabFolder}/Furniture/{root.name}.prefab");
		}

		private static GameObject Lamp()
		{
			var root = Root("Lamp");
			var t = root.transform;

			Prim(PrimitiveType.Cylinder, "Base", t, new Vector3(0, 0.05f, 0), new Vector3(0.4f, 0.05f, 0.4f), StoneMat);
			Prim(PrimitiveType.Cylinder, "Post", t, new Vector3(0, 0.7f, 0), new Vector3(0.07f, 0.65f, 0.07f), DarkWood);
			Prim(PrimitiveType.Sphere, "Crystal", t, new Vector3(0, 1.45f, 0), new Vector3(0.22f, 0.3f, 0.22f), Lit("LampCrystal", new Color(0.7f, 0.95f, 1f), 0.8f, new Color(0.6f, 0.9f, 1f) * 2.5f), false);

			PointLight(t, new Vector3(0, 1.45f, 0), new Color(1f, 0.85f, 0.6f), 7f, 2f);

			return Save(root, "Furniture");
		}

		private static GameObject Bookshelf()
		{
			var root = Root("Bookshelf");
			var t = root.transform;

			Prim(PrimitiveType.Cube, "Back", t, new Vector3(0, 1f, -0.18f), new Vector3(1.2f, 2f, 0.04f), DarkWood);
			Prim(PrimitiveType.Cube, "SideL", t, new Vector3(-0.58f, 1f, 0), new Vector3(0.04f, 2f, 0.4f), Wood, false);
			Prim(PrimitiveType.Cube, "SideR", t, new Vector3(0.58f, 1f, 0), new Vector3(0.04f, 2f, 0.4f), Wood, false);

			var bookColours = new[] { new Color(0.7f, 0.25f, 0.3f), new Color(0.25f, 0.4f, 0.65f), new Color(0.35f, 0.55f, 0.35f), new Color(0.85f, 0.7f, 0.35f), new Color(0.5f, 0.3f, 0.55f) };

			var random = new System.Random(7);

			for (var shelf = 0; shelf < 4; shelf++)
			{
				var y = 0.04f + shelf * 0.5f;

				Prim(PrimitiveType.Cube, "Shelf", t, new Vector3(0, y, 0), new Vector3(1.16f, 0.04f, 0.38f), Wood, false);

				var x = -0.5f;

				while (x < 0.48f && shelf < 3)
				{
					var width = 0.06f + (float)random.NextDouble() * 0.05f;
					var height = 0.3f + (float)random.NextDouble() * 0.12f;
					var colour = bookColours[random.Next(bookColours.Length)];

					Prim(PrimitiveType.Cube, "Book", t, new Vector3(x + width / 2f, y + 0.02f + height / 2f, 0.02f), new Vector3(width, height, 0.26f), Lit($"Book{System.Array.IndexOf(bookColours, colour)}", colour), false);

					x += width + 0.01f;
				}
			}

			Prim(PrimitiveType.Cube, "Top", t, new Vector3(0, 2f, 0), new Vector3(1.24f, 0.05f, 0.42f), Wood);

			var body = root.AddComponent<BoxCollider>();
			body.center = new Vector3(0, 1f, 0);
			body.size = new Vector3(1.2f, 2f, 0.4f);

			return Save(root, "Furniture");
		}

		private static GameObject PotionShelf()
		{
			var root = Root("PotionShelf");
			var t = root.transform;

			Prim(PrimitiveType.Cube, "Back", t, new Vector3(0, 1.1f, -0.12f), new Vector3(1.2f, 1.2f, 0.04f), DarkWood);

			var potionColours = items.Values.Where(item => item.Category == ItemCategory.Potion).Select(item => item.Color).ToArray();

			for (var shelf = 0; shelf < 3; shelf++)
			{
				var y = 0.6f + shelf * 0.4f;

				Prim(PrimitiveType.Cube, "Shelf", t, new Vector3(0, y, 0), new Vector3(1.2f, 0.04f, 0.26f), Wood, shelf == 0);

				for (var i = 0; i < 5; i++)
				{
					var colour = potionColours[(i + shelf * 2) % potionColours.Length];

					Prim(PrimitiveType.Cylinder, "Bottle", t, new Vector3(-0.45f + i * 0.22f, y + 0.12f, 0), new Vector3(0.1f, 0.1f, 0.1f),
						Lit($"PotionGlass{(i + shelf * 2) % potionColours.Length}", colour, 0.9f, colour * 0.8f), false);
				}
			}

			foreach (var x in new[] { -0.6f, 0.6f })
				Prim(PrimitiveType.Cube, "Leg", t, new Vector3(x, 0.85f, 0), new Vector3(0.05f, 1.7f, 0.26f), Wood, false);

			var body = root.AddComponent<BoxCollider>();
			body.center = new Vector3(0, 0.85f, 0);
			body.size = new Vector3(1.25f, 1.7f, 0.3f);

			return Save(root, "Furniture");
		}

		private static GameObject Armchair()
		{
			var root = Root("Armchair");
			var t = root.transform;

			var fabric = Cloth("Moss", new Color(0.45f, 0.6f, 0.4f));

			Prim(PrimitiveType.Cube, "Seat", t, new Vector3(0, 0.3f, 0.05f), new Vector3(0.9f, 0.35f, 0.8f), fabric);
			Prim(PrimitiveType.Cube, "Back", t, new Vector3(0, 0.75f, -0.35f), new Vector3(0.9f, 0.9f, 0.2f), fabric, false);
			Prim(PrimitiveType.Cube, "ArmL", t, new Vector3(-0.45f, 0.5f, 0.05f), new Vector3(0.18f, 0.45f, 0.8f), fabric, false);
			Prim(PrimitiveType.Cube, "ArmR", t, new Vector3(0.45f, 0.5f, 0.05f), new Vector3(0.18f, 0.45f, 0.8f), fabric, false);
			Prim(PrimitiveType.Cube, "Cushion", t, new Vector3(0, 0.5f, 0.1f), new Vector3(0.7f, 0.08f, 0.6f), Cloth("Cream", new Color(0.97f, 0.93f, 0.85f)), false);

			return Save(root, "Furniture");
		}

		private static GameObject GlowcapLamp()
		{
			var root = Root("GlowcapLamp");
			var t = root.transform;

			Prim(PrimitiveType.Cylinder, "Stem", t, new Vector3(0, 0.45f, 0), new Vector3(0.18f, 0.45f, 0.18f), Lit("MushroomStem", new Color(0.95f, 0.92f, 0.82f)));
			Prim(PrimitiveType.Sphere, "Cap", t, new Vector3(0, 0.95f, 0), new Vector3(0.9f, 0.4f, 0.9f), Lit("GlowcapCap", new Color(0.35f, 1f, 0.85f), 0.5f, new Color(0.2f, 0.9f, 0.7f) * 2f), false);

			PointLight(t, new Vector3(0, 0.8f, 0), new Color(0.4f, 1f, 0.85f), 6f, 2f);

			return Save(root, "Furniture");
		}

		#endregion

		#region Companions

		private static void BuildCompanions()
		{
			Database.Companions.Clear();

			var pip = AddCompanion("pip", "Pip", "Frog", new Color(0.45f, 0.82f, 0.38f), Frog(),
				"\"Ribbit! Oh! A witch! I've always wanted to live somewhere with a roof. Ponds are so... damp.\"",
				"\"If you brought me three Glowcaps, I'd hop right over to your house. They make the best night-lights.\"",
				("glowcap", 3),
				"\"Ribbit-ribbit! Wait for me, I'll catch up. Probably. Frogs are not fast walkers.\"",
				new[] { "\"Did you know lily pads are just pond plates?\"", "\"I counted the stars last night. I got to eleven and fell asleep.\"", "\"Sable keeps watching me. I don't think she wants to be friends. I think she wants a snack.\"" },
				new[] { "glowcap", "wild_berry", "moonpetal" });

			AddReward(pip, 3, recipes["glimmer"], null, "\"Here, my family's secret: Glimmer Draught. Moon Dew, two ground Glowcaps, boil, a Moonpetal, boil again. You'll sparkle!\"");
			AddReward(pip, 9, null, null, "\"You're my best friend. Don't tell the newts.\"");

			var bramble = AddCompanion("bramble", "Bramble", "Hedgehog", new Color(0.62f, 0.45f, 0.3f), Hedgehog(),
				"\"Hm? Oh, hullo. Mind the prickles. I'm Bramble. I mostly forage and complain about weather.\"",
				"\"Your home has a garden, does it? Bring me four Wild Berries and I'll come tend it.\"",
				("wild_berry", 4),
				"\"Right then. I'll pack my things. Which is to say, I'll roll there.\"",
				new[] { "\"The lavender in your planters wants more sun. Or less. One of those.\"", "\"Nettles are misunderstood. Like me.\"", "\"I heard the owl up in the sky islands keeps recipes in his nest.\"" },
				new[] { "wild_berry", "lavender", "nettle" });

			AddReward(bramble, 5, recipes["featherfall"], null, "\"Found this page in a hedge. Featherfall Elixir. Moon Dew, two Feather Down, ground Lavender, one boil.\"");

			var hoot = AddCompanion("hoot", "Hoot", "Owl", new Color(0.78f, 0.66f, 0.5f), Owl(),
				"\"Hoo. A visitor, all the way up here. You must be a determined sort of witch.\"",
				"\"I'd come down to live with you, but my wings are tired. A Swiftness Brew would see me there in no time.\"",
				("swiftness_brew", 1),
				"\"Hoo hoo! Already I feel younger. I'll meet you at your hearth.\"",
				new[] { "\"From my perch, your home looks like a little lantern.\"", "\"Wisdom is mostly sitting still and listening. And snacks.\"", "\"The fox spirit in the crystal cave is lonely. Bring her something that glows.\"" },
				new[] { "moonpetal", "feather_down", "leaping_tonic" });

			hoot.Nocturnal = true;

			AddReward(hoot, 4, recipes["skyward"], null, "\"Take this: Skyward Draught. Spring Water, a Moonpetal, ground Feather Down, Ember Moss, and three long boils. Your broom will never tire.\"");

			var ember = AddCompanion("ember", "Ember", "Fox Spirit", new Color(1f, 0.55f, 0.25f), Fox(),
				"\"...You can see me? Most people only see a flicker. I am Ember.\"",
				"\"It's so dark down here. If you brought me a Glimmer Draught, I think I could find my way out.\"",
				("glimmer_draught", 1),
				"\"It's so bright! Thank you, witch. I will keep your hearth warm, always.\"",
				new[] { "\"Your hearth is the cosiest fire I've ever slept beside.\"", "\"Crystals hum if you listen closely. These ones hum in B flat.\"", "\"Pip asked if I was a very orange dog. I have decided to allow it.\"" },
				new[] { "ember_moss", "crystal", "glimmer_draught" });

			AddReward(ember, 6, null, null, "\"I will tell you a secret, friend: light and life together make a little friend of their own. Lux and Vita.\"");
		}

		private static CompanionDefinition AddCompanion(string id, string name, string species, Color colour, GameObject prefab,
			string greeting, string plea, (string item, int amount) gift, string moveIn, string[] chat, string[] liked)
		{
			var companion = LoadOrCreate<CompanionDefinition>($"{DataFolder}/Companions/{id}.asset");

			companion.Id = id;
			companion.DisplayName = name;
			companion.Species = species;
			companion.Color = colour;
			companion.Prefab = prefab;
			companion.Nocturnal = false;
			companion.Greeting = greeting;
			companion.Plea = plea;
			companion.RecruitGift = new ItemStack(items[gift.item], gift.amount);
			companion.MoveInLine = moveIn;
			companion.ChatLines = chat.ToList();
			companion.LikedGifts = liked.Select(itemId => items[itemId]).ToList();
			companion.Rewards = new List<FriendshipReward>();

			EditorUtility.SetDirty(companion);

			Database.Companions.Add(companion);

			return companion;
		}

		private static void AddReward(CompanionDefinition companion, int friendship, RecipeDefinition recipe, GlyphDefinition glyph, string line)
		{
			companion.Rewards.Add(new FriendshipReward { Friendship = friendship, Recipe = recipe, Glyph = glyph, Line = line });
		}

		private static GameObject CompanionRoot(string name, out Transform visual)
		{
			var root = new GameObject(name);
			root.layer = Interactable;

			Trigger(root, 0.9f, new Vector3(0, 0.5f, 0));

			visual = Node("Visual", root.transform).transform;

			return root;
		}

		private static GameObject SaveCompanion(GameObject root)
		{
			SetLayer(root, Interactable);

			return SavePrefab(root, $"{PrefabFolder}/Companions/{root.name}.prefab");
		}

		private static void Eyes(Transform parent, float y, float z, float spread, float size)
		{
			var white = Lit("EyeWhite", Color.white, 0.6f);
			var pupil = Lit("EyePupil", new Color(0.08f, 0.06f, 0.1f), 0.9f);

			foreach (var side in new[] { -1f, 1f })
			{
				Prim(PrimitiveType.Sphere, "Eye", parent, new Vector3(side * spread, y, z), Vector3.one * size, white, false);
				Prim(PrimitiveType.Sphere, "Pupil", parent, new Vector3(side * spread, y, z + size * 0.35f), Vector3.one * size * 0.55f, pupil, false);
			}
		}

		private static GameObject Frog()
		{
			var root = CompanionRoot("Frog", out var v);

			var green = Lit("FrogGreen", new Color(0.45f, 0.82f, 0.38f), 0.5f);

			Prim(PrimitiveType.Sphere, "Body", v, new Vector3(0, 0.25f, 0), new Vector3(0.6f, 0.42f, 0.6f), green, false);
			Prim(PrimitiveType.Sphere, "Belly", v, new Vector3(0, 0.22f, 0.12f), new Vector3(0.45f, 0.32f, 0.4f), Lit("FrogBelly", new Color(0.85f, 0.95f, 0.7f)), false);

			foreach (var side in new[] { -1f, 1f })
			{
				Prim(PrimitiveType.Sphere, "EyeBump", v, new Vector3(side * 0.16f, 0.46f, 0.12f), Vector3.one * 0.2f, green, false);
				Prim(PrimitiveType.Sphere, "Foot", v, new Vector3(side * 0.28f, 0.05f, 0.1f), new Vector3(0.22f, 0.08f, 0.3f), green, false);
			}

			Eyes(v, 0.5f, 0.17f, 0.16f, 0.13f);

			Prim(PrimitiveType.Cube, "Smile", v, new Vector3(0, 0.3f, 0.29f), new Vector3(0.2f, 0.02f, 0.02f), Lit("EyePupil", new Color(0.08f, 0.06f, 0.1f)), false);

			return SaveCompanion(root);
		}

		private static GameObject Hedgehog()
		{
			var root = CompanionRoot("Hedgehog", out var v);

			var brown = Lit("HedgehogSpines", new Color(0.42f, 0.3f, 0.22f));
			var face = Lit("HedgehogFace", new Color(0.88f, 0.75f, 0.58f));

			Prim(PrimitiveType.Sphere, "Body", v, new Vector3(0, 0.25f, 0), new Vector3(0.55f, 0.45f, 0.7f), brown, false);
			Prim(PrimitiveType.Sphere, "Face", v, new Vector3(0, 0.22f, 0.28f), new Vector3(0.32f, 0.3f, 0.32f), face, false);
			Prim(PrimitiveType.Sphere, "Nose", v, new Vector3(0, 0.22f, 0.45f), Vector3.one * 0.07f, Lit("EyePupil", new Color(0.08f, 0.06f, 0.1f)), false);

			var cone = Cone();

			for (var i = 0; i < 14; i++)
			{
				var angle = i / 14f * Mathf.PI * 2f;
				var direction = new Vector3(Mathf.Cos(angle) * 0.8f, 0.6f + Mathf.Sin(i * 1.7f) * 0.2f, Mathf.Sin(angle) * 0.9f - 0.2f).normalized;

				MeshObject("Spine", v, cone, new Vector3(0, 0.27f, -0.05f) + direction * 0.24f, new Vector3(0.12f, 0.28f, 0.12f), brown, false,
					Quaternion.FromToRotation(Vector3.up, direction).eulerAngles);
			}

			Eyes(v, 0.3f, 0.38f, 0.08f, 0.07f);

			return SaveCompanion(root);
		}

		private static GameObject Owl()
		{
			var root = CompanionRoot("Owl", out var v);

			var feathers = Lit("OwlFeathers", new Color(0.62f, 0.5f, 0.38f));
			var face = Lit("OwlFace", new Color(0.95f, 0.88f, 0.75f));

			Prim(PrimitiveType.Sphere, "Body", v, new Vector3(0, 0.4f, 0), new Vector3(0.55f, 0.75f, 0.5f), feathers, false);
			Prim(PrimitiveType.Sphere, "Face", v, new Vector3(0, 0.55f, 0.16f), new Vector3(0.42f, 0.3f, 0.2f), face, false);
			Prim(PrimitiveType.Sphere, "Belly", v, new Vector3(0, 0.3f, 0.15f), new Vector3(0.38f, 0.42f, 0.25f), face, false);

			var amber = Lit("OwlEye", new Color(1f, 0.75f, 0.2f), 0.8f);

			foreach (var side in new[] { -1f, 1f })
			{
				Prim(PrimitiveType.Sphere, "Eye", v, new Vector3(side * 0.1f, 0.58f, 0.25f), Vector3.one * 0.14f, amber, false);
				Prim(PrimitiveType.Sphere, "Pupil", v, new Vector3(side * 0.1f, 0.58f, 0.31f), Vector3.one * 0.07f, Lit("EyePupil", new Color(0.08f, 0.06f, 0.1f)), false);
				MeshObject("Tuft", v, Cone(), new Vector3(side * 0.16f, 0.72f, 0f), new Vector3(0.1f, 0.18f, 0.1f), feathers, false, new Vector3(0, 0, -side * 20f));
				Prim(PrimitiveType.Sphere, "Wing", v, new Vector3(side * 0.26f, 0.38f, -0.03f), new Vector3(0.12f, 0.5f, 0.35f), feathers, false);
			}

			MeshObject("Beak", v, Cone(), new Vector3(0, 0.52f, 0.27f), new Vector3(0.06f, 0.1f, 0.06f), amber, false, new Vector3(150f, 0, 0));

			return SaveCompanion(root);
		}

		private static GameObject Fox()
		{
			var root = CompanionRoot("FoxSpirit", out var v);

			var fur = Lit("FoxFur", new Color(1f, 0.55f, 0.25f), 0.3f, new Color(1f, 0.35f, 0.1f) * 0.6f);
			var white = Lit("FoxWhite", new Color(1f, 0.96f, 0.9f), 0.3f, new Color(1f, 0.9f, 0.8f) * 0.3f);

			Prim(PrimitiveType.Sphere, "Body", v, new Vector3(0, 0.35f, -0.05f), new Vector3(0.4f, 0.4f, 0.7f), fur, false);
			Prim(PrimitiveType.Sphere, "Head", v, new Vector3(0, 0.62f, 0.32f), new Vector3(0.36f, 0.32f, 0.36f), fur, false);
			Prim(PrimitiveType.Sphere, "Snout", v, new Vector3(0, 0.56f, 0.5f), new Vector3(0.16f, 0.13f, 0.2f), white, false);
			Prim(PrimitiveType.Sphere, "Tail", v, new Vector3(0, 0.55f, -0.55f), new Vector3(0.32f, 0.32f, 0.6f), fur, false, new Vector3(-35f, 0, 0));
			Prim(PrimitiveType.Sphere, "TailTip", v, new Vector3(0, 0.78f, -0.8f), new Vector3(0.2f, 0.2f, 0.25f), white, false, new Vector3(-35f, 0, 0));

			foreach (var side in new[] { -1f, 1f })
			{
				MeshObject("Ear", v, Cone(), new Vector3(side * 0.11f, 0.78f, 0.3f), new Vector3(0.12f, 0.2f, 0.08f), fur, false, new Vector3(0, 0, -side * 12f));
				Prim(PrimitiveType.Cylinder, "Leg", v, new Vector3(side * 0.12f, 0.1f, 0.12f), new Vector3(0.08f, 0.12f, 0.08f), fur, false);
				Prim(PrimitiveType.Cylinder, "Leg", v, new Vector3(side * 0.12f, 0.1f, -0.25f), new Vector3(0.08f, 0.12f, 0.08f), fur, false);
			}

			Eyes(v, 0.67f, 0.46f, 0.09f, 0.06f);

			PointLight(v, new Vector3(0, 0.6f, 0), new Color(1f, 0.6f, 0.3f), 5f, 1.5f);

			return SaveCompanion(root);
		}

		#endregion

		private static void BuildHomeLevels()
		{
			Database.HomeLevels = new List<HomeLevel>
			{
				new() { PlotCells = 8, RequiredCompanions = 0 },
				new() { PlotCells = 12, RequiredCompanions = 1, Cost = Stacks(("timber", 12), ("stone", 10), ("crystal", 2)) },
				new() { PlotCells = 16, RequiredCompanions = 3, Cost = Stacks(("timber", 20), ("stone", 16), ("crystal", 6), ("clay", 6)) },
			};
		}

		private static void BuildNewGame()
		{
			Database.StartingItems = Stacks(("timber", 8), ("stone", 4), ("reed", 4), ("clay", 2), ("spring_water", 2), ("nettle", 2), ("lavender", 1));
			Database.StartingGlyphs = new List<GlyphDefinition> { glyphs["forma"], glyphs["terra"], glyphs["aer"] };
			Database.StartingRecipes = new List<RecipeDefinition> { recipes["swiftness"], recipes["leaping"] };
		}

		private static List<ItemStack> Stacks(params (string item, int amount)[] entries)
			=> entries.Select(entry => new ItemStack(items[entry.item], entry.amount)).ToList();
	}
}
