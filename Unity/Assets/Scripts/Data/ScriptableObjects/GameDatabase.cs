using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Data.ScriptableObjects
{
	// Lives in Resources so the Bootstrapper can find it before any scene loads
	[CreateAssetMenu(fileName = "GameDatabase", menuName = "Witch Craft/Game Database", order = 0)]
	public class GameDatabase : ScriptableObject
	{
		public const string ResourcePath = "GameDatabase";

		public List<ItemDefinition> Items = new();

		public List<GlyphDefinition> Glyphs = new();

		public List<SpellDefinition> Spells = new();

		public List<RecipeDefinition> Recipes = new();

		public List<BuildPieceDefinition> BuildPieces = new();

		public List<CompanionDefinition> Companions = new();

		public List<HomeLevel> HomeLevels = new();

		public List<StoryBookDefinition> Books = new();

		[Header("New game")]
		public List<ItemStack> StartingItems = new();

		public List<GlyphDefinition> StartingGlyphs = new();

		public List<RecipeDefinition> StartingRecipes = new();

		public ItemDefinition FailedBrew;

		[Header("Shared materials")]
		public Material ParticleMaterial;

		public Material GhostMaterial;

		public Material GlowMaterial;

		public ItemDefinition FindItem(string id) => Items.FirstOrDefault(item => item.Id == id);

		public GlyphDefinition FindGlyph(string id) => Glyphs.FirstOrDefault(glyph => glyph.Id == id);

		public SpellDefinition FindSpell(string id) => Spells.FirstOrDefault(spell => spell.Id == id);

		public RecipeDefinition FindRecipe(string id) => Recipes.FirstOrDefault(recipe => recipe.Id == id);

		public BuildPieceDefinition FindBuildPiece(string id) => BuildPieces.FirstOrDefault(piece => piece.Id == id);

		public StoryBookDefinition FindBook(string id) => Books.FirstOrDefault(book => book.Id == id);

		public CompanionDefinition FindCompanion(string id) => Companions.FirstOrDefault(companion => companion.Id == id);
	}
}
