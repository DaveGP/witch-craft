namespace Identity.Interfaces
{
	public interface IWitchProfile
	{
		/// <summary>
		/// The witch's chosen name, or "Witch" until she has one.
		/// </summary>
		string Name { get; }

		bool HasName { get; }

		/// <summary>
		/// Names the witch now, in the loaded game.
		/// </summary>
		void Rename(string name);

		/// <summary>
		/// A name chosen on the title screen for a slot that is about to load; the slot takes it if it has none.
		/// </summary>
		void SetPendingName(string name);

		/// <summary>
		/// Fills {name} in dialogue and messages with the witch's name.
		/// </summary>
		string Personalise(string text);
	}
}
