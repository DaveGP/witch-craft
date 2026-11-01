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

		/// <summary>
		/// Takes back the active spell's last action, such as the last piece conjured.
		/// </summary>
		public void OnUndo();

		/// <summary>
		/// Copies what the active spell is aimed at, such as a built piece's type and colour.
		/// </summary>
		public void OnPick();

		public void OnSelectSpell(int slot);

		public void OnSpellAdjust(float rotate, float scroll);
	}
}
