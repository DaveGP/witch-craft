using Character.Movement.Interfaces;
using Data.Enums;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using Potions.Interfaces;
using UnityEngine;
using Utilities;


namespace Character.Movement
{
	// Flying on the broom: takes the rigidbody over from the MovementController while mounted.
	// Flight drains a charge that refills on the ground, so the high places still need some platforming.
	public class BroomController : MonoBehaviour, IBroomController
	{
		public bool IsMounted { get; private set; }

		public bool IsBoosting { get; private set; }

		public BroomStats Stats => stats;

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

		private LayerMask ceilingMask;

		// What the hover ceiling is measured from: the land below, followed up at once but down only gradually, so
		// flying off a hill or the spire is a long glide rather than a sudden sink
		private float ceilingGround;


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

			// The sea counts as something to fly above
			ceilingMask = LayerMask.GetMask("Ground", "Environment", "Water");

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

			ceilingGround = GroundBelow() ?? transform.position.y;

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

			target.y = ApplyCeiling(target.y, vertical > 0f);

			rigidbody.linearVelocity = Vector3.MoveTowards(rigidbody.linearVelocity, target, stats.Acceleration * Time.fixedDeltaTime);

			UpdateFacing(direction);

			Drain();

			// Touching down while diving lands the witch
			if (vertical < 0f && movementController.IsGrounded && Time.time - mountedTime > 0.5f)
				Dismount();
		}

		// A broom skims the land and the sea: it holds its height anywhere under its ceiling. Climbing slows as it
		// nears the ceiling and comes to a gentle stop there (with a shimmer, and a hint the first time), and above it
		// the broom eases back down, so it settles rather than bobbing. Updrafts carry it past
		private float ApplyCeiling(float verticalVelocity, bool climbing)
		{
			var ground = GroundBelow();

			if (ground.HasValue && ground.Value >= ceilingGround)
				ceilingGround = ground.Value;
			else
				ceilingGround = Mathf.MoveTowards(ceilingGround, ground ?? float.NegativeInfinity, stats.CeilingFollowDown * Time.fixedDeltaTime);

			if (Time.time < upliftUntil)
			{
				// Ride the updraft up, and the ceiling with it
				ceilingGround = Mathf.Max(ceilingGround, transform.position.y - hoverCeiling);
				return Mathf.Max(verticalVelocity, stats.VerticalSpeed);
			}

			var excess = transform.position.y - ceilingGround - hoverCeiling;

			// Below the ceiling: full speed until the last few metres, then easing to a stop at it
			if (excess <= 0f)
			{
				if (verticalVelocity <= 0f)
					return verticalVelocity;

				var room = Mathf.Clamp01(-excess / CeilingEaseBand);

				if (climbing && room < 0.1f)
					PushedAgainstCeiling();

				return verticalVelocity * Mathf.SmoothStep(0f, 1f, room);
			}

			if (climbing)
				PushedAgainstCeiling();

			return Mathf.Min(verticalVelocity, -Mathf.Min(excess * 1.5f, stats.GlideSpeed));
		}

		// How far below the ceiling the climb starts to slow
		private const float CeilingEaseBand = 2.5f;

		// Shown once a session, the first time the witch climbs into the ceiling
		private static bool ceilingHinted;

		private float nextShimmer;

		/// <summary>
		/// The witch has climbed into the broom's ceiling at least once this session (for the playtest).
		/// </summary>
		public static bool CeilingHinted => ceilingHinted;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetSession() => ceilingHinted = false;

		// Pushing up against the ceiling: the broom's magic visibly thins, and the first time, a hint says why
		private void PushedAgainstCeiling()
		{
			if (Time.time >= nextShimmer)
			{
				nextShimmer = Time.time + 0.35f;
				FxUtility.Burst(transform.position + Vector3.up * 0.4f, new Color(0.75f, 0.7f, 1f), 8, 0.8f, 0.12f);
			}

			if (ceilingHinted)
				return;

			ceilingHinted = true;
			eventBus.Notify("The broom's magic thins this high. Updrafts can carry you further.");
		}

		private float? GroundBelow() =>
			Physics.Raycast(transform.position, Vector3.down, out var hit, 200f, ceilingMask, QueryTriggerInteraction.Ignore)
				? hit.point.y
				: null;

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
