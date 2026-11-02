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

		/// <summary>
		/// Only offered when nothing else is in reach: Sable at the witch's heel shouldn't steal E from a herb.
		/// </summary>
		bool InBackground => false;

		Vector3 Position { get; }

		void Interact(GameObject interactor);
	}
}
