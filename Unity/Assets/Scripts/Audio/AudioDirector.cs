using System.Collections.Generic;
using Character.Interfaces;
using Data.Enums;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using GameState.Interfaces;
using UnityEngine;


namespace Audio
{
	using Interfaces;

	// Owns the game's sound: generates the clips, plays the music loop and ambience, and turns gameplay events
	// (pickups, learning, building, jumping, flying) into sounds so the systems themselves stay quiet.
	public class AudioDirector : MonoBehaviour, IAudioService
	{
		private const string MusicKey = "WitchCraft.Music";

		private const string SoundKey = "WitchCraft.Sound";

		[SerializeField] private float musicVolume = 0.3f;

		[SerializeField] private float soundVolume = 0.8f;

		[SerializeField] private int worldVoices = 8;

		private readonly Dictionary<Sound, AudioClip> clips = new();

		private readonly List<AudioSource> worldSources = new();

		private AudioSource interfaceSource;

		private AudioSource musicSource;

		private AudioSource windSource;

		private IEventBus eventBus;

		private IInputModeService inputModeService;

		private IObserverTarget witch;

		private int nextWorldSource;

		private float stepTimer;

		private float nextChirp;

		private bool musicEnabled;

		private bool soundEnabled;

		public bool MusicEnabled
		{
			get => musicEnabled;
			set
			{
				musicEnabled = value;
				PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
				musicSource.volume = value ? musicVolume : 0f;
			}
		}

		public bool SoundEnabled
		{
			get => soundEnabled;
			set
			{
				soundEnabled = value;
				PlayerPrefs.SetInt(SoundKey, value ? 1 : 0);
			}
		}

		private void Awake()
		{
			eventBus = ServiceLocator.Get<IEventBus>();
			inputModeService = ServiceLocator.Get<IInputModeService>();

			if (eventBus == null)
				Debug.LogError($"{nameof(eventBus)} is null");

			foreach (Sound sound in System.Enum.GetValues(typeof(Sound)))
				clips[sound] = SoundSynth.Create(sound);

			interfaceSource = CreateSource("Interface", false, 0f);
			musicSource = CreateSource("Music", true, 0f);
			windSource = CreateSource("BroomWind", true, 0f);

			for (var i = 0; i < worldVoices; i++)
				worldSources.Add(CreateSource($"World{i}", false, 1f));

			musicEnabled = PlayerPrefs.GetInt(MusicKey, 1) == 1;
			soundEnabled = PlayerPrefs.GetInt(SoundKey, 1) == 1;

			musicSource.clip = clips[Sound.Music];
			musicSource.volume = musicEnabled ? musicVolume : 0f;
			musicSource.Play();

			windSource.clip = clips[Sound.BroomWind];
			windSource.volume = 0f;
			windSource.Play();

			ServiceLocator.Register<IAudioService>(this);

			AddEvents();
		}

		private void OnDestroy()
		{
			ServiceLocator.Unregister<IAudioService>(this);

			RemoveEvents();
		}

		private AudioSource CreateSource(string sourceName, bool loop, float spatial)
		{
			var child = new GameObject(sourceName);
			child.transform.SetParent(transform, false);

			var source = child.AddComponent<AudioSource>();
			source.playOnAwake = false;
			source.loop = loop;
			source.spatialBlend = spatial;
			source.rolloffMode = AudioRolloffMode.Linear;
			source.minDistance = 3f;
			source.maxDistance = 35f;

			return source;
		}

		public AudioClip Clip(Sound sound) => clips[sound];

		public void Play(Sound sound, float volume = 1f, float pitchJitter = 0.05f)
		{
			if (!soundEnabled)
				return;

			interfaceSource.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
			interfaceSource.PlayOneShot(clips[sound], volume * soundVolume);
		}

		public void PlayAt(Sound sound, Vector3 position, float volume = 1f)
		{
			if (!soundEnabled)
				return;

			var source = worldSources[nextWorldSource];
			nextWorldSource = (nextWorldSource + 1) % worldSources.Count;

			source.transform.position = position;
			source.pitch = 1f + Random.Range(-0.05f, 0.05f);
			source.PlayOneShot(clips[sound], volume * soundVolume);
		}

		#region Events

