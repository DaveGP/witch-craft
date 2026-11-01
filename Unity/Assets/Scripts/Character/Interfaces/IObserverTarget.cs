using Camera.Interfaces;
using Character.Movement.Interfaces;
using Spells.Interfaces;
using UnityEngine;

namespace Character.Interfaces
{
	/// <summary>
	/// Everything the camera, UI and companions need to follow the active character.
	/// </summary>
	public interface IObserverTarget
	{
		ICameraTarget Camera { get; }

		Transform Transform { get; }

		ICharacterMovementState Movement { get; }

		IBroomState Broom { get; }

		/// <summary>
		/// Where a passenger (the cat) sits while flying.
		/// </summary>
		Transform BroomSeat { get; }

		ISpellCasterState Spells { get; }
	}
}
