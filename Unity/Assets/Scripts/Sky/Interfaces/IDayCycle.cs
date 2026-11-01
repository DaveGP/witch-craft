namespace Sky.Interfaces
{
	public interface IDayCycle
	{
		/// <summary>
		/// Time of day in hours, 0-24.
		/// </summary>
		float Hour { get; }

		int Day { get; }

		bool IsNight { get; }

		/// <summary>
		/// 0 in full daylight, 1 in the dead of night, easing through dusk and dawn.
		/// </summary>
		float Darkness { get; }

		/// <summary>
		/// Morning, Afternoon, Evening or Night.
		/// </summary>
		string PeriodName { get; }

		/// <summary>
		/// Moves time forward to the given hour, into the next day if it has passed.
		/// </summary>
		void SkipTo(float hour);
	}
}
