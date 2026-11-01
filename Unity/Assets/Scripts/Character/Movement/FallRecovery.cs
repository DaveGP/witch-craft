using Character.Movement.Interfaces;
using DependencyInjection;
using Events.Interfaces;
using UnityEngine;
using World;


namespace Character.Movement
{
	// Remembers the last solid ground the witch stood on and puts her back there after a fall into the sea or sky.
	public class FallRecovery : MonoBehaviour
	{
		[SerializeField] private float killHeight = -12f;

		[SerializeField] private float sampleInterval = 0.5f;

		private IMovementController movementController;

		private IEventBusSender eventBus;

		private Vector3 safePosition;

		private float nextSample;

		private void Awake()
		{
			movementController = GetComponent<IMovementController>();

			eventBus = ServiceLocator.Get<IEventBusSender>();

			if (movementController == null)
				Debug.LogError($"{nameof(movementController)} is null");

			safePosition = transform.position;
		}

		private void FixedUpdate()
		{
			if (transform.position.y < killHeight)
			{
				Recover();
				return;
			}

			if (Time.time < nextSample || !movementController.IsGrounded)
				return;

			nextSample = Time.time + sampleInterval;

			// Moving platforms are not safe to respawn on, they will have moved
			if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out var hit, 1.5f, LayerMask.GetMask("Ground", "Environment"), QueryTriggerInteraction.Ignore)
			    && hit.collider.GetComponentInParent<MovingPlatform>() == null)
				safePosition = transform.position;
		}

		private void Recover()
		{
			movementController.Teleport(safePosition + Vector3.up * 0.5f);

			eventBus.Notify("Whoops! A little magic carries you back.");
		}
	}
}
