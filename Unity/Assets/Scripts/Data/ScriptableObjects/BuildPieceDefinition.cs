using System.Collections.Generic;
using System.Linq;
using Data.Enums;
using UnityEngine;

namespace Data.ScriptableObjects
{
	[CreateAssetMenu(fileName = "BuildPiece", menuName = "Witch Craft/Build Piece", order = 14)]
	public class BuildPieceDefinition : ScriptableObject
	{
		public string Id;

		public string DisplayName;

		public BuildCategory Category;

		public BuildSnap Snap;

		public GameObject Prefab;

		public int RequiredHomeLevel = 1;

		[Tooltip("How much this piece adds to the home's comfort")]
		public int Comfort;

		public List<ItemStack> Cost = new();

		public string CostSummary
			=> Cost.Count == 0 ? "free" : string.Join(", ", Cost.Select(stack => stack.ToString()));
	}
}
