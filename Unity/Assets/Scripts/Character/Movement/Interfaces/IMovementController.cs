using Data.Enums;
using UnityEngine;


namespace Character.Movement.Interfaces
{
	public interface IMovementController
	{
		public bool IsGrounded { get; }

		/// <summary>
		/// While suspended, another controller (the broom) owns the rigidbody.
		/// </summary>
		public bool Suspended { get; set; }

		/// <summary>
		/// Applies world-space movement to the character. Magnitude 0-1.
		/// </summary>
		public void Move(Vector3 direction);

		/// <summary>
		/// Requests a jump. Buffered briefly so early presses still count.
		/// </summary>
		public void Jump();

		/// <summary>
		/// Releasing jump early cuts the jump short.
		/// </summary>
		public void SetJumpHeld(bool held);

		/// <summary>
		/// Sets the Character's Gait.
		/// </summary>
		public void SetGait(CharacterGait gait);

		/// <summary>
		/// Turns the character to face a world direction.
		/// </summary>
		public void Face(Vector3 direction);

		/// <summary>
		/// Replaces the character's velocity, for bounce pads and gusts.
		/// </summary>
		public void Launch(Vector3 velocity, bool restoreAirJumps);

		public void Teleport(Vector3 position);
	}
}
