using UnityEngine;

namespace Data.ScriptableObjects
{
	[CreateAssetMenu(fileName = "CharacterStats", menuName = "Witch Craft/Character Stats", order = 1)]
	public class CharacterStats : ScriptableObject
	{
		[Header("Ground")]
		public float WalkSpeed = 2.5f;

		public float RunSpeed = 5.5f;

		public float SprintSpeed = 8.5f;

		public float Acceleration = 60f;

		public float TurnSpeed = 900f;

		[Header("Air")]
		public float AirAcceleration = 30f;

		public float JumpHeight = 1.8f;

		public float AirJumpHeight = 1.3f;

		public int AirJumps = 1;

		public float CoyoteTime = 0.12f;

		public float JumpBufferTime = 0.15f;

		[Header("Gravity")]
		public float Gravity = 28f;

		public float FallGravityMultiplier = 1.6f;

		public float LowJumpGravityMultiplier = 2.2f;

		public float MaxFallSpeed = 30f;
	}
}
