using Data.Enums;
using UnityEngine;


namespace Data.Input
{
	public readonly struct InputData
	{
		public readonly Vector2 Move;

		public readonly Vector2 Look;

		public readonly float Rotate;

		public readonly float Scroll;

		public readonly CharacterAction Action;

		public readonly MenuAction MenuAction;

		public InputData(
			Vector2 move,
			Vector2 look,
			float rotate,
			float scroll,
			CharacterAction action,
			MenuAction menuAction)
		{
			Move = move;
			Look = look;
			Rotate = rotate;
			Scroll = scroll;
			Action = action;
			MenuAction = menuAction;
		}
	}
}
