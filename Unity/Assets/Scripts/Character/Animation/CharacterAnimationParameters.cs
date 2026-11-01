using UnityEngine;


namespace Character.Animation
{
	// Crewfall's locomotion parameter names, so its Synty Animator Controller can drive the witch,
	// plus a few for jumping and the broom.
	public static class CharacterAnimationParameters
	{
		public static readonly int ForwardVelocity =
			Animator.StringToHash("ForwardVelocity");

		public static readonly int StrafeVelocity =
			Animator.StringToHash("StrafeVelocity");

		public static readonly int Velocity =
			Animator.StringToHash("Velocity");

		public static readonly int Grounded =
			Animator.StringToHash("IsGrounded");

		public static readonly int Gait =
			Animator.StringToHash("Gait");

		public static readonly int MovementDirection =
			Animator.StringToHash("Direction");

		public static readonly int Flying =
			Animator.StringToHash("IsFlying");

		public static readonly int VerticalVelocity =
			Animator.StringToHash("VerticalVelocity");

		public static readonly int Jump =
			Animator.StringToHash("Jump");
	}
}
