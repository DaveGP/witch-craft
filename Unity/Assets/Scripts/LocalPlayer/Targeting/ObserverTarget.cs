using Camera;
using Camera.Interfaces;
using Character.Interfaces;
using Character.Movement.Interfaces;
using Spells.Interfaces;
using UnityEngine;


namespace LocalPlayer.Targeting
{
	public class ObserverTarget : MonoBehaviour, IObserverTarget
	{
		[SerializeField]
		private CharacterCameraTarget cameraTarget;

		[SerializeField]
		private Transform broomSeat;

		public ICameraTarget Camera => cameraTarget;

		public Transform Transform => transform;

		public ICharacterMovementState Movement { get; private set; }

		public IBroomState Broom { get; private set; }

		public Transform BroomSeat => broomSeat;

		public ISpellCasterState Spells { get; private set; }

		private void Awake()
		{
			Movement = GetComponent<Character.PlayerCharacter>();
			Broom = GetComponent<IBroomState>();
			Spells = GetComponent<ISpellCasterState>();

			if (Movement == null)
				Debug.LogError($"{nameof(Movement)} is null");

			if (Broom == null)
				Debug.LogError($"{nameof(Broom)} is null");

			if (Spells == null)
				Debug.LogError($"{nameof(Spells)} is null");
		}
	}
}
