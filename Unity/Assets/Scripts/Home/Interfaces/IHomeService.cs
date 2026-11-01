using Data.ScriptableObjects;
using UnityEngine;

namespace Home.Interfaces
{
	public interface IHomeService
	{
		int Level { get; }

		HomePlot Plot { get; }

		/// <summary>
		/// The next expansion, or null at the top level.
		/// </summary>
		HomeLevel NextLevel { get; }

		bool CanExpand(out string reason);

		bool Expand();

		/// <summary>
		/// Snaps a build piece to the home grid. False when outside the plot or the spot is taken.
		/// </summary>
		bool TrySnap(BuildPieceDefinition piece, Vector3 point, int rotationSteps, out Vector3 position, out Quaternion rotation);

		PlacedPiece Place(BuildPieceDefinition piece, Vector3 position, Quaternion rotation);

		void Remove(PlacedPiece piece, bool refund);

		/// <summary>
		/// Records a new resting place for a piece moved with Levitate.
		/// </summary>
		void NotifyMoved(PlacedPiece piece);
	}
}
