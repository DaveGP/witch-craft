using Data.ScriptableObjects;

namespace Spells.Interfaces
{
	public interface ISpellCasterState
	{
		int ActiveSlot { get; }

		SpellDefinition ActiveSpell { get; }

		/// <summary>
		/// One line describing what the active spell will do, for the HUD.
		/// </summary>
		string Status { get; }

		bool IsAiming { get; }
	}
}
