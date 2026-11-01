using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Data.Save;
using Data.ScriptableObjects;
using SystemManagement.Interfaces;
using UnityEngine;


namespace Persistence
{
	using Interfaces;

	// Three save slots as JSON files in persistentDataPath (browser storage on the web). Slot one keeps the
	// original single-save file name, so saves from before slots existed carry straight over.
	public class SaveService : ISaveService, IUpdateSystem
	{
		private const float AutosaveInterval = 120f;

		private const string SlotKey = "WitchCraft.Slot";

		public int SlotCount => 3;

		public int CurrentSlot { get; private set; }

		public bool SavingEnabled { get; set; }

		private readonly GameDatabase database;

		private readonly List<ISaveParticipant> participants = new();

		private SaveData data;

		private float nextAutosave;

		public SaveService(GameDatabase database)
		{
			this.database = database;

			CurrentSlot = Mathf.Clamp(PlayerPrefs.GetInt(SlotKey, 0), 0, SlotCount - 1);

			data = Load(CurrentSlot) ?? CreateNew();

			nextAutosave = Time.realtimeSinceStartup + AutosaveInterval;
		}

		private static string PathFor(int slot)
			=> Path.Combine(Application.persistentDataPath, slot == 0 ? "witchcraft_save.json" : $"witchcraft_save_{slot + 1}.json");

		public void Register(ISaveParticipant participant)
		{
			if (!participants.Contains(participant))
				participants.Add(participant);

			participant.Read(data);
		}

		public void Unregister(ISaveParticipant participant)
		{
			// Scene objects leave on unload: capture their state first so nothing is lost
			participant.Write(data);

			participants.Remove(participant);
		}

		public void Save()
		{
			if (!SavingEnabled)
				return;

			foreach (var participant in participants)
				participant.Write(data);

			try
			{
				File.WriteAllText(PathFor(CurrentSlot), JsonUtility.ToJson(data, true));
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		public void NewGame()
		{
			Erase(CurrentSlot);

			data = CreateNew();

			ReadAll();
		}

		public void SelectSlot(int slot)
		{
			CurrentSlot = Mathf.Clamp(slot, 0, SlotCount - 1);

			PlayerPrefs.SetInt(SlotKey, CurrentSlot);
			PlayerPrefs.Save();

			data = Load(CurrentSlot) ?? CreateNew();

			ReadAll();
		}

		public void Erase(int slot)
		{
			var path = PathFor(slot);

			if (File.Exists(path))
				File.Delete(path);
		}

		public SlotSummary Describe(int slot)
		{
			var path = PathFor(slot);

			var saved = Load(slot);

			if (saved == null)
				return default;

			return new SlotSummary(true, saved.Day, saved.Home.Level, saved.Companions.Count, saved.Spells.Count, File.GetLastWriteTime(path));
		}

		public void RunUpdate()
		{
			if (Time.realtimeSinceStartup < nextAutosave)
				return;

			nextAutosave = Time.realtimeSinceStartup + AutosaveInterval;

			Save();
		}

		private void ReadAll()
		{
			foreach (var participant in participants.ToList())
				participant.Read(data);
		}

		private static SaveData Load(int slot)
		{
			var path = PathFor(slot);

			if (!File.Exists(path))
				return null;

			try
			{
				return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
			}
			catch (Exception exception)
			{
				Debug.LogWarning($"SaveService: Could not read slot {slot + 1}, treating it as empty. {exception.Message}");
				return null;
			}
		}

		private SaveData CreateNew()
		{
			var fresh = new SaveData();

			foreach (var stack in database.StartingItems.Where(stack => stack.Item != null))
				fresh.Items.Add(new ItemCount { Id = stack.Item.Id, Amount = stack.Amount });

			fresh.Glyphs.AddRange(database.StartingGlyphs.Where(glyph => glyph != null).Select(glyph => glyph.Id));

			fresh.Recipes.AddRange(database.StartingRecipes.Where(recipe => recipe != null).Select(recipe => recipe.Id));

			return fresh;
		}
	}
}
