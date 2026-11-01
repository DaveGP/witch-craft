using System.Collections.Generic;
using Data.ScriptableObjects;

namespace Companions.Interfaces
{
	public interface ICompanionRoster
	{
		IReadOnlyList<CompanionDefinition> Residents { get; }

		bool IsResident(CompanionDefinition companion);

		void Recruit(CompanionDefinition companion);

		int Friendship(CompanionDefinition companion);

		/// <summary>
		/// Raises friendship and grants any rewards newly reached. Returns those rewards.
		/// </summary>
		IReadOnlyList<FriendshipReward> AddFriendship(CompanionDefinition companion, int amount);
	}
}
