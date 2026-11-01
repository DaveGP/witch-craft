using System.Collections.Generic;
using System.Linq;
using Data.Enums;
using Data.ScriptableObjects;
using DependencyInjection;
using Home;
using Home.Interfaces;
using Items.Interfaces;
using UnityEngine;
using Utilities;

namespace Spells.Effects
{
	// Terra + Forma. The building spell: shows a ghost of the chosen piece snapped to the home grid and
	// conjures it from materials in the satchel. Scroll picks the piece, right-click swaps between
	// structures and furniture, Q/R rotates.
	public class ConjureSpell : SpellEffect
	{
		public override SpellEffectType Type => SpellEffectType.Conjure;

		public override bool CapturesScroll => true;

		public override string Status
		{
			get
			{
				var piece = Current;

				if (piece == null)
					return "Conjure: nothing to build yet";

				var other = category == BuildCategory.Structure ? "furniture" : "structures";

				return $"Conjure {piece.DisplayName} ({piece.CostSummary}) · scroll: next · Q/R: rotate · right-click: {other}";
			}
		}

		private readonly Color validColour = new(0.55f, 1f, 0.65f, 0.45f);

		private readonly Color invalidColour = new(1f, 0.4f, 0.4f, 0.4f);

		private IInventory inventory;

		private IHomeService home;

		private BuildCategory category = BuildCategory.Structure;

		private int index;

		private int rotationSteps;

		private float lastRotate;

		private GameObject ghost;

		private BuildPieceDefinition ghostPiece;

		private Material validMaterial;

		private Material invalidMaterial;

		private bool hasSpot;

		private bool inPlot;

		private Vector3 spotPosition;

		private Quaternion spotRotation;

		protected override void Awake()
		{
			base.Awake();

			inventory = ServiceLocator.Get<IInventory>();

			if (inventory == null)
				Debug.LogError($"{nameof(inventory)} is null");
		}

		private List<BuildPieceDefinition> Pieces
			=> home == null
				? new List<BuildPieceDefinition>()
				: Database.BuildPieces.Where(piece => piece.Category == category && piece.RequiredHomeLevel <= home.Level).ToList();

		private BuildPieceDefinition Current
		{
			get
			{
				var pieces = Pieces;

				return pieces.Count == 0 ? null : pieces[Mathf.Abs(index) % pieces.Count];
			}
		}

		protected override void OnBegin()
		{
			home = ServiceLocator.Get<IHomeService>();

			validMaterial ??= new Material(Database.GhostMaterial) { color = validColour };
			invalidMaterial ??= new Material(Database.GhostMaterial) { color = invalidColour };
		}

		protected override void OnEnd()
		{
			DestroyGhost();
		}

		public override void AltCast()
		{
			category = category == BuildCategory.Structure ? BuildCategory.Furniture : BuildCategory.Structure;
			index = 0;
		}

		public override void Adjust(float rotate, float scroll)
		{
			if (Mathf.Abs(scroll) > 0.01f)
				index += scroll > 0 ? 1 : -1;

			// Rotate is an axis; step once per press
			var sign = Mathf.Abs(rotate) > 0.5f ? Mathf.Sign(rotate) : 0f;

			if (sign != 0f && !Mathf.Approximately(sign, lastRotate))
				rotationSteps += (int)sign;

			lastRotate = sign;
		}

		public override void Cast()
		{
			var piece = Current;

			if (piece == null)
				return;

			if (!inPlot)
			{
				EventBus.Notify("You can only conjure on your home plot.");
				return;
			}

			if (!hasSpot)
			{
				EventBus.Notify("Something is already built there.");
				return;
			}

			if (!inventory.Has(piece.Cost))
			{
				EventBus.Notify($"You need {piece.CostSummary} to conjure a {piece.DisplayName}.");
				return;
			}

			inventory.RemoveAll(piece.Cost);

			home.Place(piece, spotPosition, spotRotation);
		}

		private void LateUpdate()
		{
			if (!IsReadied || home == null)
				return;

			var piece = Current;

			if (piece != ghostPiece)
				CreateGhost(piece);

			if (ghost == null)
				return;

			if (!Aim(out var hit, LayerUtility.SurfaceMask))
			{
				ghost.SetActive(false);
				hasSpot = inPlot = false;
				return;
			}

			// Nudge off the surface so aiming at a wall top or floor face picks the right storey
			var point = piece.Snap == BuildSnap.Free ? hit.point : hit.point + hit.normal * 0.05f;

			inPlot = home.Plot.Contains(point);

			hasSpot = home.TrySnap(piece, point, rotationSteps, out spotPosition, out spotRotation);

			if (!inPlot)
			{
				spotPosition = point;
				spotRotation = Quaternion.Euler(0f, rotationSteps * 90f, 0f);
			}

			ghost.SetActive(true);
			ghost.transform.SetPositionAndRotation(spotPosition, spotRotation);

			var valid = inPlot && hasSpot && inventory.Has(piece.Cost);

			RendererTint.SetMaterial(ghost, valid ? validMaterial : invalidMaterial);
		}

		private void CreateGhost(BuildPieceDefinition piece)
		{
			DestroyGhost();

			ghostPiece = piece;

			if (piece == null || piece.Prefab == null)
				return;

			ghost = Instantiate(piece.Prefab);
			ghost.name = $"Ghost_{piece.DisplayName}";

			// A ghost is only a picture: no physics, no behaviour, no light
			foreach (var collider in ghost.GetComponentsInChildren<Collider>())
				DestroyImmediate(collider);

			foreach (var behaviour in ghost.GetComponentsInChildren<MonoBehaviour>())
				DestroyImmediate(behaviour);

			foreach (var light in ghost.GetComponentsInChildren<Light>())
				light.enabled = false;

			LayerUtility.SetLayerRecursively(ghost, LayerMask.NameToLayer("Ignore Raycast"));
		}

		private void DestroyGhost()
		{
			if (ghost != null)
				Destroy(ghost);

			ghost = null;
			ghostPiece = null;
		}
	}
}
