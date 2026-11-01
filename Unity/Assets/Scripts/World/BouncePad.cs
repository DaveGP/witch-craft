using Character.Movement.Interfaces;
using UnityEngine;

namespace World
{
	// Springy mushroom cap. Landing on it launches the witch skyward and refreshes her air jumps.
	public class BouncePad : MonoBehaviour
	{
		[SerializeField] private float bounceSpeed = 16f;

		[SerializeField] private Transform cap;

		private Vector3 capScale;

		private float squash;

		private void Awake()
		{
			if (cap != null)
				capScale = cap.localScale;
		}

		private void OnTriggerEnter(Collider other)
		{
			var movement = other.GetComponentInParent<IMovementController>();

			var body = other.attachedRigidbody;

			if (movement == null || body == null)
				return;

			var velocity = body.linearVelocity;
			velocity.y = bounceSpeed;

			movement.Launch(velocity, true);

			squash = 1f;
		}

		private void Update()
		{
			if (cap == null)
				return;

			squash = Mathf.MoveTowards(squash, 0f, Time.deltaTime * 4f);

			var wobble = Mathf.Sin(squash * Mathf.PI * 3f) * squash * 0.25f;

			cap.localScale = new Vector3(capScale.x * (1f + wobble), capScale.y * (1f - wobble), capScale.z * (1f + wobble));
		}
	}
}
