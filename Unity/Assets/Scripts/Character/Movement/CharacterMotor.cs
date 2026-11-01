using Camera.Interfaces;
using Character.Actions.Interfaces;
using Data.Enums;
using Data.Input;
using DependencyInjection;
using UnityEngine;


namespace Character.Movement
{
	using Interfaces;


	public class CharacterMotor : MonoBehaviour, ICharacterMotor
	{
		private IMovementController movementController;

		private IBroomController broomController;

		private IActionController actionController;

		private ICameraController cameraController;

		private void Awake()
		{
			movementController = GetComponent<IMovementController>();

			broomController = GetComponent<IBroomController>();

			actionController = GetComponent<IActionController>();

			if (movementController == null)
				Debug.LogError($"{nameof(movementController)} is null");

			if (broomController == null)
				Debug.LogError($"{nameof(broomController)} is null");

			if (actionController == null)
				Debug.LogError($"{nameof(actionController)} is null");
		}

		private void Start()
		{
			// The camera registers itself in Awake, so it is only guaranteed to exist from Start
			cameraController = ServiceLocator.Get<ICameraController>();

			if (cameraController == null)
				Debug.LogError($"{nameof(cameraController)} is null");
		}

		public void ProcessInput(CharacterInput characterInput)
		{
			HandleBroomToggle(characterInput.Action.HasFlag(CharacterAction.ToggleBroom));

			if (broomController.IsMounted)
				Fly(characterInput);
			else
				Move(characterInput);

			Actions(characterInput);
		}

		private void HandleBroomToggle(bool toggle)
		{
			if (toggle)
				broomController.Toggle();
		}

		private void Move(CharacterInput characterInput)
		{
			HandleJump(characterInput.Action);

			HandleGait(characterInput);

			HandleMovement(characterInput.Move);
		}

		private void HandleJump(CharacterAction action)
		{
			if (action.HasFlag(CharacterAction.Jump))
				movementController.Jump();

			movementController.SetJumpHeld(action.HasFlag(CharacterAction.JumpHeld));
		}

		private void HandleGait(CharacterInput characterInput)
		{
			var gait = CalculateGait(characterInput);

			movementController.SetGait(gait);
		}

		private static CharacterGait CalculateGait(CharacterInput characterInput)
		{
			var input = characterInput.Move.magnitude;

			if (input < 0.05f)
				return CharacterGait.Idle;

			if (characterInput.Action.HasFlag(CharacterAction.Sprint))
				return CharacterGait.Sprint;

			return input < 0.6f ? CharacterGait.Walk : CharacterGait.Run;
		}

		private void HandleMovement(Vector2 move)
		{
			movementController.Move(CameraRelative(move, cameraController.Forward));
		}

		private void Fly(CharacterInput characterInput)
		{
			movementController.SetGait(CharacterGait.Fly);

			var vertical = 0f;

			if (characterInput.Action.HasFlag(CharacterAction.JumpHeld))
				vertical += 1f;

			if (characterInput.Action.HasFlag(CharacterAction.Descend))
				vertical -= 1f;

			// Steer with the full camera look so pitching the view climbs and dives
			var direction = CameraRelative(characterInput.Move, cameraController.LookDirection);

			broomController.Fly(direction, vertical, characterInput.Action.HasFlag(CharacterAction.Sprint));
		}

		private Vector3 CameraRelative(Vector2 move, Vector3 forward)
		{
			var direction = forward * move.y + cameraController.Right * move.x;

			return Vector3.ClampMagnitude(direction, 1f);
		}

		private void Actions(CharacterInput characterInput)
		{
			var action = characterInput.Action;

			if (action.HasFlag(CharacterAction.Interact))
				actionController.OnInteract();

			HandleSpellSlots(action);

			if (action.HasFlag(CharacterAction.Cast))
				actionController.OnCast();

			if (action.HasFlag(CharacterAction.AltCast))
				actionController.OnAltCast();

			if (action.HasFlag(CharacterAction.Undo))
				actionController.OnUndo();

			if (action.HasFlag(CharacterAction.Pick))
				actionController.OnPick();

			actionController.OnSpellAdjust(characterInput.Rotate, characterInput.Scroll);
		}

		private void HandleSpellSlots(CharacterAction action)
		{
			if (action.HasFlag(CharacterAction.Spell1))
				actionController.OnSelectSpell(0);

			if (action.HasFlag(CharacterAction.Spell2))
				actionController.OnSelectSpell(1);

			if (action.HasFlag(CharacterAction.Spell3))
				actionController.OnSelectSpell(2);

			if (action.HasFlag(CharacterAction.Spell4))
				actionController.OnSelectSpell(3);
		}
	}
}
