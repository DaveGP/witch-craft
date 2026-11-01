using System;


namespace Data.Enums
{
	[Flags]
	public enum CharacterAction
	{
		None = 0,

		Interact = 1 << 0,

		Jump = 1 << 1,

		JumpHeld = 1 << 2,

		Sprint = 1 << 3,

		Descend = 1 << 4,

		Cast = 1 << 5,

		AltCast = 1 << 6,

		ToggleBroom = 1 << 7,

		Spell1 = 1 << 8,

		Spell2 = 1 << 9,

		Spell3 = 1 << 10,

		Spell4 = 1 << 11
	}
}
