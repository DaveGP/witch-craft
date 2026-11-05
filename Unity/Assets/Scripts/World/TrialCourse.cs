using Character.Interfaces;
using Character.Movement.Interfaces;
using DependencyInjection;
using Events.Interfaces;
using UnityEngine;
using Utilities;

namespace World
{
	// A long climb with checkpoints. Landing on one remembers it, and a fall to the ground lifts the witch back there
	// rather than sending her to the start. Walking away from the course forgets it.
	public class TrialCourse : MonoBehaviour
	{
		[SerializeField] private Transform[] checkpoints = new Transform[0];

		[SerializeField] private float radius = 16f;

		[SerializeField] private float fallDistance = 3.5f;

		[SerializeField] private string liftMessage;

		private IEventBusSender eventBus;

		private IObserverTarget witch;

		private int reached = -1;

		/// <summary>
		/// The furthest checkpoint landed on, or -1, for the playtest.
		/// </summary>
		public int Reached => reached;

		private void Awake()
		{
			eventBus = ServiceLocator.Get<IEventBusSender>();

			eventBus.ActiveTargetChanged += SetWitch;
		}

		private void OnDestroy()
		{
			eventBus.ActiveTargetChanged -= SetWitch;
		}

		private void SetWitch(IObserverTarget target) => witch = target;

		private void Update()
		{
			if (witch == null)
				return;

			var position = witch.Transform.position;

			if (Flat(position - transform.position).magnitude > radius)
			{
				reached = -1;
				return;
			}

			if (!witch.Movement.IsGrounded)
				return;

			for (var i = reached + 1; i < checkpoints.Length; i++)
				if (Flat(position - checkpoints[i].position).magnitude < 2f && Mathf.Abs(position.y - checkpoints[i].position.y) < 0.8f)
					reached = i;

			if (reached < 0 || position.y > checkpoints[reached].position.y - fallDistance)
				return;

			witch.Transform.GetComponent<IMovementController>()?.Teleport(checkpoints[reached].position + Vector3.up * 0.2f);

			FxUtility.Burst(checkpoints[reached].position + Vector3.up * 0.5f, new Color(0.7f, 1f, 0.7f), 30, 2.5f, 0.15f);

			eventBus.Notify(liftMessage);
		}

		private static Vector3 Flat(Vector3 value) => new(value.x, 0f, value.z);
	}
}
