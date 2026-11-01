using System;
using Character.Input.Interfaces;
using Character.Interfaces;
using Character.Movement;
using Character.Movement.Interfaces;
using Data.Enums;
using DependencyInjection;
using UnityEngine;


namespace Character
{
	// Single player counterpart of crewfall's CharacterNetworkController: pulls the buffered input each
	// physics step, hands it to the motor and exposes the combined movement state for animation and UI.
	public class PlayerCharacter : MonoBehaviour, ICharacterMovementState
	{
		private ICharacterInputSource inputSource;

		private IObserverTarget observerTarget;

		private ICharacterMotor characterMotor;

		private IActiveTargetController targetController;

		private ICharacterMovementState movementState;

		private IBroomState broomState;

		public Vector3 Velocity => movementState.Velocity;

		public Vector3 LocalVelocity => movementState.LocalVelocity;

		public bool IsGrounded => movementState.IsGrounded;

		public CharacterGait Gait => movementState.Gait;

		public MovementMode Mode => broomState.IsMounted ? MovementMode.Broom : MovementMode.Grounded;

		public event Action Jumped
		{
			add => movementState.Jumped += value;
			remove => movementState.Jumped -= value;
		}

		public event Action Landed
		{
			add => movementState.Landed += value;
			remove => movementState.Landed -= value;
		}

		private void Awake()
		{
			inputSource = ServiceLocator.Get<ICharacterInputSource>();

			targetController = ServiceLocator.Get<IActiveTargetController>();

			observerTarget = GetComponent<IObserverTarget>();

			characterMotor = GetComponent<ICharacterMotor>();

			movementState = GetComponent<MovementController>();

			broomState = GetComponent<IBroomState>();

			if (inputSource == null)
				Debug.LogError($"{nameof(inputSource)} is null");

			if (targetController == null)
				Debug.LogError($"{nameof(targetController)} is null");

			if (observerTarget == null)
				Debug.LogError($"{nameof(observerTarget)} is null");

			if (characterMotor == null)
				Debug.LogError($"{nameof(characterMotor)} is null");

			if (movementState == null)
				Debug.LogError($"{nameof(movementState)} is null");

			if (broomState == null)
				Debug.LogError($"{nameof(broomState)} is null");
		}

		private void Start()
		{
			targetController.SetActiveTarget(observerTarget);
		}

		private void FixedUpdate()
		{
			var characterInput = inputSource.ConsumeCharacterInput();

			characterMotor.ProcessInput(characterInput);
		}
	}
}
