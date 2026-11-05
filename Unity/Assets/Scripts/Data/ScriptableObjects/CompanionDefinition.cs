using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data.ScriptableObjects
{
	[CreateAssetMenu(fileName = "Companion", menuName = "Witch Craft/Companion", order = 15)]
	public class CompanionDefinition : ScriptableObject
	{
		public string Id;

		public string DisplayName;

		public string Species;

		public Color Color = Color.white;

		public GameObject Prefab;

		[Tooltip("Awake at night and asleep by day")]
		public bool Nocturnal;

		[TextArea] public string Greeting;

		[Tooltip("What they say they need before moving in")]
		[TextArea] public string Plea;

		public ItemStack RecruitGift = new();

		[TextArea] public string MoveInLine;

		[Tooltip("The storybook that leads to them: they can't be found until it's been read. Empty means they're there from the start")]
		public StoryBookDefinition RevealBook;

		[Tooltip("Where to look, finishing \"Find Bramble ...\"")]
		public string Whereabouts;

		[Tooltip("What they ask of the witch, in order, before moving in. Empty means the recruit gift alone")]
		public List<QuestStep> Quests = new();

		[Tooltip("What they say when she's back with a step done")]
		[TextArea] public string Expectant;

		public List<string> ChatLines = new();

		public List<ItemDefinition> LikedGifts = new();

		[Tooltip("A piece of furniture that makes them especially happy to have at home")]
		public BuildPieceDefinition FavouriteFurniture;

		public List<FriendshipReward> Rewards = new();
	}

	[Serializable]
	public class FriendshipReward
	{
		public int Friendship;

		public RecipeDefinition Recipe;

		public GlyphDefinition Glyph;

		[TextArea] public string Line;
	}
}
