using Character.Movement.Interfaces;
using UnityEngine;
using Utilities;

namespace World
{
	// A column of warm rising air. Lifts the witch on foot and refills her broom while she rides it.
	public class Updraft : MonoBehaviour
	{
		[SerializeField] private float lift = 45f;

		[SerializeField] private float maxRiseSpeed = 9f;

		[SerializeField] private float rechargePerSecond = 4f;

		[SerializeField] private float radius = 2f;

		private void Start()
		{
			FxUtility.Loop(transform, Vector3.zero, new Color(0.8f, 0.95f, 1f, 0.6f), 30f, 6f, radius);
		}

		private void OnTriggerStay(Collider other)
		{
			var body = other.attachedRigidbody;

			if (body == null || body.isKinematic)
				return;

			var broom = other.GetComponentInParent<IBroomController>();

			if (broom != null && broom.IsMounted)
			{
				broom.Recharge(rechargePerSecond * Time.fixedDeltaTime);
				broom.Uplift(0.4f);
				return;
			}

			if (body.linearVelocity.y < maxRiseSpeed)
				body.AddForce(Vector3.up * lift, ForceMode.Acceleration);
		}
	}
}
