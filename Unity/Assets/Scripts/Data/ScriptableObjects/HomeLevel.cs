using System;
using System.Collections.Generic;

namespace Data.ScriptableObjects
{
	[Serializable]
	public class HomeLevel
	{
		public int PlotCells = 8;

		public int RequiredCompanions;

		public List<ItemStack> Cost = new();
	}
}
