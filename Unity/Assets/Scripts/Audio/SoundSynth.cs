using System;
using UnityEngine;
using Random = System.Random;

namespace Audio
{
	// Generates every sound and the music loop from code, so the game ships with no audio files.
	// Simple building blocks: oscillators, filtered noise, pitch sweeps and envelopes.
	public static class SoundSynth
	{
		public const int SampleRate = 22050;

		private const float Tau = Mathf.PI * 2f;

		public const int StepVariants = 4;

		/// <summary>
		/// One of several footsteps, so walking doesn't repeat the same sound.
		/// </summary>
		public static AudioClip CreateStep(int variant)
		{
			var samples = Step(variant);

			Normalise(samples, 0.9f);

			var clip = AudioClip.Create($"Step{variant}", samples.Length, 1, SampleRate, false);
			clip.SetData(samples, 0);

			return clip;
		}

		public static AudioClip Create(Sound sound)
		{
			var samples = sound switch
			{
				Sound.Step => Step(0),
				Sound.Jump => Sweep(0.18f, 320f, 760f, 0.6f),
				Sound.Land => Land(),
				Sound.Pickup => Notes(0.45f, 0.07f, 1046.5f, 1568f),
				Sound.Learn => Notes(1.1f, 0.09f, 523.3f, 659.3f, 784f, 1046.5f),
				Sound.Click => Click(),
				Sound.Build => Build(),
				Sound.Unbuild => Sweep(0.22f, 900f, 260f, 0.5f),
				Sound.Cast => Cast(),
				Sound.Whoosh => Whoosh(0.4f),
				Sound.Boing => Boing(),
				Sound.Grind => Grind(),
				Sound.Purr => Purr(),
				Sound.Chirp => Chirp(),
				Sound.BroomWind => Loop(Wind(2f)),
				Sound.Bubbling => Loop(Bubbles(2.5f)),
				Sound.Rain => Loop(Rain(3f)),
				Sound.Cricket => Cricket(),
				Sound.Music => Music(),
				Sound.Hiss => Hiss(),
				_ => new float[1]
			};

			Normalise(samples, sound == Sound.Music ? 0.8f : 0.9f);

			var clip = AudioClip.Create(sound.ToString(), samples.Length, 1, SampleRate, false);
			clip.SetData(samples, 0);

			return clip;
		}

		#region Sounds

		// A foot on grass: a soft, muffled heel then a brushy crunch of blades, with no hard attack
		private static float[] Step(int variant)
		{
			var samples = Buffer(0.14f);
			var noise = new Random(100 + variant * 17);

			var heelPitch = 70f + variant * 6f;
			var crunchStart = 0.018f + variant * 0.004f;

			var low = 0f;
			var lower = 0f;
			var phase = 0f;

			for (var i = 0; i < samples.Length; i++)
			{
				var t = Time(i);

				phase += Tau * heelPitch * (1f - t * 2f) / SampleRate;
				var heel = Mathf.Sin(phase) * Decay(t, 0.006f, 0.03f) * 0.35f;

				// Band of mid frequencies: the rustle of grass rather than a thump or a hiss
				var raw = Noise(noise);
				low += (raw - low) * 0.32f;
				lower += (low - lower) * 0.06f;
				var band = low - lower;

				var crunchTime = t - crunchStart;
				var crunch = crunchTime < 0f ? 0f : band * Decay(crunchTime, 0.012f, 0.045f) * (0.75f + 0.25f * Mathf.Sin(Tau * 55f * crunchTime));

				samples[i] = heel + crunch;
			}

			return samples;
		}

		private static float[] Land()
		{
			var samples = Buffer(0.16f);
			var noise = new Random(2);
			var low = 0f;
			var phase = 0f;

			for (var i = 0; i < samples.Length; i++)
			{
				var t = Time(i);
				phase += Tau * Mathf.Lerp(130f, 55f, t / 0.16f) / SampleRate;
				low += (Noise(noise) - low) * 0.1f;
				samples[i] = (Mathf.Sin(phase) * 0.8f + low * 0.6f) * Decay(t, 0.002f, 0.1f);
			}

			return samples;
		}

		private static float[] Sweep(float length, float from, float to, float harmonic)
		{
			var samples = Buffer(length);
			var phase = 0f;

			for (var i = 0; i < samples.Length; i++)
			{
				var t = Time(i);
				var progress = t / length;
				phase += Tau * Mathf.Lerp(from, to, progress * progress * (3f - 2f * progress)) / SampleRate;
				samples[i] = (Mathf.Sin(phase) + Mathf.Sin(phase * 2f) * harmonic * 0.3f) * Decay(t, 0.01f, length * 0.7f);
			}

			return samples;
		}

