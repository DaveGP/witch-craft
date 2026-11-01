using System;
using Data.Enums;


namespace GameState.Interfaces
{
	public interface IInputModeService
	{
		InputMode Mode { get; }

		event Action<InputMode> ModeChanged;

		/// <summary>
		/// Gives focus to a menu or station until the matching Pop.
		/// </summary>
		void Push(InputMode mode);

		void Pop();

		/// <summary>
		/// Drops every pushed mode and returns to gameplay.
		/// </summary>
		void Reset();
	}
}
