using System.Collections.Generic;
using System.Linq;
using Books.Interfaces;
using Data.Save;
using Data.ScriptableObjects;
using DependencyInjection;
using Home.Interfaces;
using Items.Interfaces;
using Persistence.Interfaces;


namespace Companions
{
	using Interfaces;

	// How far the witch has got with earning each companion. Steps are checks on game state, so doing something
	// before being asked still counts. Companions who moved in under older saves simply have nothing left to do.
	public class QuestLog : IQuestLog, ISaveParticipant
	{
		private readonly GameDatabase database;

		private readonly IInventory inventory;

		private readonly IBookCollection books;

		private readonly ICompanionRoster roster;

		private readonly List<QuestRecord> records = new();

		private readonly HashSet<string> goals = new();

		private string followed = "";

		public QuestLog(GameDatabase database, IInventory inventory, IBookCollection books, ICompanionRoster roster)
		{
			this.database = database;
			this.inventory = inventory;
			this.books = books;
			this.roster = roster;
		}

		public IReadOnlyList<CompanionDefinition> Active
			=> database.Companions.Where(companion => companion.Quests.Count > 0 && IsRevealed(companion) && !roster.IsResident(companion)).ToList();

		public CompanionDefinition Followed
		{
			get
			{
				var active = Active;

				return active.FirstOrDefault(companion => companion.Id == followed) ?? active.FirstOrDefault();
			}
		}

		public bool IsRevealed(CompanionDefinition companion) => companion.RevealBook == null || books.Has(companion.RevealBook);

		public bool HasStarted(CompanionDefinition companion) => Find(companion) != null;

		public void Begin(CompanionDefinition companion)
		{
			followed = companion.Id;

			if (!HasStarted(companion))
				records.Add(new QuestRecord { Id = companion.Id });
		}

		public int StepIndex(CompanionDefinition companion) => Find(companion)?.Step ?? 0;

		public QuestStep Current(CompanionDefinition companion)
		{
			var index = StepIndex(companion);

			return index < companion.Quests.Count ? companion.Quests[index] : null;
		}

		public bool IsMet(QuestStep step)
		{
			if (step == null)
				return false;

			if (step.Needs?.Item != null && inventory.Count(step.Needs.Item) < step.Needs.Amount)
				return false;

			if (step.Piece != null && ServiceLocator.Get<IHomeService>()?.Has(step.Piece) != true)
				return false;

			return string.IsNullOrEmpty(step.Goal) || HasGoal(step.Goal);
		}

		public bool TurnIn(CompanionDefinition companion)
		{
			var record = Find(companion);
			var step = Current(companion);

			if (record == null || !IsMet(step))
				return false;

			if (step.Needs?.Item != null)
				inventory.Remove(step.Needs.Item, step.Needs.Amount);

			record.Step++;

			return true;
		}

		public bool HasGoal(string goal) => goals.Contains(goal);

		public void MarkGoal(string goal) => goals.Add(goal);

		public string NextStep(CompanionDefinition companion)
		{
			if (!HasStarted(companion))
				return $"Find {companion.DisplayName} {companion.Whereabouts}";

			var step = Current(companion);

			return IsMet(step) ? $"Go back to {companion.DisplayName}" : step?.Objective ?? "";
		}

		private QuestRecord Find(CompanionDefinition companion)
			=> companion == null ? null : records.FirstOrDefault(record => record.Id == companion.Id);

		public void Read(SaveData data)
		{
			records.Clear();
			records.AddRange(data.Quests.Select(record => new QuestRecord { Id = record.Id, Step = record.Step }));

			goals.Clear();
			goals.UnionWith(data.QuestGoals);

			followed = data.FollowedQuest;
		}

		public void Write(SaveData data)
		{
			data.Quests = records.ToList();
			data.QuestGoals = goals.ToList();
			data.FollowedQuest = followed;
		}
	}
}
