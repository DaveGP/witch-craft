using System.Collections.Generic;
using System.Linq;
using Data.Save;
using Data.ScriptableObjects;
using Events.Interfaces;
using Persistence.Interfaces;


namespace Books
{
	using Interfaces;

	// The storybooks the witch has found, kept on her bookcase at home.
	public class BookCollection : IBookCollection, ISaveParticipant
	{
		private readonly GameDatabase database;

		private readonly IEventBusSender eventBus;

		private readonly List<StoryBookDefinition> found = new();

		public IReadOnlyList<StoryBookDefinition> AllBooks => database.Books;

		public int Count => found.Count;

		public BookCollection(GameDatabase database, IEventBusSender eventBus)
		{
			this.database = database;
			this.eventBus = eventBus;
		}

		public bool Has(StoryBookDefinition book) => found.Contains(book);

		public bool Collect(StoryBookDefinition book)
		{
			if (book == null || Has(book))
				return false;

			found.Add(book);

			eventBus.Notify($"Found \"{book.Title}\" ({found.Count} of {database.Books.Count}). It's been added to your bookcase at home.");

			return true;
		}

		public void Read(SaveData data)
		{
			found.Clear();
			found.AddRange(data.Books.Select(database.FindBook).Where(book => book != null));
		}

		public void Write(SaveData data)
		{
			data.Books = found.Select(book => book.Id).ToList();
		}
	}
}
