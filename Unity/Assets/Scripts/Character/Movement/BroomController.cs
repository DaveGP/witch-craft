using Character.Movement.Interfaces;
using Data.Enums;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using Potions.Interfaces;
using UnityEngine;


namespace Character.Movement
{
	// Flying on the broom: takes the rigidbody over from the MovementController while mounted.
	// Flight drains a charge that refills on the ground, so the high places still need some platforming.
	public class BroomController : MonoBehaviour, IBroomController
	{
		public bool IsMounted { get; private set; }

		public bool IsBoosting { get; private set; }

		public float Charge01 => stats.MaxCharge <= 0f ? 0f : charge / stats.MaxCharge;

		[SerializeField] private BroomStats stats;

		[SerializeField] private GameObject broomVisual;

		[SerializeField] private Transform bankPivot;

		[Tooltip("How high above the ground below the broom can climb on its own. Updrafts lift past it.")]
		[SerializeField] private float hoverCeiling = 7f;

		private Rigidbody rigidbody;

		private IMovementController movementController;

		private IPotionEffects potionEffects;

		private IEventBusSender eventBus;

		private float charge;

		private float bank;

		private float mountedTime;

		private float upliftUntil;

		private LayerMask groundMask;


		private void Awake()
		{
			rigidbody = GetComponent<Rigidbody>();

			movementController = GetComponent<IMovementController>();

			potionEffects = ServiceLocator.Get<IPotionEffects>();

			eventBus = ServiceLocator.Get<IEventBusSender>();

			if (stats == null)
				Debug.LogError($"{nameof(stats)} is null");

			if (movementController == null)
				Debug.LogError($"{nameof(movementController)} is null");

			if (potionEffects == null)
				Debug.LogError($"{nameof(potionEffects)} is null");

			charge = stats.MaxCharge;

			groundMask = LayerMask.GetMask("Ground", "Environment");

			if (broomVisual != null)
				broomVisual.SetActive(false);
		}

		public void Toggle()
		{
			if (IsMounted)
				Dismount();
			else
				Mount();
		}

		private void Mount()
		{
			if (charge < stats.MaxCharge * 0.15f && !potionEffects.IsActive(PotionEffect.Skyward))
			{
				eventBus.Notify("Your broom needs to rest on the ground a moment.");
				return;
			}

			IsMounted = true;

			mountedTime = Time.time;

			movementController.Suspended = true;

			var velocity = rigidbody.linearVelocity;
			velocity.y = Mathf.Max(velocity.y, 3f);
			rigidbody.linearVelocity = velocity;

			if (broomVisual != null)
				broomVisual.SetActive(true);
		}

		private void Dismount()
		{
			IsMounted = false;

			IsBoosting = false;

			movementController.Suspended = false;

			if (broomVisual != null)
				broomVisual.SetActive(false);

			if (bankPivot != null)
				bankPivot.localRotation = Quaternion.identity;
		}

		public void Recharge(float seconds)
		{
			charge = Mathf.Min(stats.MaxCharge, charge + seconds);
		}

		public void Uplift(float seconds)
		{
			upliftUntil = Mathf.Max(upliftUntil, Time.time + seconds);
		}

		public void Fly(Vector3 direction, float vertical, bool boost)
		{
			if (!IsMounted)
				return;

			IsBoosting = boost && direction.sqrMagnitude > 0.1f;

			var speed = IsBoosting ? stats.BoostSpeed : stats.Speed;

			var target = direction * speed + Vector3.up * (vertical * stats.VerticalSpeed);

			// Gentle hover bob when idle so the broom feels alive
			if (target.sqrMagnitude < 0.01f)
				target.y = Mathf.Sin(Time.time * 2f) * 0.3f;

			target.y = ApplyCeiling(target.y);

			rigidbody.linearVelocity = Vector3.MoveTowards(rigidbody.linearVelocity, target, stats.Acceleration * Time.fixedDeltaTime);

			UpdateFacing(direction);

			Drain();

			// Touching down while diving lands the witch
			if (vertical < 0f && movementController.IsGrounded && Time.time - mountedTime > 0.5f)
				Dismount();
		}

		// A broom skims the land: above its ceiling it glides gently down, unless an updraft is carrying it
		private float ApplyCeiling(float verticalVelocity)
		{
			if (Time.time < upliftUntil)
				return Mathf.Max(verticalVelocity, stats.VerticalSpeed);

			var altitude = Physics.Raycast(transform.position, Vector3.down, out var hit, 200f, groundMask, QueryTriggerInteraction.Ignore)
				? transform.position.y - hit.point.y
				: float.PositiveInfinity;

			if (altitude <= hoverCeiling)
				return verticalVelocity;

			return Mathf.Min(verticalVelocity, -2.5f);
		}

		private void UpdateFacing(Vector3 direction)
		{
			var flat = Vector3.ProjectOnPlane(direction, Vector3.up);

			var turn = 0f;

			if (flat.sqrMagnitude > 0.01f)
			{
				var desired = Quaternion.LookRotation(flat.normalized, Vector3.up);

				turn = Vector3.SignedAngle(transform.forward, flat, Vector3.up);

				rigidbody.MoveRotation(Quaternion.RotateTowards(rigidbody.rotation, desired, stats.TurnSpeed * Time.fixedDeltaTime));
			}

			if (bankPivot == null)
				return;

			bank = Mathf.Lerp(bank, Mathf.Clamp(-turn, -stats.BankAngle, stats.BankAngle), 1f - Mathf.Exp(-6f * Time.fixedDeltaTime));

			var pitch = Mathf.Clamp(-rigidbody.linearVelocity.y * 2f, -20f, 20f);

			bankPivot.localRotation = Quaternion.Euler(pitch, 0f, bank);
		}

		private void Drain()
		{
			if (potionEffects.IsActive(PotionEffect.Skyward))
				return;

			charge -= Time.fixedDeltaTime * (IsBoosting ? stats.BoostDrainMultiplier : 1f);

			if (charge > 0f)
				return;

			charge = 0f;

			eventBus.Notify("Your broom sputters out of magic!");

			Dismount();
		}

		private void FixedUpdate()
		{
			if (IsMounted || !movementController.IsGrounded)
				return;

			Recharge(stats.RechargeRate * Time.fixedDeltaTime);
		}
	}
}
