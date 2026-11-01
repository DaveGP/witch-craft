using Character.Interfaces;
using DependencyInjection;
using Events.Interfaces;


namespace LocalPlayer
{
	using Interfaces;


	public class PlayerContext : IPlayerContext
	{
		private readonly IEventBusSender eventBusSender;

		public IObserverTarget ActiveTarget { get; private set; }

		public bool ScrollCaptured { get; set; }

		public PlayerContext()
		{
			eventBusSender = ServiceLocator.Get<IEventBusSender>();
		}

		public void SetActiveTarget(IObserverTarget target)
		{
			if (ActiveTarget == target)
				return;

			ActiveTarget = target;

			if (ActiveTarget == null)
				return;

			eventBusSender.SetActiveTarget(ActiveTarget);
		}
	}
}
