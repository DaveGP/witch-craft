namespace Persistence.Interfaces
{
	public interface ISaveService
	{
		int SlotCount { get; }

		int CurrentSlot { get; }

		/// <summary>
		/// Off while the title screen is up, so browsing slots never overwrites one.
		/// </summary>
		bool SavingEnabled { get; set; }

		/// <summary>
		/// Registers a participant and immediately reads the current save into it.
		/// </summary>
		void Register(ISaveParticipant participant);

		void Unregister(ISaveParticipant participant);

		void Save();

		/// <summary>
		/// Wipes the current slot and resets every participant to new game state.
		/// </summary>
		void NewGame();

		/// <summary>
		/// Makes a slot current and reads its save (or a fresh game) into every participant.
		/// </summary>
		void SelectSlot(int slot);

		void Erase(int slot);

		SlotSummary Describe(int slot);
	}

	public readonly struct SlotSummary
	{
		public readonly bool Exists;

		public readonly int Day;

		public readonly int HomeLevel;

		public readonly int Companions;

		public readonly int Spells;

		public readonly System.DateTime LastPlayed;

		/// <summary>
		/// Empty for saves made before witches had names.
		/// </summary>
		public readonly string WitchName;

		public SlotSummary(bool exists, int day, int homeLevel, int companions, int spells, System.DateTime lastPlayed, string witchName)
		{
			WitchName = witchName ?? "";
			Exists = exists;
			Day = day;
			HomeLevel = homeLevel;
			Companions = companions;
			Spells = spells;
			LastPlayed = lastPlayed;
		}
	}
}
