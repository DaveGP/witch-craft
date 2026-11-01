namespace Persistence.Interfaces
{
	public interface ISaveService
	{
		/// <summary>
		/// Registers a participant and immediately reads the current save into it.
		/// </summary>
		void Register(ISaveParticipant participant);

		void Unregister(ISaveParticipant participant);

		void Save();

		/// <summary>
		/// Wipes the save and resets every participant to new game state.
		/// </summary>
		void NewGame();
	}
}
