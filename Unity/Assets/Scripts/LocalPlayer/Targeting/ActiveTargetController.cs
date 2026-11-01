using Character.Interfaces;
using LocalPlayer.Interfaces;
using DependencyInjection;
using UnityEngine;

namespace LocalPlayer.Targeting
{
	public class ActiveTargetController : IActiveTargetController
	{
		private readonly IPlayerContext playerContext;

		public ActiveTargetController()
		{
			playerContext = ServiceLocator.Get<IPlayerContext>();

			if (playerContext == null)
				Debug.LogError($"{nameof(playerContext)} is null");
		}

		public void SetActiveTarget(IObserverTarget observerTarget)
		{
			if (playerContext.ActiveTarget == observerTarget)
				return;

			playerContext.SetActiveTarget(observerTarget);
		}
	}
}
