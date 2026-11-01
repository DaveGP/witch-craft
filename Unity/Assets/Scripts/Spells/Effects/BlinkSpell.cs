using Audio;
using Audio.Interfaces;
using Character.Movement.Interfaces;
using Data.Enums;
using DependencyInjection;
using UnityEngine;
using Utilities;

namespace Spells.Effects
{
	// Astra + Motus. A star's quick step: the witch vanishes and reappears a few metres ahead, stopping short of
	// walls. Once per jump in the air, so it stretches a jump rather than replacing flight.
	public class BlinkSpell : SpellEffect
	{
		public override SpellEffectType Type => SpellEffectType.Blink;

		public override bool IsAiming => false;

		public override string Status => usedInAir ? "Blink: land to blink again" : "Blink: click to step through the air";

		[SerializeField] private float distance = 7f;

		[SerializeField] private float cooldown = 0.6f;

		private IMovementController movement;

		private Rigidbody body;

		private CapsuleCollider capsule;

		private bool usedInAir;

		private float readyTime;

		protected override void Awake()
		{
			base.Awake();

			movement = GetComponent<IMovementController>();
			body = GetComponent<Rigidbody>();
			capsule = GetComponent<CapsuleCollider>();
		}

		private void FixedUpdate()
		{
			if (movement.IsGrounded)
				usedInAir = false;
		}

		private const float StepUp = 1.6f;

		private float Sweep(Vector3 from, Vector3 direction, LayerMask solid)
			=> Physics.SphereCast(from, capsule.radius * 0.9f, direction, out var hit, distance, solid, QueryTriggerInteraction.Ignore)
				? Mathf.Max(0f, hit.distance - 0.2f)
				: distance;

		public override void Cast()
		{
			if (Time.time < readyTime || usedInAir && !movement.IsGrounded)
				return;

			readyTime = Time.time + cooldown;

			if (!movement.IsGrounded)
				usedInAir = true;

			var direction = CameraController != null ? CameraController.Forward : transform.forward;

			var start = body.position;

			var solid = LayerMask.GetMask("Ground", "Environment");

			// Sweep at chest height so rising ground underfoot doesn't stop the blink; walls still do
			var chest = start + Vector3.up * (capsule.height * 0.65f);

			var travel = Sweep(chest, direction, solid);

			var lift = 0f;

			// Blocked by a hillside rather than a wall? Try again a little higher and go up and over
			if (travel < distance - 0.5f)
			{
				var higher = Sweep(chest + Vector3.up * StepUp, direction, solid);

				if (higher > travel + 0.5f)
				{
					travel = higher;
					lift = StepUp;
				}
			}

			var end = start + direction * travel + Vector3.up * lift;

			// Then settle onto any slope at the far end rather than inside it
			if (Physics.Raycast(end + Vector3.up * 1.6f, Vector3.down, out var ground, 1.8f + lift, solid, QueryTriggerInteraction.Ignore) && ground.point.y > end.y - lift)
				end.y = ground.point.y + 0.05f;

			FxUtility.Burst(start + Vector3.up, SpellColour, 30, 2.5f, 0.18f);

			body.position = end;
			transform.position = end;

			// Keep the run going, but never blink into a fall
			var velocity = body.linearVelocity;
			velocity.y = Mathf.Max(0f, velocity.y);
			body.linearVelocity = velocity;

			movement.Face(direction);

			FxUtility.Burst(end + Vector3.up, SpellColour, 30, 2.5f, 0.18f);

			ServiceLocator.Get<IAudioService>()?.Play(Sound.Whoosh, 0.5f, 0.1f);
		}
	}
}
