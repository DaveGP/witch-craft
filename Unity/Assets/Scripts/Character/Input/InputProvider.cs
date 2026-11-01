using UnityEngine;
using Character.Input.Interfaces;
using Data.Enums;
using Data.Input;
using GameState.Interfaces;
using SystemManagement.Interfaces;


namespace Character.Input
{
	/// <summary>
	/// Buffers raw input read during Update and exposes it for
	/// consumption during FixedUpdate. Character and camera input is
	/// suppressed while a menu or station has focus.
	/// </summary>
	public class InputProvider : IUpdateSystem, ICharacterInputSource, ICameraInputSource, IMenuInputSource
	{
		private readonly IInputReader inputReader;

		private readonly IInputModeService inputModeService;

		private Vector2 movement;

		private Vector2 looking;

		private float rotate;

		private float scroll;

		private float zoom;

		private CharacterAction actions;

		private MenuAction menuActions;


		public InputProvider(IInputReader inputReader, IInputModeService inputModeService)
		{
			this.inputReader = inputReader;
			this.inputModeService = inputModeService;

			if (inputReader == null)
				Debug.LogError($"{nameof(inputReader)} is null!");

			if (inputModeService == null)
				Debug.LogError($"{nameof(inputModeService)} is null!");
		}

		public CharacterInput ConsumeCharacterInput()
		{
			var characterInput = new CharacterInput(
				movement,
				rotate,
				scroll,
				actions);

			scroll = 0f;

			ClearBufferedActions();

			return characterInput;
		}

		public CameraInput GetCameraInput()
			=> new(looking, zoom);

		public MenuAction ConsumeMenuInput()
		{
			var consumed = menuActions;

			menuActions = MenuAction.None;

			return consumed;
		}

		public void RunUpdate()
		{
			ReadInput();
		}

		private void ReadInput()
		{
			var inputData = inputReader.GetInputData();

			menuActions |= inputData.MenuAction;

			if (inputModeService.Mode != InputMode.Gameplay)
			{
				movement = Vector2.zero;
				looking = Vector2.zero;
				rotate = 0f;
				zoom = 0f;
				actions = CharacterAction.None;
				return;
			}

			movement = inputData.Move;

			looking = inputData.Look;

			rotate = inputData.Rotate;

			scroll += inputData.Scroll;

			zoom = inputData.Scroll;

			ClearContinuousActions();

			BufferActions(inputData.Action);
		}

		private void BufferActions(CharacterAction action)
		{
			actions |= action;
		}

		private void ClearContinuousActions()
		{
			actions &= ~CharacterActionGroups.Continuous;
		}

		private void ClearBufferedActions()
		{
			actions &= ~CharacterActionGroups.Buffered;
		}
	}
}
