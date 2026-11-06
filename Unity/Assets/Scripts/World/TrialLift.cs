using Interaction.Interfaces;
using UnityEngine;

namespace World
{
	// A lantern at the foot of a climb. Once she's reached a checkpoint it will carry her back up to it, if she wants.
	public class TrialLift : MonoBehaviour, IInteractable
	{
		[SerializeField] private TrialCourse course;

		public string Prompt => "Ride the wind up to the last lantern";

		public bool CanInteract => course.Reached >= 0;

		public Vector3 Position => transform.position;

		public void Interact(GameObject interactor) => course.Lift();
	}
}
