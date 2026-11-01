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
