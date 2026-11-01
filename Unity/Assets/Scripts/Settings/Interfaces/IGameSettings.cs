using System;

namespace Settings.Interfaces
{
	public interface IGameSettings
	{
		/// <summary>
		/// Degrees the camera turns per unit of look input.
		/// </summary>
		float LookSensitivity { get; set; }

		bool InvertY { get; set; }

		/// <summary>
		/// 0-1.
		/// </summary>
		float MusicVolume { get; set; }

		/// <summary>
		/// 0-1.
		/// </summary>
		float SoundVolume { get; set; }

		/// <summary>
		/// Background sounds: footsteps, rain, birds, crickets, broom wind and the bubbling cauldron. 0-1.
		/// </summary>
		float AmbienceVolume { get; set; }

		event Action Changed;
	}
}
