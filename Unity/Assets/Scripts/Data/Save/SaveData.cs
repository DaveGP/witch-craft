using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data.Save
{
	[Serializable]
	public class SaveData
	{
		public int Version = 1;

		public List<ItemCount> Items = new();

		public List<string> Glyphs = new();

		public List<string> Spells = new();

		public List<string> SpellSlots = new();

		public List<string> Recipes = new();

		public List<CompanionRecord> Companions = new();

		public HomeRecord Home = new();

		public int TutorialStep;

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
	}
}
