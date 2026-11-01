using System;

namespace Character.Input.Interfaces
{
	public interface IControlScheme
	{
		/// <summary>
		/// True when the last input came from a gamepad, for button prompts and menu focus.
		/// </summary>
		bool UsingGamepad { get; }

		event Action Changed;
	}
}
