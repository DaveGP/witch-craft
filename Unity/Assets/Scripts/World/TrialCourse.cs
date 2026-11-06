using Character.Interfaces;
using Character.Movement.Interfaces;
using DependencyInjection;
using Events.Interfaces;
using Potions.Interfaces;
using UnityEngine;
using Utilities;

namespace World
{
	// A long climb with checkpoints. Landing on one remembers it, so after a fall the TrialLift at the foot can carry
	// the witch back up. A fall never moves her by itself: she's free to walk away, to fetch a potion or to give up.
	public class TrialCourse : MonoBehaviour
	{
		[SerializeField] private Transform[] checkpoints = new Transform[0];

		[SerializeField] private float radius = 16f;

		[SerializeField] private float fallDistance = 3.5f;

		[SerializeField] private string fallMessage;

		[Tooltip("Said on landing on the last checkpoint, unless she's already leaping")]
		[SerializeField] private string lastCheckpointHint;

		private IEventBusSender eventBus;

		private IObserverTarget witch;

		private int reached = -1;

		private bool toldOfFall;

		/// <summary>
		/// The furthest checkpoint landed on, or -1.
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
			if (witch == null || !witch.Movement.IsGrounded)
				return;

			var position = witch.Transform.position;

			if (Flat(position - transform.position).magnitude > radius)
				return;

			for (var i = reached + 1; i < checkpoints.Length; i++)
			{
				if (Flat(position - checkpoints[i].position).magnitude >= 2f || Mathf.Abs(position.y - checkpoints[i].position.y) >= 0.8f)
					continue;

				reached = i;
				toldOfFall = false;

				if (i == checkpoints.Length - 1 && ServiceLocator.Get<IPotionEffects>()?.JumpMultiplier <= 1f)
					eventBus.Notify(lastCheckpointHint);
			}

			if (reached < 0 || toldOfFall || position.y > checkpoints[reached].position.y - fallDistance)
				return;

			toldOfFall = true;
			eventBus.Notify(fallMessage);
		}

		/// <summary>
		/// Carries the witch up to the furthest checkpoint she's reached.
		/// </summary>
		public void Lift()
		{
			if (reached < 0 || witch == null)
				return;

			witch.Transform.GetComponent<IMovementController>()?.Teleport(checkpoints[reached].position + Vector3.up * 0.2f);

			FxUtility.Burst(checkpoints[reached].position + Vector3.up * 0.5f, new Color(0.7f, 1f, 0.7f), 30, 2.5f, 0.15f);

			toldOfFall = false;
		}

		private static Vector3 Flat(Vector3 value) => new(value.x, 0f, value.z);
	}
}
