using Data.Enums;
using UnityEngine;
using Utilities;
using World;

namespace Spells.Effects
{
	// Lux + Vita. Calls a little light that drifts at the witch's shoulder, lighting dark places and
	// making hidden paths solid while it lasts.
	public class WispSpell : SpellEffect
	{
		public override SpellEffectType Type => SpellEffectType.Wisp;

		public override bool IsAiming => false;

		public override string Status => wisp != null
			? $"Wisp: {Mathf.CeilToInt(remaining)}s of light left · click to dismiss"
			: "Wisp: click to call a guiding light";

		[SerializeField] private float duration = 90f;

		[SerializeField] private Vector3 shoulderOffset = new(0.8f, 2.1f, -0.3f);

		private Transform wisp;

		private float remaining;

		private Vector3 velocity;

		public override void Cast()
		{
			if (wisp != null)
			{
				Dismiss();
				return;
			}

			Summon();
		}

		private void Summon()
		{
			var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			orb.name = "Wisp";
			Destroy(orb.GetComponent<Collider>());

			orb.transform.position = transform.TransformPoint(shoulderOffset);
			orb.transform.localScale = Vector3.one * 0.3f;

			var renderer = orb.GetComponent<Renderer>();
			renderer.sharedMaterial = Database.GlowMaterial;
			RendererTint.Apply(orb, SpellColour, 3f);

			var light = orb.AddComponent<Light>();
			light.type = LightType.Point;
			light.color = SpellColour;
			light.range = 12f;
			light.intensity = 3f;

			FxUtility.Loop(orb.transform, Vector3.zero, SpellColour, 12f, 0.3f, 0.1f, 0.08f);

			wisp = orb.transform;
			remaining = duration;

			HiddenPath.AddRevealer(wisp);

			FxUtility.Burst(wisp.position, SpellColour, 30, 3f);
		}

		private void Dismiss()
		{
			if (wisp == null)
				return;

			FxUtility.Burst(wisp.position, SpellColour, 20, 2f);

			HiddenPath.RemoveRevealer(wisp);
			Destroy(wisp.gameObject);
			wisp = null;
		}

		private void LateUpdate()
		{
			if (wisp == null)
				return;

			remaining -= Time.deltaTime;

			if (remaining <= 0f)
			{
				EventBus.Notify("Your wisp flickers out.");
				Dismiss();
				return;
			}

			var target = transform.TransformPoint(shoulderOffset) + new Vector3(Mathf.Sin(Time.time * 1.3f), Mathf.Sin(Time.time * 2.1f) * 0.5f, Mathf.Cos(Time.time * 1.7f)) * 0.3f;

			wisp.position = Vector3.SmoothDamp(wisp.position, target, ref velocity, 0.35f);
		}

		private void OnDestroy()
		{
			Dismiss();
		}
	}
}
