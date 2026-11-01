using System;
using UnityEngine;


namespace Settings
{
	using Interfaces;

	// Player preferences, kept per machine (per browser on the web) rather than per save slot.
	public class GameSettings : IGameSettings
	{
		public const float DefaultLookSensitivity = 0.12f;

		private const string LookKey = "WitchCraft.LookSensitivity";
		private const string InvertKey = "WitchCraft.InvertY";
		private const string MusicKey = "WitchCraft.MusicVolume";
		private const string SoundKey = "WitchCraft.SoundVolume";
		private const string AmbienceKey = "WitchCraft.AmbienceVolume";

		public event Action Changed;

		public float LookSensitivity
		{
			get => PlayerPrefs.GetFloat(LookKey, DefaultLookSensitivity);
			set => Store(() => PlayerPrefs.SetFloat(LookKey, Mathf.Clamp(value, 0.02f, 0.5f)));
		}

		public bool InvertY
		{
			get => PlayerPrefs.GetInt(InvertKey, 0) == 1;
			set => Store(() => PlayerPrefs.SetInt(InvertKey, value ? 1 : 0));
		}

		public float MusicVolume
		{
			get => PlayerPrefs.GetFloat(MusicKey, 1f);
			set => Store(() => PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)));
		}

		public float SoundVolume
		{
			get => PlayerPrefs.GetFloat(SoundKey, 1f);
			set => Store(() => PlayerPrefs.SetFloat(SoundKey, Mathf.Clamp01(value)));
		}

		public float AmbienceVolume
		{
			get => PlayerPrefs.GetFloat(AmbienceKey, 1f);
			set => Store(() => PlayerPrefs.SetFloat(AmbienceKey, Mathf.Clamp01(value)));
		}

		private void Store(Action write)
		{
			write();
			PlayerPrefs.Save();
			Changed?.Invoke();
		}
	}
}
