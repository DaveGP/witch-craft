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

	public class SaveService : ISaveService, IUpdateSystem
	{
		private const float AutosaveInterval = 120f;

		private static string SavePath => Path.Combine(Application.persistentDataPath, "witchcraft_save.json");

		private readonly GameDatabase database;

		private readonly List<ISaveParticipant> participants = new();

		private SaveData data;

		private float nextAutosave;

		public SaveService(GameDatabase database)
		{
			this.database = database;

			data = Load() ?? CreateNew();

			nextAutosave = Time.realtimeSinceStartup + AutosaveInterval;
		}

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
			foreach (var participant in participants)
				participant.Write(data);

			try
			{
				File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}

		public void NewGame()
		{
			if (File.Exists(SavePath))
				File.Delete(SavePath);

			data = CreateNew();

			foreach (var participant in participants.ToList())
				participant.Read(data);
		}

		public void RunUpdate()
		{
			if (Time.realtimeSinceStartup < nextAutosave)
				return;

			nextAutosave = Time.realtimeSinceStartup + AutosaveInterval;

			Save();
		}

		private static SaveData Load()
		{
			if (!File.Exists(SavePath))
				return null;

			try
			{
				return JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
			}
			catch (Exception exception)
			{
				Debug.LogWarning($"SaveService: Could not read save, starting fresh. {exception.Message}");
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
