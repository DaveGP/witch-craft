namespace Character.Movement.Interfaces
{
	public interface IBroomState
	{
		bool IsMounted { get; }

		bool IsBoosting { get; }

		/// <summary>
		/// Remaining flight charge, 0-1.
		/// </summary>
		float Charge01 { get; }
	}
}
