using System.Linq;
using Art;
using Data.ScriptableObjects;
using UnityEditor;
using UnityEngine;
using static WitchCraftEditor.WitchCraftBuildKit;


namespace WitchCraftEditor
{
	// Gives the island BOXOPHOBIC's Polyverse Skies, through the Art Set: a clear blue day with a warm horizon, a
	// pink-and-blue dusk and a starry deep-blue night, which the day cycle blends between. The day preset's horizon
	// is a hard neon-yellow band, so the day is a copy of it with a pale, softly blended glow, saved beside the pack.
	// Only empty slots are filled, so hand-picked skies are never replaced.
	public static class PolyverseSkySetup
	{
		private const string Presets = "Assets/ThirdParty/AssetStore/BOXOPHOBIC/Polyverse Skies/Core/Presets";

		private const string TunedFolder = "Assets/ThirdParty/AssetStore/BOXOPHOBIC/WitchCraft";

		public static bool PackPresent => AssetDatabase.IsValidFolder(Presets);

		private static readonly (string key, string preset)[] skies =
		{
			("sky:day", null),
			("sky:dusk", "Sky 04"),
			("sky:night", "Sky 01"),
		};

		[MenuItem("Witch Craft/Art/Set Up Polyverse Skies", false, 41)]
		private static void Menu()
		{
			if (!Run())
				EditorUtility.DisplayDialog("Polyverse Skies", $"No Polyverse Skies presets at {Presets}.", "OK");
		}

		/// <summary>
		/// Fills the empty sky slots from the pack's presets. False when the pack isn't on this machine.
		/// </summary>
		public static bool Run()
		{
			if (!PackPresent)
				return false;

			var set = LoadOrCreate<ArtSet>(ArtSetPath);
			var filled = 0;

			foreach (var (key, preset) in skies)
			{
				var entry = set.Entries.FirstOrDefault(existing => existing.Key == key);

				if (entry == null)
				{
					entry = new ArtEntry { Key = key };
					set.Entries.Add(entry);
				}

				// The untuned day preset counts as empty, for Art Sets filled before the day was softened
				if (entry.Material != null && !(preset == null && entry.Material.name == "Sky 02"))
					continue;

				entry.Material = preset != null ? AssetDatabase.LoadAssetAtPath<Material>($"{Presets}/{preset}.mat") : DaySky();
				filled += entry.Material != null ? 1 : 0;
			}

			set.Entries = set.Entries.OrderBy(entry => entry.Key).ToList();
			set.NotifyChanged();
			EditorUtility.SetDirty(set);
			AssetDatabase.SaveAssets();

			Debug.Log($"{nameof(PolyverseSkySetup)}: filled {filled} sky slots.");

			return true;
		}

		// Sky 02's clear blue, with a pale gold glow on the horizon that fades softly up into it
		private static Material DaySky()
		{
			var source = AssetDatabase.LoadAssetAtPath<Material>($"{Presets}/Sky 02.mat");

			if (source == null)
				return null;

			EnsureFolder(TunedFolder);

			var path = $"{TunedFolder}/Sky Day.mat";
			var day = AssetDatabase.LoadAssetAtPath<Material>(path);

			if (day == null)
			{
				day = new Material(source);
				AssetDatabase.CreateAsset(day, path);
			}
			else
				day.CopyPropertiesFromMaterial(source);

			// The shader fades the glow by pow(height above the horizon, 1 - smoothness): a wide band, gently feathered
			day.SetColor("_SkyColor", new Color(0.24f, 0.5f, 0.95f));
			day.SetColor("_EquatorColor", new Color(1f, 0.88f, 0.7f));
			day.SetColor("_GroundColor", new Color(0.62f, 0.78f, 0.95f));
			day.SetFloat("_EquatorHeight", 0.75f);
			day.SetFloat("_EquatorSmoothness", 0.45f);
			EditorUtility.SetDirty(day);

			return day;
		}
	}
}
