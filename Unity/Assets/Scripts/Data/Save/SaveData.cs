using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data.Save
{
	[Serializable]
	public class SaveData
	{
		public int Version = 1;

		public string WitchName = "";

		public List<ItemCount> Items = new();

		public List<string> Glyphs = new();

		public List<string> Spells = new();

		public List<string> SpellSlots = new();

		public List<string> Recipes = new();

		public List<CompanionRecord> Companions = new();

		public HomeRecord Home = new();

		public int TutorialStep;

		public List<string> Books = new();

		public List<QuestRecord> Quests = new();

		public List<string> QuestGoals = new();

		public string FollowedQuest = "";

		public float Hour = 8f;

		public int Day = 1;
	}

	[Serializable]
	public class ItemCount
	{
		public string Id;

		public int Amount;
	}

	[Serializable]
	public class QuestRecord
	{
		public string Id;

		public int Step;
	}

	[Serializable]
	public class CompanionRecord
	{
		public string Id;

		public int Friendship;

		public List<int> ClaimedRewards = new();
	}

	[Serializable]
	public class HomeRecord
	{
		public int Level = 1;

		public bool Seeded;

		/// <summary>
		/// Saves from before storybooks existed get a bookcase added to the hut once.
		/// </summary>
		public bool BookcaseGiven;

		/// <summary>
		/// Saves from before starter pieces were flagged get their starter hut pieces flagged once.
		/// </summary>
		public bool StartersMarked;

		public List<PieceRecord> Pieces = new();
	}

	[Serializable]
	public class PieceRecord
	{
		public string Id;

		public Vector3 Position;

		public float Yaw;

		/// <summary>
		/// Index into HomePalette. 0 is the material's natural colour.
		/// </summary>
		public int Colour;

		/// <summary>
		/// Part of the hut the witch was given rather than built, so Unmake returns only half its materials.
		/// </summary>
		public bool Starter;
	}
}
