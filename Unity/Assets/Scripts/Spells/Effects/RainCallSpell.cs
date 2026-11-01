using Data.Enums;
using DependencyInjection;
using Sky.Interfaces;
using UnityEngine;
using Utilities;

namespace Spells.Effects
{
	// Aqua + Vita. Calls up a gentle shower, or clears one away. Rain makes herbs regrow twice as fast.
	public class RainCallSpell : SpellEffect
	{
		public override SpellEffectType Type => SpellEffectType.RainCall;

		public override bool IsAiming => false;

		public override string Status
		{
			get
			{
				var raining = ServiceLocator.Get<IWeather>()?.Current == WeatherKind.Rain;

				var wait = Mathf.CeilToInt(readyTime - Time.time);

				if (wait > 0)
					return $"Rain Call: the sky needs {wait}s to rest";

				return raining ? "Rain Call: click to send the clouds away" : "Rain Call: click to call a gentle shower (herbs regrow faster)";
			}
		}

		[SerializeField] private float cooldown = 20f;

		private float readyTime;

		public override void Cast()
		{
			var weather = ServiceLocator.Get<IWeather>();

			if (weather == null || Time.time < readyTime)
				return;

			readyTime = Time.time + cooldown;

			var raining = weather.Current == WeatherKind.Rain;

			weather.SetWeather(raining ? WeatherKind.Clear : WeatherKind.Rain);

			EventBus.Notify(raining ? "The clouds drift apart and the sun peeks through." : "Clouds gather overhead and a soft rain begins.");

			FxUtility.Burst(transform.position + Vector3.up * 2.5f, SpellColour, 40, 2f, 0.2f);
		}
	}
}
