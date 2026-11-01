using DependencyInjection;
using Events.Interfaces;
using Interaction.Interfaces;
using UnityEngine;


namespace Home
{
	// Every bookshelf in the home doubles as the witch's library of found storybooks.
	public class Bookcase : MonoBehaviour, IInteractable
	{
		public string Prompt => "Browse your storybooks";

		public bool CanInteract => true;

		public Vector3 Position => transform.position;

		public void Interact(GameObject interactor)
		{
			ServiceLocator.Get<IEventBusSender>()?.OpenBookcase();
		}
	}
}
