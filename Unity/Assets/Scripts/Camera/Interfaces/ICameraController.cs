using UnityEngine;

namespace Camera.Interfaces
{
	public interface ICameraController
	{
		/// <summary>
		/// Camera facing flattened onto the ground plane, for camera relative movement.
		/// </summary>
		Vector3 Forward { get; }

		Vector3 Right { get; }

		/// <summary>
		/// Full camera facing including pitch, for flight.
		/// </summary>
		Vector3 LookDirection { get; }

		/// <summary>
		/// Ray through the centre of the screen, for aiming spells.
		/// </summary>
		Ray AimRay { get; }
	}
}
