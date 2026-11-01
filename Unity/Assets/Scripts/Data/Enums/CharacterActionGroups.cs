namespace Data.Enums
{
	public static class CharacterActionGroups
	{
		public const CharacterAction Buffered =
			CharacterAction.Interact |
			CharacterAction.Jump |
			CharacterAction.Cast |
			CharacterAction.AltCast |
			CharacterAction.ToggleBroom |
			SpellSlots;

		public const CharacterAction Continuous =
			CharacterAction.JumpHeld |
			CharacterAction.Sprint |
			CharacterAction.Descend;

		public const CharacterAction SpellSlots =
			CharacterAction.Spell1 |
			CharacterAction.Spell2 |
			CharacterAction.Spell3 |
			CharacterAction.Spell4;
	}
}
