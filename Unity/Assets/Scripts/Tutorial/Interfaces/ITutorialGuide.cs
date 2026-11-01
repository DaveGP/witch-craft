namespace Tutorial.Interfaces
{
	public interface ITutorialGuide
	{
		bool IsActive { get; }

		/// <summary>
		/// Ends the guided introduction for players who know the ropes.
		/// </summary>
		void Skip();
	}
}
