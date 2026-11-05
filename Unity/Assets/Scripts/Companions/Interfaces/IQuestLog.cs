using System.Collections.Generic;
using Data.ScriptableObjects;

namespace Companions.Interfaces
{
	public interface IQuestLog
	{
		/// <summary>
		/// Whether they can be met yet: some companions can't be found until their storybook has been read.
		/// </summary>
		bool IsRevealed(CompanionDefinition companion);

		bool HasStarted(CompanionDefinition companion);

		/// <summary>
		/// The witch has met them, so their first step is under way, and they're the one the HUD follows.
		/// </summary>
		void Begin(CompanionDefinition companion);

		int StepIndex(CompanionDefinition companion);

		/// <summary>
		/// What they're asking for now, or null once everything's done.
		/// </summary>
		QuestStep Current(CompanionDefinition companion);

		bool IsMet(QuestStep step);

		/// <summary>
		/// Hands in the current step, taking its items. False if it isn't ready.
		/// </summary>
		bool TurnIn(CompanionDefinition companion);

		bool HasGoal(string goal);

		void MarkGoal(string goal);

		/// <summary>
		/// Companions who've been revealed and haven't moved in yet.
		/// </summary>
		IReadOnlyList<CompanionDefinition> Active { get; }

		/// <summary>
		/// The companion the HUD is following, or null when there's nobody to earn.
		/// </summary>
		CompanionDefinition Followed { get; }

		/// <summary>
		/// What to do next for this companion, in a line.
		/// </summary>
		string NextStep(CompanionDefinition companion);
	}
}
