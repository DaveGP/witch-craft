using Character.Interfaces;


namespace LocalPlayer.Interfaces
{
	public interface IPlayerContext
	{
		IObserverTarget ActiveTarget { get; }

		/// <summary>
		/// Set while a spell uses the scroll wheel, so the camera leaves it alone.
		/// </summary>
		bool ScrollCaptured { get; set; }

		void SetActiveTarget(IObserverTarget observerTarget);
	}
}
