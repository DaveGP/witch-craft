namespace Character.Actions.Interfaces
{
	public interface IActionController
	{
		/// <summary>
		/// Interacts with whatever the character is focused on.
		/// </summary>
		public void OnInteract();

		/// <summary>
		/// Casts the active spell.
		/// </summary>
		public void OnCast();

		/// <summary>
		/// The active spell's secondary action, usually cancel or cycle.
		/// </summary>
		public void OnAltCast();

		public void OnSelectSpell(int slot);

		public void OnSpellAdjust(float rotate, float scroll);
	}
}
