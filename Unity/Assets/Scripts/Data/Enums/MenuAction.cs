using System;


namespace Data.Enums
{
	[Flags]
	public enum MenuAction
	{
		None = 0,

		Satchel = 1 << 0,

		Spellbook = 1 << 1,

		Cancel = 1 << 2,

		Pump = 1 << 3,

		Journal = 1 << 4
	}
}
