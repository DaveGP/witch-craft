using System.Collections.Generic;
using System.Linq;
using Alchemy.Interfaces;
using Data.Save;
using Data.ScriptableObjects;
using Events.Interfaces;
using Persistence.Interfaces;
using Spells.Interfaces;


namespace Companions
{
	using Interfaces;

	// The magical friends who have moved into the witch's home, and how fond of her they are.
	public class CompanionRoster : ICompanionRoster, ISaveParticipant
	{
		private readonly GameDatabase database;

		private readonly IEventBusSender eventBus;

		private readonly ISpellBook spellBook;

		private readonly IRecipeBook recipeBook;

		private readonly List<CompanionRecord> records = new();

		public IReadOnlyList<CompanionDefinition> Residents
			=> records.Select(record => database.FindCompanion(record.Id)).Where(companion => companion != null).ToList();

		public CompanionRoster(GameDatabase database, IEventBusSender eventBus, ISpellBook spellBook, IRecipeBook recipeBook)
		{
			this.database = database;
			this.eventBus = eventBus;
			this.spellBook = spellBook;
			this.recipeBook = recipeBook;
		}

		public bool IsResident(CompanionDefinition companion) => Find(companion) != null;

		public void Recruit(CompanionDefinition companion)
		{
			if (companion == null || IsResident(companion))
				return;

			records.Add(new CompanionRecord { Id = companion.Id });

			eventBus.RecruitCompanion(companion);
			eventBus.Notify($"{companion.DisplayName} is moving into your home!");
		}

		public int Friendship(CompanionDefinition companion) => Find(companion)?.Friendship ?? 0;

		public IReadOnlyList<FriendshipReward> AddFriendship(CompanionDefinition companion, int amount)
		{
			var record = Find(companion);

			var granted = new List<FriendshipReward>();

			if (record == null)
				return granted;

			record.Friendship += amount;

			for (var i = 0; i < companion.Rewards.Count; i++)
			{
				var reward = companion.Rewards[i];

				if (record.Friendship < reward.Friendship || record.ClaimedRewards.Contains(i))
					continue;

				record.ClaimedRewards.Add(i);

				recipeBook.Learn(reward.Recipe);
				spellBook.LearnGlyph(reward.Glyph);

				granted.Add(reward);
			}

			return granted;
		}

		private CompanionRecord Find(CompanionDefinition companion)
			=> companion == null ? null : records.FirstOrDefault(record => record.Id == companion.Id);

		public void Read(SaveData data)
		{
			records.Clear();

			records.AddRange(data.Companions.Select(record => new CompanionRecord
			{
				Id = record.Id,
				Friendship = record.Friendship,
				ClaimedRewards = record.ClaimedRewards.ToList()
			}));
		}

		public void Write(SaveData data)
		{
			data.Companions = records.ToList();
		}
	}
}
