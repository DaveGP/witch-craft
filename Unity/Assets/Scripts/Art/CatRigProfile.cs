using System;
using System.Collections.Generic;
using UnityEngine;

namespace Art
{
	// Describes a rigged cat for CatCompanion: which Animator states do what, on which side, how long each lasts
	// and how fast the walk travels. Written by the art setup tools; the cat crossfades to states by name.
	public class CatRigProfile : MonoBehaviour
	{
		public enum Role
		{
			Idle,
			Walk,
			Jump,
			Sit,
			Groom,
			Scratch,
			Loaf,
			LieDown,
			Sleep,
			SleepBlink,
			ThreatShort,
			ThreatLong,
			RollOver,
		}

		public enum Side
		{
			Centre,
			Left,
			Right,
		}

		[Serializable]
		public class State
		{
			public string Name;

			public Role Role;

			public Side Side;

			[Tooltip("Seconds for one loop at speed 1")]
			public float Length;
		}

		public List<State> States = new();

		[Tooltip("Metres a second the walk covers at playback speed 1 and scale 1")]
		public float WalkSpeed = 0.5f;

		[Tooltip("Float parameter multiplying the walk's playback speed")]
		public string WalkSpeedParameter = "WalkSpeed";

		[Tooltip("Float parameter multiplying the jump's playback speed")]
		public string JumpSpeedParameter = "JumpSpeed";

		[Tooltip("Normalised time in the jump clip when the paws leave the ground")]
		public float JumpTakeoff = 0.3f;

		[Tooltip("Normalised time in the jump clip when the paws land")]
		public float JumpLanding = 0.6f;

		[Tooltip("How high the clip itself lifts the paws at scale 1, so the hop arc doesn't add it twice")]
		public float JumpLift;

		[Tooltip("How far the leap carries forward between takeoff and landing at scale 1, for bounding")]
		public float JumpDistance = 0.5f;

		[Tooltip("The bone that carries the whole skeleton, pinned in place so clips that travel stay on the cat")]
		public string RootBone = "Root";

		[Tooltip("Height of the head bone at scale 1 in the idle pose")]
		public float HeadHeight = 0.3f;

		public bool Has(Role role) => States.Exists(state => state.Role == role);

		/// <summary>
		/// A random state for the role, on the given side when there is one, else centre, else any side.
		/// </summary>
		public State Pick(Role role, Side side)
		{
			var matches = States.FindAll(state => state.Role == role && state.Side == side);

			if (matches.Count == 0)
				matches = States.FindAll(state => state.Role == role && state.Side == Side.Centre);

			if (matches.Count == 0)
				matches = States.FindAll(state => state.Role == role);

			return matches.Count == 0 ? null : matches[UnityEngine.Random.Range(0, matches.Count)];
		}
	}
}