		private void AddEvents()
		{
			eventBus.ActiveTargetChanged += SetWitch;
			eventBus.ItemCollected += Collected;
			eventBus.GlyphLearned += LearnedGlyph;
			eventBus.SpellLearned += LearnedSpell;
			eventBus.RecipeLearned += LearnedRecipe;
			eventBus.CompanionRecruited += Recruited;
			eventBus.HomeLevelChanged += Expanded;
			eventBus.PiecePlaced += Placed;
			eventBus.PieceRemoved += Removed;
		}

		private void RemoveEvents()
		{
			eventBus.ActiveTargetChanged -= SetWitch;
			eventBus.ItemCollected -= Collected;
			eventBus.GlyphLearned -= LearnedGlyph;
			eventBus.SpellLearned -= LearnedSpell;
			eventBus.RecipeLearned -= LearnedRecipe;
			eventBus.CompanionRecruited -= Recruited;
			eventBus.HomeLevelChanged -= Expanded;
			eventBus.PiecePlaced -= Placed;
			eventBus.PieceRemoved -= Removed;

			if (witch != null)
			{
				witch.Movement.Jumped -= Jumped;
				witch.Movement.Landed -= Landed;
			}
		}

		private void SetWitch(IObserverTarget target)
		{
			if (witch != null)
			{
				witch.Movement.Jumped -= Jumped;
				witch.Movement.Landed -= Landed;
			}

			witch = target;

			witch.Movement.Jumped += Jumped;
			witch.Movement.Landed += Landed;
		}

		private void Jumped() => Play(Sound.Jump, 0.45f, 0.08f);

		private void Landed() => Play(Sound.Land, 0.6f, 0.1f);

		private void Collected(ItemDefinition item, int amount) => Play(Sound.Pickup, 0.5f, 0.03f);

		private void LearnedGlyph(GlyphDefinition glyph) => Play(Sound.Learn, 0.7f, 0f);

		private void LearnedSpell(SpellDefinition spell) => Play(Sound.Learn, 0.8f, 0f);

		private void LearnedRecipe(RecipeDefinition recipe) => Play(Sound.Learn, 0.6f, 0f);

		private void Recruited(CompanionDefinition companion) => Play(Sound.Learn, 0.8f, 0f);

		private void Expanded(int level) => Play(Sound.Learn, 1f, 0f);

		private void Placed(Vector3 position) => PlayAt(Sound.Build, position, 0.7f);

		private void Removed(Vector3 position) => PlayAt(Sound.Unbuild, position, 0.7f);

		#endregion

		private void Update()
		{
			if (witch == null)
				return;

			UpdateFootsteps();
			UpdateWind();
			UpdateAmbience();
		}

		private void UpdateFootsteps()
		{
			var movement = witch.Movement;

			var velocity = movement.Velocity;
			var speed = new Vector2(velocity.x, velocity.z).magnitude;

			if (!movement.IsGrounded || movement.Mode == MovementMode.Broom || speed < 0.8f)
			{
				stepTimer = 0.05f;
				return;
			}

			// Steps come quicker the faster she runs
			stepTimer -= Time.deltaTime * Mathf.Lerp(2.4f, 4.4f, speed / 8.5f);

			if (stepTimer > 0f)
				return;

			stepTimer = 1f;

			Play(Sound.Step, 0.35f, 0.12f);
		}

		private void UpdateWind()
		{
			var flying = witch.Broom.IsMounted;

			var speed = witch.Movement.Velocity.magnitude;

			var target = flying && soundEnabled ? Mathf.Clamp01(0.15f + speed / 18f) * 0.45f : 0f;

			windSource.volume = Mathf.MoveTowards(windSource.volume, target, Time.deltaTime * 1.5f);
			windSource.pitch = Mathf.Lerp(0.8f, 1.3f, speed / 18f);
		}

		private void UpdateAmbience()
		{
			if (Time.time < nextChirp)
				return;

			nextChirp = Time.time + Random.Range(5f, 13f);

			if (inputModeService.Mode != InputMode.Gameplay)
				return;

			// A bird somewhere nearby, off to one side
			var offset = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(8f, 20f);

			PlayAt(Sound.Chirp, witch.Transform.position + offset + Vector3.up * 4f, Random.Range(0.25f, 0.45f));
		}
	}
}
