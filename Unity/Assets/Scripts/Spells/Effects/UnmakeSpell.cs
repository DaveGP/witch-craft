using System.Linq;
using Data.Enums;
using DependencyInjection;
using Home;
using Home.Interfaces;
using UnityEngine;
using Utilities;

namespace Spells.Effects
{
	// Nihil + Forma. Unravels something the witch built and returns its materials to her satchel.
	public class UnmakeSpell : SpellEffect
	{
		public override SpellEffectType Type => SpellEffectType.Unmake;

		public override string Status
		{
			get
			{
				if (hovered == null)
					return "Unmake: aim at something you built";

				if (!hovered.Record.Starter)
					return $"Unmake {hovered.Definition.DisplayName}: click to unravel it (returns {hovered.Definition.CostSummary})";

				var refund = ServiceLocator.Get<IHomeService>()?.Refund(hovered);
				var returns = refund == null || refund.Count == 0 ? "nothing" : string.Join(", ", refund.Select(stack => stack.ToString()));

				// The starter hut was a gift: say why it returns less than its price
				return $"Unmake {hovered.Definition.DisplayName}: click to unravel it (returns {returns}; part of the hut you were given, so only half comes back)";
			}
		}

		private PlacedPiece hovered;

		protected override void OnEnd()
		{
			SetHovered(null);
		}

		public override void Cast()
		{
			if (hovered == null)
				return;

			var piece = hovered;

			SetHovered(null);

			ServiceLocator.Get<IHomeService>()?.Remove(piece, true);
		}

		private void LateUpdate()
		{
			if (!IsReadied)
				return;

			SetHovered(Aim(out var hit) ? hit.collider.GetComponentInParent<PlacedPiece>() : null);
		}

		private void SetHovered(PlacedPiece piece)
		{
			if (piece == hovered)
				return;

			if (hovered != null)
			{
				RendererTint.Clear(hovered.gameObject);
				hovered.GetComponent<Paintable>()?.Apply();
			}

			hovered = piece;

			if (hovered != null)
				RendererTint.Apply(hovered.gameObject, SpellColour, 0.8f);
		}
	}
}
