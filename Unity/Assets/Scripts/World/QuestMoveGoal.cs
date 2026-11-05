using Companions.Interfaces;
using DependencyInjection;
using Events.Interfaces;
using Home;
using UnityEngine;

namespace World
{
	// Something in the way that a quest wants shifted with Levitate. Once it has been set down clear of its spot the
	// goal is met, and on later visits it stays out of the way.
	[RequireComponent(typeof(Movable))]
	public class QuestMoveGoal : MonoBehaviour
	{
		[SerializeField] private string goal;

		[SerializeField] private float clearDistance = 2.5f;

		[SerializeField] private Vector3 restOffset = new(0f, 0f, 3f);

		[SerializeField] private string clearedMessage;

		private IQuestLog quests;

		private Movable movable;

		private Vector3 origin;

		private bool done;

		public string Goal => goal;

		private void Start()
		{
			quests = ServiceLocator.Get<IQuestLog>();
			movable = GetComponent<Movable>();
			origin = transform.position;
		}

		private void Update()
		{
			if (done || movable.IsHeld)
				return;

			// Already shifted on an earlier visit
			if (quests.HasGoal(goal))
			{
				done = true;
				transform.position = origin + restOffset;
				return;
			}

			var moved = transform.position - origin;
			moved.y = 0f;

			if (moved.magnitude < clearDistance)
				return;

			done = true;
			quests.MarkGoal(goal);
			ServiceLocator.Get<IEventBusSender>()?.Notify(clearedMessage);
		}
	}
}
