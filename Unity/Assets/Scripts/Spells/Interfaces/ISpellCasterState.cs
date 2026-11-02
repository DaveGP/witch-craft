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

		/// <summary>
		/// The witch cast the readied spell (whether or not it found a target), for the casting animation.
		/// </summary>
		event System.Action<SpellDefinition> SpellCast;
	}
}
