using Data.Save;

namespace Persistence.Interfaces
{
	public interface ISaveParticipant
	{
		/// <summary>
		/// Restores state from save data. Called on registration and on a new game.
		/// </summary>
		void Read(SaveData data);

		/// <summary>
		/// Writes current state into save data before it is written to disk.
		/// </summary>
		void Write(SaveData data);
	}
}
