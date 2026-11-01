using UnityEngine;

namespace Data.ScriptableObjects
{
	[CreateAssetMenu(fileName = "BroomStats", menuName = "Witch Craft/Broom Stats", order = 2)]
	public class BroomStats : ScriptableObject
	{
		public float Speed = 11f;

		public float BoostSpeed = 18f;

		public float VerticalSpeed = 6f;

		public float Acceleration = 14f;

		public float TurnSpeed = 240f;

		public float BankAngle = 25f;

		[Tooltip("Seconds of flight a full charge gives")]
		public float MaxCharge = 10f;

		[Tooltip("Charge regained per second while standing on the ground")]
		public float RechargeRate = 2.5f;

		public float BoostDrainMultiplier = 2f;
	}
}
