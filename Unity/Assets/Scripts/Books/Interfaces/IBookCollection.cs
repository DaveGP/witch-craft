using System.Collections.Generic;
using Data.ScriptableObjects;

namespace Books.Interfaces
{
	public interface IBookCollection
	{
		IReadOnlyList<StoryBookDefinition> AllBooks { get; }

		bool Has(StoryBookDefinition book);

		int Count { get; }

		/// <summary>
		/// Adds a found book to the bookcase. Returns false if it was already there.
		/// </summary>
		bool Collect(StoryBookDefinition book);
	}
}
