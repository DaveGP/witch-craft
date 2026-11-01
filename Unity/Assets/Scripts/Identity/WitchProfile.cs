using System.Text.RegularExpressions;
using Data.Save;
using Persistence.Interfaces;


namespace Identity
{
	using Interfaces;

	// Who the witch is. Her name is saved with each slot and is filled into dialogue wherever a line says {name}.
	public class WitchProfile : IWitchProfile, ISaveParticipant
	{
		public const int MaxLength = 16;

		public const string Token = "{name}";

		private static readonly string[] Suggestions =
		{
			"Hazel", "Juniper", "Morwen", "Tansy", "Elspeth", "Clover", "Saffron", "Briony",
			"Rowan", "Sorrel", "Fennel", "Mabel", "Thistle", "Wynn", "Agatha", "Isolde"
		};

		private string name = "";

		private string pending;

		public string Name => HasName ? name : "Witch";

		public bool HasName => name.Length > 0;

		public void Rename(string newName)
		{
			name = Clean(newName);
		}

		public void SetPendingName(string newName)
		{
			pending = Clean(newName);
		}

		public string Personalise(string text) => string.IsNullOrEmpty(text) ? text : text.Replace(Token, Name);

		/// <summary>
		/// Trims, drops the characters rich text would read as markup, and keeps it short enough for the HUD.
		/// </summary>
		public static string Clean(string raw)
		{
			if (string.IsNullOrWhiteSpace(raw))
				return "";

			// Whole tags, and a tag cut off by the length limit
			var cleaned = Regex.Replace(raw, "<[^>]*(>|$)", "");
			cleaned = Regex.Replace(cleaned, "[<>{}]", "");
			cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

			return cleaned.Length > MaxLength ? cleaned[..MaxLength].Trim() : cleaned;
		}

		public static string Suggest(string avoid = null)
		{
			string pick;

			do
				pick = Suggestions[UnityEngine.Random.Range(0, Suggestions.Length)];
			while (pick == avoid);

			return pick;
		}

		public void Read(SaveData data)
		{
			name = Clean(data.WitchName);

			if (name.Length == 0 && !string.IsNullOrEmpty(pending))
				name = pending;

			pending = null;
		}

		public void Write(SaveData data)
		{
			data.WitchName = name;
		}
	}
}
