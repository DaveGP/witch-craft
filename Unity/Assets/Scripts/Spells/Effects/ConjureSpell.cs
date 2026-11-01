using System.Collections.Generic;
using System.Linq;
using Data.Enums;
using Data.ScriptableObjects;
using DependencyInjection;
using Home;
using Home.Interfaces;
using Items.Interfaces;
using Spells.Interfaces;
using UnityEngine;
using Utilities;

namespace Spells.Effects
{
	// Terra + Forma. The building spell: shows a ghost of the chosen piece snapped to the home grid and
	// conjures it, in the chosen colour, from materials in the satchel. Right-click opens the build catalogue
	// (pieces, colours, the paintbrush and undo); scroll steps through pieces, Q/R rotates, Z undoes the last
	// piece and middle-click copies a built piece's type, colour and rotation.
	public class ConjureSpell : SpellEffect, IBuildPalette
	{
		private const int UndoDepth = 30;

		public override SpellEffectType Type => SpellEffectType.Conjure;

		public override bool CapturesScroll => true;

		public override string Status
		{
			get
			{
				var colourName = HomePalette.Entries[colour].Name;

				if (painting)
					return hoveredPiece != null
						? $"Paint {hoveredPiece.Definition.DisplayName} {colourName}: click · right-click: catalogue"
						: $"Paintbrush ({colourName}): aim at something you built · right-click: catalogue";

				var piece = selected;

				if (piece == null)
					return "Conjure: nothing to build yet";

				return $"Conjure {piece.DisplayName} ({piece.CostSummary}) · {colourName} · scroll: next · Q/R: rotate · right-click: catalogue · Z: undo · middle-click: copy";
			}
		}

		public BuildPieceDefinition Selected => painting ? null : selected;

		public bool Painting => painting;

		public int Colour => colour;

		public bool CanUndo => history.Any(piece => piece != null);

		public IReadOnlyList<BuildPieceDefinition> AllPieces => Database.BuildPieces;

		public int HomeLevel => home?.Level ?? 1;

		private readonly Color validColour = new(0.55f, 1f, 0.65f, 0.45f);

		private readonly Color invalidColour = new(1f, 0.4f, 0.4f, 0.4f);

		private readonly List<PlacedPiece> history = new();

		private IInventory inventory;

		private IHomeService home;

		private BuildPieceDefinition selected;

		private bool painting;

		private int colour;

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

		private PlacedPiece hoveredPiece;

		protected override void Awake()
		{
			base.Awake();

			inventory = ServiceLocator.Get<IInventory>();

			if (inventory == null)
				Debug.LogError($"{nameof(inventory)} is null");
		}

		private bool Unlocked(BuildPieceDefinition piece) => home != null && piece.RequiredHomeLevel <= home.Level;

		protected override void OnBegin()
		{
			home = ServiceLocator.Get<IHomeService>();

			selected ??= Database.BuildPieces.FirstOrDefault(Unlocked);

			validMaterial ??= new Material(Database.GhostMaterial) { color = validColour };
			invalidMaterial ??= new Material(Database.GhostMaterial) { color = invalidColour };
		}

		protected override void OnEnd()
		{
			DestroyGhost();
			SetHovered(null);
		}

		#region Build palette

		public void Select(BuildPieceDefinition piece)
		{
			if (piece == null || !Unlocked(piece))
				return;

			selected = piece;
			painting = false;
		}

		public void SelectPaintbrush()
		{
			painting = true;
		}

		public void SetColour(int newColour)
		{
			colour = HomePalette.Clamp(newColour);
		}

		public override void Undo()
		{
			// Skip anything already unmade or moved out of existence
			while (history.Count > 0)
			{
				var last = history[^1];
				history.RemoveAt(history.Count - 1);

				if (last == null)
					continue;

				var name = last.Definition.DisplayName;

				home.Remove(last, true);

				EventBus.Notify($"Undid {name}. Materials returned.");
				return;
			}

			EventBus.Notify("Nothing to undo.");
		}

		#endregion

		public override void AltCast()
		{
			if (home != null)
				EventBus.OpenBuildCatalogue(this);
		}

		public override void Pick()
		{
			if (!Aim(out var hit))
				return;

			var piece = hit.collider.GetComponentInParent<PlacedPiece>();

			if (piece == null)
				return;

			selected = piece.Definition;
			painting = false;
			colour = piece.Record.Colour;

			var step = piece.Definition.Snap == BuildSnap.Free ? 45f : 90f;
			rotationSteps = Mathf.RoundToInt(piece.Record.Yaw / step);

			EventBus.Notify($"Copied {piece.Definition.DisplayName} ({HomePalette.Entries[colour].Name}).");
		}

		public override void Adjust(float rotate, float scroll)
		{
			if (Mathf.Abs(scroll) > 0.01f && !painting)
				Step(scroll > 0 ? 1 : -1);

			// Rotate is an axis; step once per press
			var sign = Mathf.Abs(rotate) > 0.5f ? Mathf.Sign(rotate) : 0f;

			if (sign != 0f && !Mathf.Approximately(sign, lastRotate))
				rotationSteps += (int)sign;

			lastRotate = sign;
		}

		// Scroll through unlocked pieces of the same kind as the current one
		private void Step(int direction)
		{
			if (selected == null)
				return;

			var pieces = Database.BuildPieces.Where(piece => piece.Category == selected.Category && Unlocked(piece)).ToList();

			var index = pieces.IndexOf(selected);

			selected = pieces[(index + direction + pieces.Count) % pieces.Count];
		}

		public override void Cast()
		{
			if (painting)
			{
				PaintHovered();
				return;
			}

			var piece = selected;

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

			var placed = home.Place(piece, spotPosition, spotRotation, colour);

			history.Add(placed);

			if (history.Count > UndoDepth)
				history.RemoveAt(0);
		}

		private void PaintHovered()
		{
			if (hoveredPiece == null)
			{
				EventBus.Notify("Aim at something you built to paint it.");
				return;
			}

			var piece = hoveredPiece;

			SetHovered(null);

			home.Paint(piece, colour);
		}

		private void LateUpdate()
		{
			if (!IsReadied || home == null)
				return;

			if (painting)
			{
				DestroyGhost();
				SetHovered(Aim(out var paintHit) ? paintHit.collider.GetComponentInParent<PlacedPiece>() : null);
				return;
			}

			SetHovered(null);

			UpdateGhost();
		}

		private void UpdateGhost()
		{
			var piece = selected;

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

		// Preview the paint on whatever the brush is over
		private void SetHovered(PlacedPiece piece)
		{
			if (piece == hoveredPiece)
				return;

			if (hoveredPiece != null)
			{
				RendererTint.Clear(hoveredPiece.gameObject);
				hoveredPiece.GetComponent<Paintable>()?.Apply();
			}

			hoveredPiece = piece;

			if (hoveredPiece != null)
				RendererTint.Apply(hoveredPiece.gameObject, colour == 0 ? Color.white : HomePalette.Entries[colour].Colour, 0.3f);
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

			// Keep the art swap so the ghost previews the real art, if any
			foreach (var behaviour in ghost.GetComponentsInChildren<MonoBehaviour>())
				if (behaviour is not Art.ArtSwap)
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
