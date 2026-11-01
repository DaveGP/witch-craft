using UnityEngine;

namespace Interaction.Interfaces
{
	public interface IInteractable
	{
		/// <summary>
		/// Shown on the HUD while this is the interaction focus, e.g. "Gather Lavender".
		/// </summary>
		string Prompt { get; }

		bool CanInteract { get; }

		Vector3 Position { get; }

		void Interact(GameObject interactor);
	}
}