		// Bell-like notes in sequence, each ringing on under the next
		private static float[] Notes(float length, float spacing, params float[] frequencies)
		{
			var samples = Buffer(length + spacing * frequencies.Length);

			for (var n = 0; n < frequencies.Length; n++)
			{
				var start = (int)(spacing * n * SampleRate);

				for (var i = start; i < samples.Length; i++)
				{
					var t = (i - start) / (float)SampleRate;
					samples[i] += Bell(frequencies[n], t) * Decay(t, 0.004f, length * 0.6f) * 0.6f;
				}
			}

			return samples;
		}

		private static float[] Click()
		{
			var samples = Buffer(0.04f);

			for (var i = 0; i < samples.Length; i++)
			{
				var t = Time(i);
				samples[i] = Triangle(1300f * t) * Decay(t, 0.001f, 0.02f);
			}

			return samples;
		}

		private static float[] Build()
		{
			var pop = Sweep(0.16f, 380f, 980f, 0.4f);
			var chime = Notes(0.3f, 0.05f, 1318.5f, 1760f);
			var samples = Buffer(0.4f);

			for (var i = 0; i < samples.Length; i++)
				samples[i] = (i < pop.Length ? pop[i] : 0f) + (i < chime.Length ? chime[i] * 0.4f : 0f);

			return samples;
		}

		private static float[] Cast()
		{
			const float length = 0.55f;

			var samples = Buffer(length);
			var noise = new Random(3);
			var high = 0f;

			for (var i = 0; i < samples.Length; i++)
			{
				var t = Time(i);
				var vibrato = 1f + Mathf.Sin(Tau * 9f * t) * 0.02f;
				var shimmer = Mathf.Sin(Tau * 1568f * vibrato * t) + Mathf.Sin(Tau * 2093f * vibrato * t) * 0.6f + Mathf.Sin(Tau * 2637f * vibrato * t) * 0.4f;

				var raw = Noise(noise);
				high = raw - (raw - high) * 0.5f;

				samples[i] = (shimmer * 0.4f + high * 0.15f) * Decay(t, 0.02f, 0.35f);
			}

			return samples;
		}

		private static float[] Whoosh(float length)
		{
			var samples = Buffer(length);
			var noise = new Random(4);
			var low = 0f;
			var band = 0f;

			for (var i = 0; i < samples.Length; i++)
			{
				var t = Time(i);
				var progress = t / length;
				var cutoff = Mathf.Lerp(0.04f, 0.25f, Mathf.Sin(progress * Mathf.PI));

				low += (Noise(noise) - low) * cutoff;
				band += (low - band) * 0.5f;

				samples[i] = (low - band * 0.5f) * Mathf.Sin(progress * Mathf.PI);
			}

			return samples;
		}

		private static float[] Boing()
		{
			const float length = 0.45f;

			var samples = Buffer(length);
			var phase = 0f;

			for (var i = 0; i < samples.Length; i++)
			{
				var t = Time(i);
				var wobble = 1f + Mathf.Sin(Tau * 14f * t) * 0.12f * (1f - t / length);
				phase += Tau * Mathf.Lerp(140f, 520f, Mathf.Sqrt(t / length)) * wobble / SampleRate;
				samples[i] = Mathf.Sin(phase) * Decay(t, 0.005f, 0.3f);
			}

			return samples;
		}

		private static float[] Grind()
		{
			var samples = Buffer(0.18f);
			var noise = new Random(5);

			for (var i = 0; i < samples.Length; i++)
			{
				var t = Time(i);
				var grit = Mathf.Sin(Tau * 38f * t) > 0.2f ? 1f : 0.3f;
				samples[i] = Noise(noise) * grit * Decay(t, 0.01f, 0.12f) * 0.6f;
			}

			return samples;
		}

		private static float[] Purr()
		{
			const float length = 1.4f;

			var samples = Buffer(length);
			var noise = new Random(6);
			var low = 0f;

			for (var i = 0; i < samples.Length; i++)
			{
				var t = Time(i);
				low += (Noise(noise) - low) * 0.04f;

				var rumble = 0.5f + 0.5f * Mathf.Sin(Tau * 24f * t);
				var breath = Mathf.Sin(Mathf.PI * t / length);

				samples[i] = (low * 2.5f + Mathf.Sin(Tau * 48f * t) * 0.2f) * rumble * breath;
			}

			return samples;
		}

