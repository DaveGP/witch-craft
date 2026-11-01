using UnityEngine;
using Controls;
using Data.Enums;
using Data.Input;


namespace Character.Input
{
	using Interfaces;


	public class UnityInputReader : IInputReader
	{
		private readonly PlayerControls playerControls;

		public UnityInputReader(PlayerControls playerControls)
		{
			this.playerControls = playerControls;

			playerControls.Player.Enable();
		}

		public InputData GetInputData()
			=> new InputData(
				Movement,
				Look,
				Rotate,
				Scroll,
				GetActions(),
				GetMenuActions());

		private Vector2 Movement => Vector2.ClampMagnitude(playerControls.Player.Move.ReadValue<Vector2>(), 1f);

		private Vector2 Look => playerControls.Player.Look.ReadValue<Vector2>();

		private float Rotate => playerControls.Player.Rotate.ReadValue<float>();

		private float Scroll => playerControls.Player.Scroll.ReadValue<float>();

		private CharacterAction GetActions()
		{
			var player = playerControls.Player;

			var actions = CharacterAction.None;

			if (player.Interact.WasPressedThisFrame())
				actions |= CharacterAction.Interact;

			if (player.Jump.WasPressedThisFrame())
				actions |= CharacterAction.Jump;

			if (player.Jump.IsPressed())
				actions |= CharacterAction.JumpHeld;

			if (player.Sprint.IsPressed())
				actions |= CharacterAction.Sprint;

			if (player.Descend.IsPressed())
				actions |= CharacterAction.Descend;

			if (player.Cast.WasPressedThisFrame())
				actions |= CharacterAction.Cast;

			if (player.AltCast.WasPressedThisFrame())
				actions |= CharacterAction.AltCast;

			if (player.Broom.WasPressedThisFrame())
				actions |= CharacterAction.ToggleBroom;

			if (player.Spell1.WasPressedThisFrame())
				actions |= CharacterAction.Spell1;

			if (player.Spell2.WasPressedThisFrame())
				actions |= CharacterAction.Spell2;

			if (player.Spell3.WasPressedThisFrame())
				actions |= CharacterAction.Spell3;

			if (player.Spell4.WasPressedThisFrame())
				actions |= CharacterAction.Spell4;

			return actions;
		}

		private MenuAction GetMenuActions()
		{
			var player = playerControls.Player;

			var actions = MenuAction.None;

			if (player.Satchel.WasPressedThisFrame())
				actions |= MenuAction.Satchel;

			if (player.Spellbook.WasPressedThisFrame())
				actions |= MenuAction.Spellbook;

			if (player.Cancel.WasPressedThisFrame())
				actions |= MenuAction.Cancel;

			if (player.Jump.WasPressedThisFrame())
				actions |= MenuAction.Pump;

			return actions;
		}
	}
}
