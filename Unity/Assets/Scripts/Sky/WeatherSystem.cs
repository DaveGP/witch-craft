using Audio;
using Audio.Interfaces;
using Data.Enums;
using Data.ScriptableObjects;
using DependencyInjection;
using UnityEngine;


namespace Sky
{
	using Interfaces;

	// Rolls the weather every few in-game hours: mostly clear, sometimes cloudy, now and then a shower.
	// Rain falls around the camera, greys the sky through the day cycle, and makes herbs regrow faster.
	public class WeatherSystem : MonoBehaviour, IWeather
	{
		private const float HoursBetweenRolls = 4f;

		public WeatherKind Current { get; private set; } = WeatherKind.Clear;

		public bool IsRaining => Current == WeatherKind.Rain && Overcast > 0.5f;

		public float Overcast { get; private set; }

		[SerializeField, Range(0f, 1f)] private float cloudyChance = 0.25f;

		[SerializeField, Range(0f, 1f)] private float rainChance = 0.2f;

		[SerializeField] private float rainRate = 900f;

		private IDayCycle dayCycle;

		private IAudioService audioService;

		private ParticleSystem rain;

		private AudioSource rainSound;

		private float nextRollHour;

		private int nextRollDay;

		private void Awake()
		{
			ServiceLocator.Register<IWeather>(this);
		}

		private void Start()
		{
			dayCycle = ServiceLocator.Get<IDayCycle>();
			audioService = ServiceLocator.Get<IAudioService>();

			if (dayCycle == null)
				Debug.LogError($"{nameof(dayCycle)} is null");

			ScheduleNextRoll();

			rain = CreateRain();

			if (audioService != null)
			{
				rainSound = gameObject.AddComponent<AudioSource>();
				rainSound.clip = audioService.Clip(Sound.Rain);
				rainSound.loop = true;
				rainSound.volume = 0f;
				rainSound.Play();
			}
		}

		private void OnDestroy()
		{
			ServiceLocator.Unregister<IWeather>(this);
		}

		public void SetWeather(WeatherKind weather)
		{
			Current = weather;

			ScheduleNextRoll();
		}

		private void Update()
		{
			if (dayCycle != null && (dayCycle.Day > nextRollDay || dayCycle.Day == nextRollDay && dayCycle.Hour >= nextRollHour))
				Roll();

			var targetOvercast = Current switch
			{
				WeatherKind.Rain => 1f,
				WeatherKind.Cloudy => 0.5f,
				_ => 0f
			};

			// Weather drifts in over a few seconds rather than snapping
			Overcast = Mathf.MoveTowards(Overcast, targetOvercast, Time.deltaTime * 0.12f);

			UpdateRain();
		}

		private void Roll()
		{
			var roll = Random.value;

			Current = roll < rainChance ? WeatherKind.Rain
				: roll < rainChance + cloudyChance ? WeatherKind.Cloudy
				: WeatherKind.Clear;

			ScheduleNextRoll();
		}

		private void ScheduleNextRoll()
		{
			if (dayCycle == null)
				return;

			nextRollHour = dayCycle.Hour + HoursBetweenRolls;
			nextRollDay = dayCycle.Day;

			if (nextRollHour < 24f)
				return;

			nextRollHour -= 24f;
			nextRollDay++;
		}

		private void UpdateRain()
		{
			var amount = Current == WeatherKind.Rain ? Mathf.InverseLerp(0.5f, 1f, Overcast) : 0f;

			if (rain != null)
			{
				var camera = UnityEngine.Camera.main;

				if (camera != null)
					rain.transform.position = camera.transform.position + Vector3.up * 12f + camera.transform.forward * 6f;

				var emission = rain.emission;
				emission.rateOverTime = rainRate * amount;
			}

			if (rainSound != null)
				rainSound.volume = audioService.SoundEnabled ? amount * 0.4f : 0f;
		}

		private ParticleSystem CreateRain()
		{
			var rainObject = new GameObject("Rain");
			rainObject.transform.SetParent(transform, false);

			var system = rainObject.AddComponent<ParticleSystem>();
			system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

			var main = system.main;
			main.loop = true;
			main.startLifetime = 1.2f;
			main.startSpeed = 18f;
			main.startSize = 0.05f;
			main.startColor = new Color(0.75f, 0.85f, 1f, 0.45f);
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.maxParticles = 3000;

			var emission = system.emission;
			emission.rateOverTime = 0f;

			var shape = system.shape;
			shape.shapeType = ParticleSystemShapeType.Box;
			shape.scale = new Vector3(40f, 40f, 1f);
			shape.rotation = new Vector3(90f, 0f, 0f);

			var renderer = system.GetComponent<ParticleSystemRenderer>();
			renderer.renderMode = ParticleSystemRenderMode.Stretch;
			renderer.lengthScale = 6f;
			renderer.velocityScale = 0.05f;
			renderer.material = ServiceLocator.Get<GameDatabase>()?.ParticleMaterial;

			system.Play();

			return system;
		}
	}
}