		// A cat's hiss: bright breathy noise with a sharp start, fading out
		private static float[] Hiss()
		{
			const float length = 0.75f;

			var samples = Buffer(length);
			var noise = new Random(13);
			var low = 0f;

			for (var i = 0; i < samples.Length; i++)
			{
				var t = Time(i);
				var white = Noise(noise);

				// Subtracting a smoothed copy leaves the hissy top end
				low += (white - low) * 0.3f;

				var spit = t < 0.05f ? 1.6f : 1f;
				samples[i] = (white - low) * spit * Decay(t, 0.008f, 0.6f);
			}

			return samples;
		}

		private static float[] Chirp()
		{
			var samples = Buffer(0.32f);
			var phase = 0f;

			for (var i = 0; i < samples.Length; i++)
			{
				var t = Time(i);
				var local = t % 0.14f;
				var active = t < 0.28f && local < 0.09f;
				var frequency = 1700f + Mathf.Sin(local / 0.09f * Mathf.PI) * 500f;

				phase += Tau * frequency / SampleRate;
				samples[i] = active ? Mathf.Sin(phase) * Mathf.Sin(local / 0.09f * Mathf.PI) : 0f;
			}

			return samples;
		}

		private static float[] Wind(float length)
		{
			var samples = Buffer(length);
			var noise = new Random(7);
			var low = 0f;

			for (var i = 0; i < samples.Length; i++)
			{
				var t = Time(i);
				var gust = 0.6f + 0.4f * Mathf.Sin(Tau * t / length);
				low += (Noise(noise) - low) * (0.05f + 0.04f * gust);
				samples[i] = low * gust;
			}

			return samples;
		}

		private static float[] Bubbles(float length)
		{
			var samples = Buffer(length);
			var random = new Random(8);
			var low = 0f;

			for (var i = 0; i < samples.Length; i++)
			{
				low += (Noise(random) - low) * 0.02f;
				samples[i] = low * 0.6f;
			}

			// Little rising blips at random moments
			for (var b = 0; b < 26; b++)
			{
				var start = random.Next(0, samples.Length - SampleRate / 10);
				var pitch = 280f + (float)random.NextDouble() * 420f;
				var phase = 0f;

				for (var i = 0; i < SampleRate / 25; i++)
				{
					var t = i / (float)SampleRate;
					phase += Tau * pitch * (1f + t * 25f) / SampleRate;
					samples[start + i] += Mathf.Sin(phase) * Decay(t, 0.002f, 0.025f) * 0.5f;
				}
			}

			return samples;
		}

		private static float[] Rain(float length)
		{
			var samples = Buffer(length);
			var random = new Random(9);
			var low = 0f;
			var lower = 0f;

			for (var i = 0; i < samples.Length; i++)
			{
				low += (Noise(random) - low) * 0.5f;
				lower += (low - lower) * 0.08f;
				samples[i] = (low - lower) * 0.5f;
			}

			// Individual drops pattering on leaves
			for (var d = 0; d < 260; d++)
			{
				var start = random.Next(0, samples.Length - SampleRate / 40);
				var pitch = 1800f + (float)random.NextDouble() * 2600f;
				var level = 0.15f + (float)random.NextDouble() * 0.35f;

				for (var i = 0; i < SampleRate / 50; i++)
				{
					var t = i / (float)SampleRate;
					samples[start + i] += Mathf.Sin(Tau * pitch * t) * Decay(t, 0.0005f, 0.006f) * level;
				}
			}

			return samples;
		}

		private static float[] Cricket()
		{
			var samples = Buffer(0.6f);

			for (var i = 0; i < samples.Length; i++)
			{
				var t = Time(i);

				// Three quick trills, each a burst of rapid pulses
				var trill = t % 0.2f;
				var active = t < 0.58f && trill < 0.11f;
				var pulse = Mathf.Sin(Tau * 32f * trill) > 0f ? 1f : 0.2f;

				samples[i] = active ? Mathf.Sin(Tau * 4300f * t) * pulse * Mathf.Sin(trill / 0.11f * Mathf.PI) : 0f;
			}

			return samples;
		}

		#endregion

		#region Music

