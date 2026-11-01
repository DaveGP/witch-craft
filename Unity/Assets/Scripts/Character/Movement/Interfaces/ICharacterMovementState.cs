using System;
using Data.Enums;
using UnityEngine;

namespace Character.Movement.Interfaces
{
	public interface ICharacterMovementState
	{
		Vector3 Velocity { get; }

		Vector3 LocalVelocity { get; }

		bool IsGrounded { get; }

		CharacterGait Gait { get; }

		MovementMode Mode { get; }

		event Action Jumped;

		event Action Landed;
	}
}
