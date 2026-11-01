using System.Collections.Generic;
using Data.ScriptableObjects;

namespace Spells.Interfaces
{
	/// <summary>
	/// What the build catalogue shows and changes on the Conjure spell.
	/// </summary>
	public interface IBuildPalette
	{
		/// <summary>
		/// The piece being conjured, or null while the paintbrush is in hand.
		/// </summary>
		BuildPieceDefinition Selected { get; }

		bool Painting { get; }

		int Colour { get; }

		bool CanUndo { get; }

		IReadOnlyList<BuildPieceDefinition> AllPieces { get; }

		int HomeLevel { get; }

		void Select(BuildPieceDefinition piece);

		void SelectPaintbrush();

		void SetColour(int colour);

		void Undo();
	}
}