		// A gentle loop: four seventh chords on a soft pad, a walking bass and a plucked pentatonic melody
		private static float[] Music()
		{
			const float tempo = 76f;
			const int barsPerChord = 2;

			var beat = 60f / tempo;
			var bar = beat * 4f;

			// F maj7, E min7, D min7, C maj7 (as frequencies)
			var chords = new[]
			{
				new[] { 174.6f, 220f, 261.6f, 329.6f },
				new[] { 164.8f, 196f, 246.9f, 293.7f },
				new[] { 146.8f, 174.6f, 220f, 261.6f },
				new[] { 130.8f, 164.8f, 196f, 246.9f },
			};

			var pentatonic = new[] { 523.3f, 587.3f, 659.3f, 784f, 880f, 1046.5f };

			var length = bar * barsPerChord * chords.Length * 2;

			var samples = Buffer(length);
			var random = new Random(11);

			var chordLength = bar * barsPerChord;

			for (var i = 0; i < samples.Length; i++)
			{
				var t = Time(i);
				var chordIndex = (int)(t / chordLength) % chords.Length;
				var inChord = t % chordLength;

				// Pad fades in and out across each chord so changes are soft
				var padEnvelope = Mathf.Clamp01(inChord / 0.8f) * Mathf.Clamp01((chordLength - inChord) / 0.8f);

				var pad = 0f;

				foreach (var frequency in chords[chordIndex])
					pad += Mathf.Sin(Tau * frequency * t) * 0.6f + Triangle(frequency * 2f * t) * 0.08f;

				samples[i] += pad * padEnvelope * 0.12f;

				// Bass on beats one and three
				var inHalf = t % (beat * 2f);
				samples[i] += Mathf.Sin(Tau * chords[chordIndex][0] * 0.5f * t) * Decay(inHalf, 0.02f, 0.9f) * 0.35f;
			}

			// Plucked melody in eighth notes, resting now and then
			var eighth = beat / 2f;
			var notes = (int)(length / eighth);
			var previous = 2;

			for (var n = 0; n < notes; n++)
			{
				if (random.NextDouble() < 0.35)
					continue;

				previous = Mathf.Clamp(previous + random.Next(-2, 3), 0, pentatonic.Length - 1);

				var frequency = pentatonic[previous];
				var start = (int)(n * eighth * SampleRate);
				var ring = (int)(eighth * 3f * SampleRate);

				for (var i = 0; i < ring && start + i < samples.Length; i++)
				{
					var t = i / (float)SampleRate;
					samples[start + i] += Bell(frequency, t) * Decay(t, 0.005f, eighth * 1.6f) * 0.22f;
				}
			}

			return samples;
		}

		#endregion

		#region Building blocks

		private static float[] Buffer(float seconds) => new float[Mathf.Max(1, (int)(seconds * SampleRate))];

		private static float Time(int index) => index / (float)SampleRate;

		private static float Noise(Random random) => (float)random.NextDouble() * 2f - 1f;

		private static float Triangle(float cycles) => 1f - 4f * Mathf.Abs(cycles - Mathf.Floor(cycles + 0.5f));

		private static float Bell(float frequency, float t)
			=> Mathf.Sin(Tau * frequency * t) + Mathf.Sin(Tau * frequency * 2.01f * t) * 0.35f * Mathf.Exp(-t * 8f) + Mathf.Sin(Tau * frequency * 3.02f * t) * 0.15f * Mathf.Exp(-t * 14f);

		private static float Decay(float t, float attack, float release)
		{
			if (t < attack)
				return t / attack;

			return Mathf.Exp(-(t - attack) / Math.Max(0.0001f, release) * 2.5f);
		}

		// Crossfades the tail into the head so the clip loops without a click
		private static float[] Loop(float[] samples)
		{
			var fade = Math.Min(samples.Length / 4, SampleRate / 5);
			var looped = new float[samples.Length - fade];

			Array.Copy(samples, looped, looped.Length);

			for (var i = 0; i < fade; i++)
			{
				var blend = i / (float)fade;
				looped[i] = samples[looped.Length + i] * (1f - blend) + samples[i] * blend;
			}

			return looped;
		}

		private static void Normalise(float[] samples, float peak)
		{
			var max = 0f;

			foreach (var sample in samples)
				max = Mathf.Max(max, Mathf.Abs(sample));

			if (max <= 0f)
				return;

			var scale = peak / max;

			for (var i = 0; i < samples.Length; i++)
				samples[i] *= scale;
		}

		#endregion
	}
}
