using Audio;
using Audio.Interfaces;
using Character.Movement.Interfaces;
using Data.Enums;
using DependencyInjection;
using UnityEngine;
using Utilities;

namespace Spells.Effects
{
	// Aqua + Aer. Wraps the witch in a bubble that floats her steadily upwards for a few seconds, steerable as
	// she rises. Once per landing; jumping or casting again pops it.
	public class BubbleSpell : SpellEffect
	{
		public override SpellEffectType Type => SpellEffectType.Bubble;

		public override bool IsAiming => false;

		public override string Status => movement.IsFloating
			? "Bubble: floating! Steer as you rise · jump or click to pop"
			: usedSinceLanding ? "Bubble: land to blow another" : "Bubble: click to float upwards in a bubble";

		[SerializeField] private float duration = 2.6f;

		[SerializeField] private float riseSpeed = 4.2f;

		private IMovementController movement;

		private IBroomState broom;

		private GameObject bubble;

		private bool usedSinceLanding;

		private bool wasFloating;

		protected override void Awake()
		{
			base.Awake();

			movement = GetComponent<IMovementController>();
			broom = GetComponent<IBroomState>();
		}

		public override void Cast()
		{
			if (movement.IsFloating)
			{
				movement.StopFloating();
				return;
			}

			if (broom.IsMounted)
			{
				EventBus.Notify("You can't blow a bubble while riding the broom.");
				return;
			}

			if (usedSinceLanding)
				return;

			usedSinceLanding = true;

			movement.Float(duration, riseSpeed);

			ShowBubble();

			ServiceLocator.Get<IAudioService>()?.Play(Sound.Boing, 0.4f, 0.05f);
		}

		private void FixedUpdate()
		{
			if (movement.IsGrounded && !movement.IsFloating)
				usedSinceLanding = false;
		}

		private void LateUpdate()
		{
			var floating = movement.IsFloating;

			if (wasFloating && !floating)
				Pop();

			wasFloating = floating;

			if (bubble != null)
				bubble.transform.localScale = Vector3.one * (2.3f + Mathf.Sin(Time.time * 6f) * 0.05f);
		}

		private void ShowBubble()
		{
			if (bubble != null)
				return;

			bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			bubble.name = "Bubble";
			Destroy(bubble.GetComponent<Collider>());

			bubble.transform.SetParent(transform, false);
			bubble.transform.localPosition = Vector3.up * 0.85f;

			bubble.GetComponent<Renderer>().sharedMaterial = new Material(Database.GhostMaterial) { color = new Color(0.7f, 0.9f, 1f, 0.22f) };

			LayerUtility.SetLayerRecursively(bubble, LayerMask.NameToLayer("Ignore Raycast"));
		}

		private void Pop()
		{
			if (bubble == null)
				return;

			FxUtility.Burst(bubble.transform.position, new Color(0.75f, 0.92f, 1f), 30, 3f, 0.15f);

			ServiceLocator.Get<IAudioService>()?.Play(Sound.Click, 0.6f, 0.1f);

			Destroy(bubble);
			bubble = null;
		}

		protected override void OnEnd()
		{
			// Putting the spell away mid-float lets the witch drop rather than leaving her stuck rising
			movement.StopFloating();
		}
	}
}
