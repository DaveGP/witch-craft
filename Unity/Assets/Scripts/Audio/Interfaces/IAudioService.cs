using UnityEngine;

namespace Audio.Interfaces
{
	public interface IAudioService
	{
		/// <summary>
		/// The player's sound effects volume, 0-1, for things that run their own looping sources.
		/// </summary>
		float SoundLevel { get; }

		/// <summary>
		/// Plays a sound with no position, for the witch and the interface.
		/// </summary>
		void Play(Sound sound, float volume = 1f, float pitchJitter = 0.05f);

		/// <summary>
		/// Plays a sound from a place in the world.
		/// </summary>
		void PlayAt(Sound sound, Vector3 position, float volume = 1f);

		AudioClip Clip(Sound sound);
	}
}
