using UnityEngine;

namespace World
{
	// Kinematic platform that ping-pongs between its start and an offset. Characters standing on it
	// read Velocity to be carried along.
	[RequireComponent(typeof(Rigidbody))]
	public class MovingPlatform : MonoBehaviour
	{
		public Vector3 Velocity { get; private set; }

		[SerializeField] private Vector3 offset = new(0, 0, 6);

		[SerializeField] private float period = 6f;

		[SerializeField] private float phase;

		private Rigidbody body;

		private Vector3 start;

		private void Awake()
		{
			body = GetComponent<Rigidbody>();
			body.isKinematic = true;
			body.interpolation = RigidbodyInterpolation.Interpolate;

			start = transform.position;
		}

		private void FixedUpdate()
		{
			var t = (Mathf.Sin((Time.time / period + phase) * Mathf.PI * 2f) + 1f) * 0.5f;

			var target = start + offset * t;

			Velocity = (target - body.position) / Time.fixedDeltaTime;

			body.MovePosition(target);
		}
	}
}
