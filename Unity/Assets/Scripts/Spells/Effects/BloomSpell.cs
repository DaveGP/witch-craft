using Data.Enums;
using UnityEngine;
using World;

namespace Spells.Effects
{
	// Vita + Terra. Coaxes enchanted soil into sudden growth: vine platforms out in the wilds,
	// herbs in the home garden.
	public class BloomSpell : SpellEffect
	{
		public override SpellEffectType Type => SpellEffectType.Bloom;

		public override string Status => "Bloom: aim at glowing soil and click to make it grow";

		[SerializeField] private float searchRadius = 1.8f;

		public override void Cast()
		{
			if (!Aim(out var hit))
				return;

			var spot = hit.collider.GetComponentInParent<BloomSpot>() ?? Nearest(hit.point);

			if (spot == null)
			{
				EventBus.Notify("Nothing stirs. Bloom needs enchanted soil, look for glowing patches.");
				return;
			}

			if (!spot.Bloom())
				EventBus.Notify("That has already bloomed.");
		}

		private BloomSpot Nearest(Vector3 point)
		{
			foreach (var collider in Physics.OverlapSphere(point, searchRadius, ~0, QueryTriggerInteraction.Collide))
			{
				var spot = collider.GetComponentInParent<BloomSpot>();

				if (spot != null && !spot.IsBloomed)
					return spot;
			}

			return null;
		}
	}
}
