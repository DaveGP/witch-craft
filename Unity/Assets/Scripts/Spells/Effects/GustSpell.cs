using Character.Movement.Interfaces;
using Data.Enums;
using UnityEngine;
using Utilities;

namespace Spells.Effects
{
	// Aer + Motus. A burst of wind that flings the witch forward: a long leap on the ground, an air dash
	// once per jump, and a speed burst on the broom. Also shoves loose objects away.
	public class GustSpell : SpellEffect
	{
		public override SpellEffectType Type => SpellEffectType.Gust;

		public override bool IsAiming => false;

		public override string Status => usedInAir
			? "Gust: land to call the wind again"
			: "Gust: click to dash on the wind (once per jump)";

		[SerializeField] private float dashSpeed = 14f;

		[SerializeField] private float groundLift = 7f;

		[SerializeField] private float airLift = 4.5f;

		[SerializeField] private float pushRadius = 5f;

		[SerializeField] private float pushForce = 9f;

		[SerializeField] private float cooldown = 0.6f;

		private IMovementController movement;

		private IBroomState broom;

		private Rigidbody body;

		private bool usedInAir;

		private float readyTime;

		protected override void Awake()
		{
			base.Awake();

			movement = GetComponent<IMovementController>();
			broom = GetComponent<IBroomState>();
			body = GetComponent<Rigidbody>();
		}

		private void FixedUpdate()
		{
			if (movement.IsGrounded)
				usedInAir = false;
		}

		public override void Cast()
		{
			if (Time.time < readyTime)
				return;

			var grounded = movement.IsGrounded;

			if (!grounded && usedInAir && !broom.IsMounted)
				return;

			readyTime = Time.time + cooldown;

			var forward = CameraController != null ? CameraController.Forward : transform.forward;

			if (broom.IsMounted)
			{
				body.linearVelocity = CameraController.LookDirection * dashSpeed * 1.8f;
			}
			else
			{
				movement.Face(forward);
				movement.Launch(forward * dashSpeed + Vector3.up * (grounded ? groundLift : airLift), false);
				usedInAir |= !grounded;
			}

			Push(forward);

			FxUtility.Burst(transform.position + Vector3.up * 0.5f, SpellColour, 30, 6f, 0.2f);
		}

		private void Push(Vector3 forward)
		{
			foreach (var collider in Physics.OverlapSphere(transform.position + forward * pushRadius * 0.6f, pushRadius))
			{
				var other = collider.attachedRigidbody;

				if (other == null || other == body || other.isKinematic)
					continue;

				other.AddForce((forward + Vector3.up * 0.4f) * pushForce, ForceMode.VelocityChange);
			}
		}
	}
}
