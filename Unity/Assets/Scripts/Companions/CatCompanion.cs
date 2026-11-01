using Character.Interfaces;
using DependencyInjection;
using Events.Interfaces;
using Interaction.Interfaces;
using UnityEngine;
using Utilities;


namespace Companions
{
	// The witch's familiar. Trots along at her heel, sits when she stops, hops onto the broom when she flies
	// and pops back to her side with a puff of magic if left too far behind. Interact to give her a pet.
	public class CatCompanion : MonoBehaviour, IInteractable
	{
		[SerializeField] private string catName = "Sable";

		[SerializeField] private Transform body;

		[SerializeField] private Transform tail;

		[SerializeField] private Transform head;

		[SerializeField] private float walkSpeed = 4f;

		[SerializeField] private float runSpeed = 9f;

		[SerializeField] private float followDistance = 1.8f;

		[SerializeField] private float teleportDistance = 18f;

		private IEventBus eventBus;

		private IEventBusSender eventSender;

		private IObserverTarget witch;

		private LayerMask groundMask;

		private Vector3 velocity;

		private float idleTime;

		private float stepPhase;

		private bool riding;

		private float purrUntil;

		private Vector3 bodyRestScale;

		public string Prompt => $"Pet {catName}";

		public bool CanInteract => !riding;

		public Vector3 Position => transform.position;

		private void Awake()
		{
			eventBus = ServiceLocator.Get<IEventBus>();
			eventSender = ServiceLocator.Get<IEventBusSender>();

			if (eventBus == null)
				Debug.LogError($"{nameof(eventBus)} is null");

			groundMask = LayerMask.GetMask("Ground", "Environment");

			if (body != null)
				bodyRestScale = body.localScale;

			eventBus.ActiveTargetChanged += SetWitch;
		}

		private void OnDestroy()
		{
			eventBus.ActiveTargetChanged -= SetWitch;
		}

		private void SetWitch(IObserverTarget target)
		{
			witch = target;
		}

		public void Interact(GameObject interactor)
		{
			purrUntil = Time.time + 2.5f;

			FxUtility.Burst(transform.position + Vector3.up * 0.6f, new Color(1f, 0.5f, 0.7f), 12, 1.2f, 0.2f);

			eventSender.Notify(Random.value < 0.5f ? $"{catName} purrs and headbutts your hand." : $"{catName} rolls over for belly rubs. It's a trap.");
		}

		private void Update()
		{
			if (witch == null)
				return;

			if (witch.Broom.IsMounted && witch.BroomSeat != null)
			{
				Ride();
				return;
			}

			if (riding)
				Hop();

			Follow();

			Animate();
		}

		private void Ride()
		{
			if (!riding)
			{
				riding = true;
				FxUtility.Burst(transform.position, new Color(0.7f, 0.6f, 1f), 10, 1.5f);
			}

			transform.SetParent(witch.BroomSeat, false);
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;

			if (body != null)
				body.localScale = new Vector3(bodyRestScale.x, bodyRestScale.y * 0.8f, bodyRestScale.z);

			WagTail(4f);
		}

		private void Hop()
		{
			riding = false;

			transform.SetParent(null, true);
			transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

			SnapToGround();
		}

		private void Follow()
		{
			var witchPosition = witch.Transform.position;

			var toWitch = witchPosition - transform.position;
			toWitch.y = 0f;

			var distance = toWitch.magnitude;

			if (distance > teleportDistance || Mathf.Abs(witchPosition.y - transform.position.y) > 6f && witch.Movement.IsGrounded)
			{
				Teleport();
				return;
			}

			// Aim for a spot just behind and beside the witch rather than her feet
			var heel = witchPosition - witch.Transform.forward * followDistance + witch.Transform.right * 0.7f;

			var toHeel = heel - transform.position;
			toHeel.y = 0f;

			var desired = Vector3.zero;

			if (toHeel.magnitude > 0.4f && distance > followDistance * 0.6f)
			{
				var speed = toHeel.magnitude > 4f ? runSpeed : walkSpeed;
				desired = toHeel.normalized * Mathf.Min(speed, toHeel.magnitude * 3f);
			}

			velocity = Vector3.MoveTowards(velocity, desired, 25f * Time.deltaTime);

			var next = transform.position + velocity * Time.deltaTime;

			// Don't trot off ledges: wait for the witch to come back or catch up by teleport
			if (!Physics.Raycast(next + Vector3.up * 1.5f, Vector3.down, out var hit, 4f, groundMask, QueryTriggerInteraction.Ignore))
			{
				velocity = Vector3.zero;
				return;
			}

			transform.position = new Vector3(next.x, Mathf.Lerp(transform.position.y, hit.point.y, 1f - Mathf.Exp(-20f * Time.deltaTime)), next.z);

			var look = velocity.sqrMagnitude > 0.05f ? velocity : toWitch;

			if (look.sqrMagnitude > 0.01f)
				transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look.normalized, Vector3.up), 1f - Mathf.Exp(-8f * Time.deltaTime));

			idleTime = velocity.sqrMagnitude < 0.05f ? idleTime + Time.deltaTime : 0f;
		}

		private void Teleport()
		{
			FxUtility.Burst(transform.position + Vector3.up * 0.3f, new Color(0.7f, 0.6f, 1f), 16, 2f);

			transform.position = witch.Transform.position - witch.Transform.forward * followDistance;

			SnapToGround();

			velocity = Vector3.zero;

			FxUtility.Burst(transform.position + Vector3.up * 0.3f, new Color(0.7f, 0.6f, 1f), 16, 2f);
		}

		private void SnapToGround()
		{
			if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out var hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
				transform.position = hit.point;
		}

		private void Animate()
		{
			var moving = velocity.magnitude;

			if (body == null)
				return;

			var sitting = idleTime > 1.5f;

			stepPhase += Time.deltaTime * moving * 3.5f;

			var bounce = moving > 0.1f ? Mathf.Abs(Mathf.Sin(stepPhase)) * 0.06f : 0f;

			var purr = Time.time < purrUntil ? Mathf.Sin(Time.time * 40f) * 0.01f : 0f;

			body.localPosition = new Vector3(0f, bodyRestScale.y * 0.5f + bounce + purr, 0f);

			body.localScale = Vector3.Lerp(body.localScale,
				sitting ? new Vector3(bodyRestScale.x, bodyRestScale.y * 1.15f, bodyRestScale.z * 0.75f) : bodyRestScale,
				1f - Mathf.Exp(-6f * Time.deltaTime));

			if (head != null)
				head.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 0.7f) * 8f, Mathf.Sin(Time.time * 0.4f) * 20f, 0f);

			WagTail(moving > 0.1f ? 8f : 2f);
		}

		private void WagTail(float speed)
		{
			if (tail != null)
				tail.localRotation = Quaternion.Euler(-35f, Mathf.Sin(Time.time * speed) * 30f, 0f);
		}
	}
}
