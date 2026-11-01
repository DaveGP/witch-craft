using Data.Enums;
using DependencyInjection;
using Potions.Interfaces;
using UnityEngine;
using World;

namespace Potions
{
	// Visible signs of what the witch has drunk. Glimmer makes her glow and reveals hidden paths.
	public class PotionAura : MonoBehaviour
	{
		[SerializeField] private Light glimmerLight;

		private IPotionEffects potionEffects;

		private bool glimmering;

		private void Awake()
		{
			potionEffects = ServiceLocator.Get<IPotionEffects>();

			if (potionEffects == null)
				Debug.LogError($"{nameof(potionEffects)} is null");

			if (glimmerLight != null)
				glimmerLight.enabled = false;
		}

		private void Update()
		{
			var active = potionEffects.IsActive(PotionEffect.Glimmer);

			if (active == glimmering)
				return;

			glimmering = active;

			if (glimmerLight != null)
				glimmerLight.enabled = active;

			if (active)
				HiddenPath.AddRevealer(transform);
			else
				HiddenPath.RemoveRevealer(transform);
		}

		private void OnDestroy()
		{
			HiddenPath.RemoveRevealer(transform);
		}
	}
}
