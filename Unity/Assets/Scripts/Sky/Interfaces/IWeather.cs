using Data.Enums;

namespace Sky.Interfaces
{
	public interface IWeather
	{
		WeatherKind Current { get; }

		bool IsRaining { get; }

		/// <summary>
		/// How grey the sky is, 0-1, eased so changes are gradual.
		/// </summary>
		float Overcast { get; }

		void SetWeather(WeatherKind weather);
	}
}
