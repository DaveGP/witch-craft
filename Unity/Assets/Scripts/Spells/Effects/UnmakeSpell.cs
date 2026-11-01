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

		public override string Status => hovered != null
			? $"Unmake {hovered.Definition.DisplayName}: click to unravel it (returns {hovered.Definition.CostSummary})"
			: "Unmake: aim at something you built";

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
