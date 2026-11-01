using System;
using System.Collections.Generic;
using Data.Enums;
using UnityEngine;


namespace GameState
{
	using Interfaces;

	public class InputModeService : IInputModeService
	{
		public InputMode Mode => modes.Count > 0 ? modes.Peek() : InputMode.Gameplay;

		public event Action<InputMode> ModeChanged;

		private readonly Stack<InputMode> modes = new();

		public void Push(InputMode mode)
		{
			modes.Push(mode);

			Apply();
		}

		public void Pop()
		{
			if (modes.Count > 0)
				modes.Pop();

			Apply();
		}

		public void Reset()
		{
			modes.Clear();

			Apply();
		}

		private void Apply()
		{
			var gameplay = Mode == InputMode.Gameplay;

#if UNITY_WEBGL && !UNITY_EDITOR
			// Browsers only grant pointer lock inside a click, so the page locks on the next click instead
			Utilities.WebPointerLock.SetWanted(gameplay);
#else
			Cursor.lockState = gameplay ? CursorLockMode.Locked : CursorLockMode.None;
#endif
			Cursor.visible = !gameplay;

			ModeChanged?.Invoke(Mode);
		}
	}
}
