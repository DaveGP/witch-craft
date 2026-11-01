namespace Spells.Interfaces
{
	public interface ISpellCaster : ISpellCasterState
	{
		public void SelectSlot(int slot);

		public void Cast();

		public void AltCast();

		public void Undo();

		public void Pick();

		/// <summary>
		/// Continuous rotate axis and buffered scroll for spells that position things.
		/// </summary>
		public void Adjust(float rotate, float scroll);
	}
}
