using Data.Enums;
using Data.ScriptableObjects;

namespace Spells.Interfaces
{
	public interface ISpellEffect
	{
		SpellEffectType Type { get; }

		string Status { get; }

		bool IsAiming { get; }

		/// <summary>
		/// True while the spell wants the scroll wheel (cycling pieces, pushing things further away).
		/// </summary>
		bool CapturesScroll { get; }

		/// <summary>
		/// The spell has been readied.
		/// </summary>
		void Begin(SpellDefinition spell);

		/// <summary>
		/// The spell has been put away or swapped out.
		/// </summary>
		void End();

		void Cast();

		void AltCast();

		void Undo();

		void Pick();

		void Adjust(float rotate, float scroll);
	}
}
