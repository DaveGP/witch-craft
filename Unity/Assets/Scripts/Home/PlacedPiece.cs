using Data.Save;
using Data.ScriptableObjects;
using UnityEngine;

namespace Home
{
	// Marks something the witch built, tying the scene object to its save record.
	public class PlacedPiece : MonoBehaviour
	{
		public BuildPieceDefinition Definition { get; private set; }

		public PieceRecord Record { get; private set; }

		public void Initialise(BuildPieceDefinition definition, PieceRecord record)
		{
			Definition = definition;
			Record = record;
		}
	}
}
