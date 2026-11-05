using System;
using UnityEngine;

namespace Data.ScriptableObjects
{
	// One thing a companion asks before moving in. It's ready to hand in when everything it names is true: the items
	// are in the satchel, the piece is built at home and the goal has been reached. Handing in takes the items.
	[Serializable]
	public class QuestStep
	{
		[Tooltip("What's left to do, for the HUD and the journal")]
		public string Objective;

		[Tooltip("What the companion says to ask for it, and to remind")]
		[TextArea] public string Ask;

		[Tooltip("The witch's line when handing it in")]
		public string Offer;

		[TextArea] public string Thanks;

		public ItemStack Needs = new();

		public BuildPieceDefinition Piece;

		[Tooltip("Set by something in the world, such as a boulder moved aside")]
		public string Goal;
	}
}
