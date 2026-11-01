using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Controls;
using Data.Enums;
using Data.Input;


namespace Character.Input
{
	using Interfaces;


	public class UnityInputReader : IInputReader, IControlScheme
	{
		// Right stick turn rate, in the same units as mouse movement per second
		private const float StickLookSpeed = 900f;

		public bool UsingGamepad { get; private set; }

		public event Action Changed;

		private readonly PlayerControls playerControls;

		// Presses are recorded the moment the Input System sees them, rather than polled with
		// WasPressedThisFrame, so a press handled between frames is never lost
		private CharacterAction pressedActions;

		private MenuAction pressedMenuActions;

		public UnityInputReader(PlayerControls playerControls)
		{
			this.playerControls = playerControls;

			var player = playerControls.Player;

			OnPress(player.Interact, CharacterAction.Interact);
			OnPress(player.Jump, CharacterAction.Jump, MenuAction.Pump);
			OnPress(player.Cast, CharacterAction.Cast);
			OnPress(player.AltCast, CharacterAction.AltCast);
			OnPress(player.Broom, CharacterAction.ToggleBroom);
			OnPress(player.Spell1, CharacterAction.Spell1);
			OnPress(player.Spell2, CharacterAction.Spell2);
			OnPress(player.Spell3, CharacterAction.Spell3);
			OnPress(player.Spell4, CharacterAction.Spell4);
			OnPress(player.Undo, CharacterAction.Undo);
			OnPress(player.Pick, CharacterAction.Pick);
			OnPress(player.Satchel, CharacterAction.None, MenuAction.Satchel);
			OnPress(player.Spellbook, CharacterAction.None, MenuAction.Spellbook);
			OnPress(player.Cancel, CharacterAction.None, MenuAction.Cancel);

			player.Enable();
		}

		private void OnPress(InputAction action, CharacterAction characterAction, MenuAction menuAction = MenuAction.None)
		{
			action.started += context =>
			{
				pressedActions |= characterAction;
				pressedMenuActions |= menuAction;

				SetUsingGamepad(context.control.device is Gamepad);
			};
		}

		public InputData GetInputData()
		{
			UpdateDevice();

			return new InputData(
				Movement,
				Look,
				Rotate,
				Scroll,
				GetActions(),
				GetMenuActions());
		}

		// Sticks and the mouse don't press buttons, so watch them for which device is in use
		private void UpdateDevice()
		{
			var gamepad = Gamepad.current;

			if (gamepad != null && (gamepad.leftStick.ReadValue().sqrMagnitude > 0.1f || gamepad.rightStick.ReadValue().sqrMagnitude > 0.1f))
				SetUsingGamepad(true);
			else if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 4f)
				SetUsingGamepad(false);
			else if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
				SetUsingGamepad(false);
		}

		private void SetUsingGamepad(bool usingGamepad)
		{
			if (UsingGamepad == usingGamepad)
				return;

			UsingGamepad = usingGamepad;

			Changed?.Invoke();
		}

		private Vector2 Movement => Vector2.ClampMagnitude(playerControls.Player.Move.ReadValue<Vector2>(), 1f);

		private Vector2 Look => playerControls.Player.Look.ReadValue<Vector2>()
		                        + playerControls.Player.LookStick.ReadValue<Vector2>() * (StickLookSpeed * Time.unscaledDeltaTime);

		private float Rotate => playerControls.Player.Rotate.ReadValue<float>();

		private float Scroll => playerControls.Player.Scroll.ReadValue<float>();

		private CharacterAction GetActions()
		{
			var player = playerControls.Player;

			var actions = pressedActions;

			pressedActions = CharacterAction.None;

			if (player.Jump.IsPressed())
				actions |= CharacterAction.JumpHeld;

			if (player.Sprint.IsPressed())
				actions |= CharacterAction.Sprint;

			if (player.Descend.IsPressed())
				actions |= CharacterAction.Descend;

			return actions;
		}

		private MenuAction GetMenuActions()
		{
			var actions = pressedMenuActions;

			pressedMenuActions = MenuAction.None;

			return actions;
		}
	}
}
