using Data.Save;
using DependencyInjection;
using Persistence.Interfaces;
using UnityEngine;
using Utilities;


namespace Sky
{
	using Interfaces;

	// Moves the sun and moon through a day, and shifts the sky, ambient light and fog with them. A full day lasts
	// about fourteen minutes: dawn at six, dusk at eight in the evening. Nights are moonlit and blue rather than
	// black, with fireflies near the witch, so the island stays playable after dark.
	public class DayCycle : MonoBehaviour, IDayCycle, ISaveParticipant
	{
		public const float DawnHour = 6f;

		public const float DuskHour = 20f;

		public float Hour { get; private set; } = 8f;

		public int Day { get; private set; } = 1;

		public bool IsNight => Hour < DawnHour || Hour >= DuskHour;

		public float Darkness { get; private set; }

		public string PeriodName => Hour switch
		{
			< DawnHour => "Night",
			< 11f => "Morning",
			< 17f => "Afternoon",
			< DuskHour => "Evening",
			_ => "Night"
		};

		[SerializeField] private Light sun;

		[SerializeField] private Light moon;

		[SerializeField] private float secondsPerHour = 35f;

		[SerializeField] private float sunIntensity = 1.4f;

		[SerializeField] private float moonIntensity = 0.45f;

		private static readonly Color NoonSun = new(1f, 0.93f, 0.82f);
		private static readonly Color LowSun = new(1f, 0.55f, 0.35f);

		private static readonly Color DaySky = new(0.66f, 0.66f, 0.85f);
		private static readonly Color DayEquator = new(0.62f, 0.57f, 0.62f);
		private static readonly Color DayGround = new(0.38f, 0.34f, 0.32f);
		private static readonly Color DayFog = new(0.78f, 0.75f, 0.9f);

		private static readonly Color DuskSky = new(0.55f, 0.42f, 0.62f);
		private static readonly Color DuskFog = new(0.88f, 0.6f, 0.55f);

		private static readonly Color NightSky = new(0.2f, 0.24f, 0.42f);
		private static readonly Color NightEquator = new(0.16f, 0.18f, 0.32f);
		private static readonly Color NightGround = new(0.08f, 0.08f, 0.14f);
		private static readonly Color NightFog = new(0.1f, 0.12f, 0.24f);

		private static readonly Color StormFog = new(0.55f, 0.58f, 0.62f);

		private ISaveService saveService;

		private IWeather weather;

		private Material sky;

		private float skyExposure;

		private ParticleSystem fireflies;

		private Transform fireflyAnchor;

		private void Awake()
		{
			saveService = ServiceLocator.Get<ISaveService>();

			ServiceLocator.Register<IDayCycle>(this);

			if (sun == null)
				Debug.LogError($"{nameof(sun)} is null");

			if (moon == null)
				Debug.LogError($"{nameof(moon)} is null");

			// Work on a copy so the editor's skybox asset isn't changed while playing
			if (RenderSettings.skybox != null)
			{
				sky = new Material(RenderSettings.skybox);
				RenderSettings.skybox = sky;
				skyExposure = sky.HasProperty("_Exposure") ? sky.GetFloat("_Exposure") : 1f;
			}
		}

		private void Start()
		{
			weather = ServiceLocator.Get<IWeather>();

			saveService.Register(this);

			fireflyAnchor = new GameObject("Fireflies").transform;
			fireflies = FxUtility.Loop(fireflyAnchor, Vector3.zero, new Color(0.85f, 1f, 0.45f), 0f, 0.25f, 14f, 0.09f);

			Apply();
		}

		private void OnDestroy()
		{
			saveService?.Unregister(this);

			ServiceLocator.Unregister<IDayCycle>(this);
		}

		private void Update()
		{
			Hour += Time.deltaTime / secondsPerHour;

			if (Hour >= 24f)
			{
				Hour -= 24f;
				Day++;
			}

			Apply();
		}

		public void SkipTo(float hour)
		{
			if (hour <= Hour)
				Day++;

			Hour = Mathf.Repeat(hour, 24f);

			Apply();
		}

