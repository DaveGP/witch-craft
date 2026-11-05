using Data.Enums;
using DependencyInjection;
using Home;
using Home.Interfaces;
using UnityEngine;
using Utilities;

namespace Spells.Effects
{
	// Aer + Forma. Lifts furniture and crates on a ribbon of magic so the witch can rearrange her home:
	// click to lift, steer with the camera, Q/R to turn, scroll to push it away or pull it close, click to set down.
	public class LevitateSpell : SpellEffect
	{
		public override SpellEffectType Type => SpellEffectType.Levitate;

		public override bool CapturesScroll => held != null;

		public override string Status => held == null
			? "Levitate: aim at furniture, a crate or a loose boulder and click to lift it"
			: $"Levitating {held.name}: click to set down · Q/R turn · scroll push/pull · right-click to cancel";

		[SerializeField] private float minDistance = 2.5f;

		[SerializeField] private float maxDistance = 16f;

		[SerializeField] private float turnSpeed = 140f;

		private Movable held;

		private float distance;

		private float yaw;

		private Vector3 originalPosition;

		private Quaternion originalRotation;

		private Vector3 smoothVelocity;

		private Vector3 restPosition;

		public override void Cast()
		{
			if (held != null)
				SetDown();
			else
				Lift();
		}

		public override void AltCast()
		{
			if (held == null)
				return;

			held.transform.SetPositionAndRotation(originalPosition, originalRotation);

			Release();
		}

		public override void Adjust(float rotate, float scroll)
		{
			if (held == null)
				return;

			yaw += rotate * turnSpeed * Time.fixedDeltaTime;

			if (Mathf.Abs(scroll) > 0.01f)
				distance = Mathf.Clamp(distance + Mathf.Sign(scroll) * 1f, minDistance, maxDistance);
		}

		protected override void OnEnd()
		{
			if (held != null)
				SetDown();
		}

		private void Lift()
		{
			if (!Aim(out var hit))
				return;

			var movable = hit.collider.GetComponentInParent<Movable>();

			if (movable == null)
			{
				EventBus.Notify("That won't budge. Levitate works on furniture, crates and loose boulders.");
				return;
			}

			held = movable;

			originalPosition = held.transform.position;
			originalRotation = held.transform.rotation;

			yaw = held.transform.eulerAngles.y;

			distance = Mathf.Clamp(Vector3.Distance(CameraController.AimRay.origin, hit.point), minDistance + 3f, maxDistance);

			smoothVelocity = Vector3.zero;

			held.BeginLevitate(SpellColour);

			FxUtility.Burst(held.transform.position, SpellColour, 20, 2f);
		}

		private void LateUpdate()
		{
			if (held == null)
				return;

			var ray = CameraController.AimRay;

			var point = ray.GetPoint(distance);

			if (Physics.Raycast(ray, out var blocking, distance, LayerUtility.SurfaceMask, QueryTriggerInteraction.Ignore))
				point = blocking.point - ray.direction * 0.4f;

			restPosition = point;

			if (Physics.Raycast(point + Vector3.up * 0.5f, Vector3.down, out var ground, 40f, LayerUtility.SurfaceMask, QueryTriggerInteraction.Ignore))
				restPosition.y = ground.point.y + held.BottomOffset;

			// Float a little above where it would land so the witch can see the spot
			var hover = restPosition;
			hover.y = Mathf.Max(point.y, restPosition.y + 0.35f) + Mathf.Sin(Time.time * 3f) * 0.05f;

			held.transform.position = Vector3.SmoothDamp(held.transform.position, hover, ref smoothVelocity, 0.12f);
			held.transform.rotation = Quaternion.Slerp(held.transform.rotation, Quaternion.Euler(0f, yaw, 0f), 1f - Mathf.Exp(-12f * Time.deltaTime));
		}

		private void SetDown()
		{
			var home = ServiceLocator.Get<IHomeService>();

			var position = restPosition;

			if (home != null && home.Plot.Contains(position))
			{
				position.x = Mathf.Round(position.x / HomePlot.FreeSnap) * HomePlot.FreeSnap;
				position.z = Mathf.Round(position.z / HomePlot.FreeSnap) * HomePlot.FreeSnap;
			}

			var snappedYaw = Mathf.Round(yaw / 15f) * 15f;

			held.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, snappedYaw, 0f));

			FxUtility.Burst(position, SpellColour, 16, 1.5f);

			var placed = held.GetComponent<PlacedPiece>();

			Release();

			if (placed != null)
				home?.NotifyMoved(placed);
		}

		private void Release()
		{
			held.EndLevitate();
			held = null;
		}
	}
}
