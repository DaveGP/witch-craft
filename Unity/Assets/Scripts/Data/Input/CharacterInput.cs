using UnityEngine;
using Data.Enums;


namespace Data.Input
{
	public readonly struct CharacterInput
	{
		public readonly Vector2 Move;

		public readonly float Rotate;

		public readonly float Scroll;

		public readonly CharacterAction Action;

		public CharacterInput(
			Vector2 move,
			float rotate,
			float scroll,
			CharacterAction action)
		{
			Move = move;
			Rotate = rotate;
			Scroll = scroll;
			Action = action;
		}
	}
}
