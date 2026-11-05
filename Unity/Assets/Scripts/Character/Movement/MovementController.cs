using System;
using Character.Movement.Interfaces;
using Data.Enums;
using Data.ScriptableObjects;
using DependencyInjection;
using Potions.Interfaces;
using UnityEngine;
using World;


namespace Character.Movement
{
	// Rigidbody platformer movement: accelerated camera relative running, coyote time, buffered and
	// variable height jumps, air jumps and a heavier fall for a snappy arc.
	public class MovementController : MonoBehaviour, IMovementController, ICharacterMovementState
	{
		public Vector3 Velocity => rigidbody.linearVelocity;

		public Vector3 LocalVelocity => transform.InverseTransformDirection(rigidbody.linearVelocity);

		public bool IsGrounded { get; private set; }

		public CharacterGait Gait { get; private set; }

		public MovementMode Mode => MovementMode.Grounded;

		public bool Suspended { get; set; }

		public CharacterStats Stats => stats;

		public bool IsFloating => Time.time < floatUntil;

		public event Action Jumped;

		public event Action Landed;

		[SerializeField] private CharacterStats stats;

		[SerializeField] private float groundCheckDistance = 0.15f;

		private Rigidbody rigidbody;

		private CapsuleCollider capsule;

		private IPotionEffects potionEffects;

		private LayerMask groundMask;

		private float groundCheckRadius => capsule.radius * 0.9f;

		private float lastGroundedTime = float.NegativeInfinity;

		private float lastJumpPressedTime = float.NegativeInfinity;

		private bool jumpHeld;

		// Thrown by a bounce mushroom: the arc is the mushroom's, so letting go of jump doesn't cut it short
		private bool launched;

		private bool isJumping;

		private int airJumpsUsed;

		private Quaternion desiredRotation;

		private MovingPlatform groundPlatform;

		private LayerMask waterMask;

		private float floatUntil;

		private float floatRise;


		private void Awake()
		{
			rigidbody = GetComponent<Rigidbody>();
			capsule = GetComponent<CapsuleCollider>();

			potionEffects = ServiceLocator.Get<IPotionEffects>();

			if (stats == null)
				Debug.LogError($"{nameof(stats)} is null");

			if (potionEffects == null)
				Debug.LogError($"{nameof(potionEffects)} is null");

			groundMask = LayerMask.GetMask("Ground", "Environment");
			waterMask = LayerMask.GetMask("Water");

			rigidbody.useGravity = false;

			desiredRotation = transform.rotation;
		}

		private void FixedUpdate()
		{
			UpdateGrounded();

			if (Suspended)
				return;

			UpdateJump();

			ApplyGravity();

			ApplyRotation();
		}

		public void SetGait(CharacterGait gait)
		{
			Gait = gait;
		}

		public void Move(Vector3 direction)
		{
			if (Suspended)
				return;

			var platformVelocity = PlatformVelocity;

			var velocity = rigidbody.linearVelocity;

			var horizontal = new Vector3(velocity.x, 0f, velocity.z) - new Vector3(platformVelocity.x, 0f, platformVelocity.z);

			var target = direction * CalculateMoveSpeed(Gait);

			var acceleration = IsGrounded ? stats.Acceleration : stats.AirAcceleration;

			horizontal = Vector3.MoveTowards(horizontal, target, acceleration * Time.fixedDeltaTime);

			velocity.x = horizontal.x + platformVelocity.x;
			velocity.z = horizontal.z + platformVelocity.z;

			rigidbody.linearVelocity = velocity;

			if (direction.sqrMagnitude > 0.01f)
				Face(direction);
		}

		private float CalculateMoveSpeed(CharacterGait characterGait)
		{
			var speed = characterGait switch
			{
				CharacterGait.Walk => stats.WalkSpeed,
				CharacterGait.Run => stats.RunSpeed,
				CharacterGait.Sprint => stats.SprintSpeed,
				_ => 0
			};

			return speed * potionEffects.SpeedMultiplier;
		}

		public void Jump()
		{
			lastJumpPressedTime = Time.time;
		}

		public void SetJumpHeld(bool held)
		{
			jumpHeld = held;
		}

		public void Face(Vector3 direction)
		{
			direction.y = 0f;

			if (direction.sqrMagnitude < 0.0001f)
				return;

			desiredRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
		}

		public void Launch(Vector3 velocity, bool restoreAirJumps, bool fullArc = false)
		{
			rigidbody.linearVelocity = velocity;

			launched = fullArc;

			isJumping = true;

			lastGroundedTime = float.NegativeInfinity;

			if (restoreAirJumps)
				airJumpsUsed = 0;
		}