		private void Apply()
		{
			var overcast = weather?.Overcast ?? 0f;

			// The sun rises at dawn, peaks at one in the afternoon and sets at dusk
			var dayProgress = Mathf.InverseLerp(DawnHour, DuskHour, Hour);
			var sunElevation = Mathf.Sin(dayProgress * Mathf.PI) * 62f;
			var sunUp = !IsNight;

			sun.transform.rotation = Quaternion.Euler(sunUp ? Mathf.Max(sunElevation, 2f) : -20f, Mathf.Lerp(-110f, 70f, dayProgress), 0f);

			var daylight = sunUp ? Mathf.SmoothStep(0f, 1f, Mathf.Min(Hour - DawnHour, DuskHour - Hour) / 1.5f) : 0f;
			Darkness = 1f - daylight;

			var lowSun = Mathf.Clamp01(1f - sunElevation / 25f);

			sun.intensity = sunIntensity * daylight * Mathf.Lerp(1f, 0.55f, overcast);
			sun.color = Color.Lerp(NoonSun, LowSun, lowSun * daylight);
			sun.enabled = sun.intensity > 0.01f;
			sun.shadowStrength = Mathf.Lerp(0.75f, 0.3f, overcast);

			// The moon crosses the night sky opposite the sun
			var nightProgress = Mathf.Repeat(Hour - DuskHour, 24f) / (24f - (DuskHour - DawnHour));
			moon.transform.rotation = Quaternion.Euler(Mathf.Max(8f, Mathf.Sin(Mathf.Clamp01(nightProgress) * Mathf.PI) * 55f), Mathf.Lerp(80f, 250f, nightProgress), 0f);
			moon.intensity = moonIntensity * Darkness * Mathf.Lerp(1f, 0.6f, overcast);
			moon.enabled = moon.intensity > 0.01f;

			RenderSettings.sun = sun.enabled ? sun : moon;

			// Dusk and dawn get a warm wash on the way between day and night
			var twilight = Mathf.Clamp01(1f - Mathf.Abs(Darkness - 0.5f) * 2f);

			var skyColour = Color.Lerp(Color.Lerp(DaySky, NightSky, Darkness), DuskSky, twilight * 0.6f);
			var fogColour = Color.Lerp(Color.Lerp(DayFog, NightFog, Darkness), DuskFog, twilight * 0.5f);

			fogColour = Color.Lerp(fogColour, StormFog * Mathf.Lerp(1f, 0.3f, Darkness), overcast * 0.6f);

			RenderSettings.ambientSkyColor = Color.Lerp(skyColour, skyColour * 0.75f, overcast);
			RenderSettings.ambientEquatorColor = Color.Lerp(DayEquator, NightEquator, Darkness);
			RenderSettings.ambientGroundColor = Color.Lerp(DayGround, NightGround, Darkness);
			RenderSettings.fogColor = fogColour;
			RenderSettings.fogStartDistance = Mathf.Lerp(70f, 25f, overcast);
			RenderSettings.fogEndDistance = Mathf.Lerp(260f, 150f, overcast);

			// Reflections come from the daytime sky, so fade them out or water glows white at night
			RenderSettings.reflectionIntensity = Mathf.Lerp(1f, 0.12f, Darkness) * Mathf.Lerp(1f, 0.7f, overcast);

			if (sky != null)
			{
				if (sky.HasProperty("_Exposure"))
					sky.SetFloat("_Exposure", Mathf.Lerp(skyExposure, 0.18f, Darkness) * Mathf.Lerp(1f, 0.6f, overcast));

				if (sky.HasProperty("_SkyTint"))
					sky.SetColor("_SkyTint", Color.Lerp(new Color(0.62f, 0.6f, 0.9f), new Color(0.6f, 0.62f, 0.66f), overcast));
			}

			UpdateFireflies();
		}

		private void UpdateFireflies()
		{
			if (fireflies == null)
				return;

			var camera = UnityEngine.Camera.main;

			if (camera != null)
				fireflyAnchor.position = camera.transform.position + camera.transform.forward * 8f + Vector3.down * 3f;

			var emission = fireflies.emission;
			emission.rateOverTime = Darkness > 0.6f && !(weather?.IsRaining ?? false) ? 14f * Darkness : 0f;
		}

		public void Read(SaveData data)
		{
			Hour = Mathf.Repeat(data.Hour, 24f);
			Day = Mathf.Max(1, data.Day);
		}

		public void Write(SaveData data)
		{
			data.Hour = Hour;
			data.Day = Day;
		}
	}
}
