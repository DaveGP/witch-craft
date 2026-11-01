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

		public List<ItemStack> Cost = new();

		public string CostSummary
			=> Cost.Count == 0 ? "free" : string.Join(", ", Cost.Select(stack => stack.ToString()));
	}
}