		public void Float(float seconds, float riseSpeed)
		{
			floatUntil = Time.time + seconds;
			floatRise = riseSpeed;

			// Counts as being in the air, so the ground check doesn't cancel it on take-off
			isJumping = true;
			lastGroundedTime = float.NegativeInfinity;
		}

		public void StopFloating()
		{
			floatUntil = 0f;
		}

		public void Teleport(Vector3 position)
		{
			rigidbody.position = position;
			transform.position = position;
			rigidbody.linearVelocity = Vector3.zero;
		}

		private Vector3 PlatformVelocity => groundPlatform != null && IsGrounded ? groundPlatform.Velocity : Vector3.zero;

		private void UpdateJump()
		{
			if (Time.time - lastJumpPressedTime > stats.JumpBufferTime)
				return;

			// Jumping inside a bubble pops it, and the jump becomes an air jump from there
			if (IsFloating)
				StopFloating();

			var canGroundJump = !isJumping && Time.time - lastGroundedTime <= stats.CoyoteTime;

			if (canGroundJump)
			{
				PerformJump(stats.JumpHeight);
				return;
			}

			if (airJumpsUsed < stats.AirJumps + potionEffects.ExtraAirJumps)
			{
				airJumpsUsed++;
				PerformJump(stats.AirJumpHeight);
			}
		}

		private void PerformJump(float height)
		{
			launched = false;

			var velocity = rigidbody.linearVelocity;

			velocity.y = Mathf.Sqrt(height * potionEffects.JumpMultiplier * 2f * stats.Gravity) + Mathf.Max(0f, PlatformVelocity.y);

			rigidbody.linearVelocity = velocity;

			isJumping = true;

			lastJumpPressedTime = float.NegativeInfinity;

			lastGroundedTime = float.NegativeInfinity;

			Jumped?.Invoke();
		}

		private void ApplyGravity()
		{
			var velocity = rigidbody.linearVelocity;

			if (IsFloating)
			{
				velocity.y = Mathf.MoveTowards(velocity.y, floatRise, stats.Gravity * Time.fixedDeltaTime);
				rigidbody.linearVelocity = velocity;
				return;
			}

			if (IsGrounded && !isJumping)
			{
				// Stick to slopes and ride platforms instead of bouncing off them
				velocity.y = Mathf.Max(velocity.y, -2f);

				if (groundPlatform != null)
					velocity.y = groundPlatform.Velocity.y;

				rigidbody.linearVelocity = velocity;
				return;
			}

			var gravity = stats.Gravity;

			if (velocity.y < 0f)
				gravity *= stats.FallGravityMultiplier * potionEffects.FallGravityMultiplier;
			else if (!jumpHeld && !launched)
				gravity *= stats.LowJumpGravityMultiplier;

			if (velocity.y < 0f)
				launched = false;

			velocity.y -= gravity * Time.fixedDeltaTime;

			velocity.y = Mathf.Max(velocity.y, -stats.MaxFallSpeed * potionEffects.MaxFallSpeedMultiplier);

			rigidbody.linearVelocity = velocity;
		}

		private void ApplyRotation()
		{
			var rotation = Quaternion.RotateTowards(rigidbody.rotation, desiredRotation, stats.TurnSpeed * Time.fixedDeltaTime);

			rigidbody.MoveRotation(rotation);
		}

		private void UpdateGrounded()
		{
			var wasGrounded = IsGrounded;

			var capsuleCenter = transform.TransformPoint(capsule.center);

			var bottomSphereCentre =
				capsuleCenter -
				transform.up * (capsule.height * 0.5f - capsule.radius);

			var origin = bottomSphereCentre + Vector3.up * 0.05f;

			var rising = rigidbody.linearVelocity.y > 0.5f && isJumping || IsFloating;

			// Seafoam Draught makes the sea solid underfoot
			LayerMask mask = potionEffects.IsActive(PotionEffect.Tidewalk) ? groundMask.value | waterMask.value : groundMask.value;

			var hit = default(RaycastHit);

			IsGrounded = !rising && Physics.SphereCast(
				origin,
				groundCheckRadius,
				Vector3.down,
				out hit,
				groundCheckDistance + 0.05f,
				mask,
				QueryTriggerInteraction.Ignore
			) && hit.normal.y > 0.55f;

			groundPlatform = IsGrounded ? hit.collider.GetComponentInParent<MovingPlatform>() : null;

			if (!IsGrounded)
				return;

			lastGroundedTime = Time.time;

			if (rigidbody.linearVelocity.y <= 0.5f)
			{
				isJumping = false;
				airJumpsUsed = 0;
			}

			if (!wasGrounded)
				Landed?.Invoke();
		}
	}
}
