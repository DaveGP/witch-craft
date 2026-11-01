using System.Collections.Generic;
using System.Linq;
using Data.Enums;
using Data.Save;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using Companions.Interfaces;
using Items.Interfaces;
using Persistence.Interfaces;
using UnityEngine;
using Utilities;


namespace Home
{
	using Interfaces;

	// Owns everything built on the home plot: spawns it from the save, tracks placement and expansion.
	public class HomeService : MonoBehaviour, IHomeService, ISaveParticipant
	{
		public int Level => record.Level;

		public HomePlot Plot => plot;

		public HomeLevel NextLevel => Level < database.HomeLevels.Count ? database.HomeLevels[Level] : null;

		[SerializeField] private HomePlot plot;

		[Tooltip("Pieces built for a brand new game: a little starter hut")]
		[SerializeField] private List<StarterPiece> starterPieces = new();

		private readonly List<PlacedPiece> pieces = new();

		private GameDatabase database;

		private IEventBusSender eventBus;

		private IInventory inventory;

		private ICompanionRoster roster;

		private ISaveService saveService;

		private HomeRecord record = new();

		private void Awake()
		{
			database = ServiceLocator.Get<GameDatabase>();
			eventBus = ServiceLocator.Get<IEventBusSender>();
			inventory = ServiceLocator.Get<IInventory>();
			roster = ServiceLocator.Get<ICompanionRoster>();
			saveService = ServiceLocator.Get<ISaveService>();

			if (plot == null)
				Debug.LogError($"{nameof(plot)} is null");

			if (saveService == null)
				Debug.LogError($"{nameof(saveService)} is null");

			ServiceLocator.Register<IHomeService>(this);
		}

		private void Start()
		{
			// Registering reads the save, which builds the home
			saveService.Register(this);
		}

		private void OnDestroy()
		{
			saveService?.Unregister(this);

			ServiceLocator.Unregister<IHomeService>(this);
		}

		public bool CanExpand(out string reason)
		{
			var next = NextLevel;

			if (next == null)
			{
				reason = "Your home is as grand as it can be.";
				return false;
			}

			if (roster.Residents.Count < next.RequiredCompanions)
			{
				reason = $"Needs {next.RequiredCompanions} companions living with you (you have {roster.Residents.Count}).";
				return false;
			}

			if (!inventory.Has(next.Cost))
			{
				reason = $"Needs {string.Join(", ", next.Cost.Select(stack => stack.ToString()))}.";
				return false;
			}

			reason = "";
			return true;
		}

		public bool Expand()
		{
			if (!CanExpand(out var reason))
			{
				eventBus.Notify(reason);
				return false;
			}

			inventory.RemoveAll(NextLevel.Cost);

			record.Level++;

			ApplyLevel();

			FxUtility.Burst(plot.transform.position + Vector3.up, new Color(1f, 0.85f, 0.5f), 120, 10f, 0.3f);

			eventBus.ChangeHomeLevel(Level);
			eventBus.Notify($"Your home plot grows! New pieces to build are in your Conjure spell.");

			return true;
		}

		public bool TrySnap(BuildPieceDefinition piece, Vector3 point, int rotationSteps, out Vector3 position, out Quaternion rotation)
		{
			if (!plot.Snap(piece.Snap, point, rotationSteps, out position, out rotation))
				return false;

			if (piece.Snap == BuildSnap.Free)
				return true;

			var snappedPosition = position;
			var snappedRotation = rotation;

			// One structural piece per slot: same kind of snap, same place, and for walls the same edge
			return !pieces.Any(placed =>
				placed != null
				&& placed.Definition.Snap == piece.Snap
				&& (placed.transform.position - snappedPosition).sqrMagnitude < 0.05f
				&& (piece.Snap != BuildSnap.Wall || Quaternion.Angle(placed.transform.rotation, snappedRotation) < 1f || Quaternion.Angle(placed.transform.rotation, snappedRotation) > 179f));
		}

		public PlacedPiece Place(BuildPieceDefinition piece, Vector3 position, Quaternion rotation)
		{
			var pieceRecord = new PieceRecord { Id = piece.Id, Position = position, Yaw = rotation.eulerAngles.y };

			record.Pieces.Add(pieceRecord);

			FxUtility.Burst(position + Vector3.up * 0.5f, new Color(0.8f, 0.6f, 1f), 30, 3f);

			eventBus.PlacePiece(position);

			return Spawn(piece, pieceRecord);
		}

		public void Remove(PlacedPiece piece, bool refund)
		{
			if (piece == null)
				return;

			if (refund)
				foreach (var stack in piece.Definition.Cost)
					inventory.Add(stack.Item, stack.Amount);

			record.Pieces.Remove(piece.Record);
			pieces.Remove(piece);

			FxUtility.Burst(piece.transform.position + Vector3.up * 0.5f, new Color(0.5f, 0.4f, 0.7f), 30, 3f);

			eventBus.RemovePiece(piece.transform.position);

			Destroy(piece.gameObject);
		}

		public void NotifyMoved(PlacedPiece piece)
		{
			if (piece == null)
				return;

			piece.Record.Position = piece.transform.position;
			piece.Record.Yaw = piece.transform.eulerAngles.y;
		}

		private PlacedPiece Spawn(BuildPieceDefinition piece, PieceRecord pieceRecord)
		{
			var instance = Instantiate(piece.Prefab, pieceRecord.Position, Quaternion.Euler(0f, pieceRecord.Yaw, 0f), transform);

			instance.name = piece.DisplayName;

			var placed = instance.GetComponent<PlacedPiece>() ?? instance.AddComponent<PlacedPiece>();

			placed.Initialise(piece, pieceRecord);

			if (piece.Category == BuildCategory.Furniture && instance.GetComponent<Movable>() == null)
				instance.AddComponent<Movable>();

			pieces.Add(placed);

			return placed;
		}

		private void ApplyLevel()
		{
			var level = database.HomeLevels.ElementAtOrDefault(Level - 1);

			plot.SetCells(level?.PlotCells ?? 8);
		}

		public void Read(SaveData data)
		{
			foreach (var piece in pieces.Where(piece => piece != null))
				Destroy(piece.gameObject);

			pieces.Clear();

			record = data.Home;

			if (!record.Seeded)
				SeedStarterHome();

			foreach (var pieceRecord in record.Pieces)
			{
				var definition = database.FindBuildPiece(pieceRecord.Id);

				if (definition != null)
					Spawn(definition, pieceRecord);
			}

			ApplyLevel();
		}

		private void SeedStarterHome()
		{
			record.Seeded = true;

			foreach (var starter in starterPieces.Where(starter => starter.Piece != null))
				record.Pieces.Add(new PieceRecord
				{
					Id = starter.Piece.Id,
					Position = plot.transform.position + starter.Offset,
					Yaw = starter.Yaw
				});
		}

		public void Write(SaveData data)
		{
			data.Home = record;
		}

		[System.Serializable]
		public class StarterPiece
		{
			public BuildPieceDefinition Piece;

			public Vector3 Offset;

			public float Yaw;
		}
	}
}
