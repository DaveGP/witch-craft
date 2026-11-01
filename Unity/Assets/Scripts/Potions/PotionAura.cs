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

		private bool tidewalking;

		private int playerLayer;

		private int waterLayer;

		private void Awake()
		{
			potionEffects = ServiceLocator.Get<IPotionEffects>();

			if (potionEffects == null)
				Debug.LogError($"{nameof(potionEffects)} is null");

			if (glimmerLight != null)
				glimmerLight.enabled = false;

			playerLayer = LayerMask.NameToLayer("Player");
			waterLayer = LayerMask.NameToLayer("Water");

			Physics.IgnoreLayerCollision(playerLayer, waterLayer, true);
		}

		private void Update()
		{
			UpdateTidewalk();

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

		private void UpdateTidewalk()
		{
			var active = potionEffects.IsActive(PotionEffect.Tidewalk);

			if (active == tidewalking)
				return;

			tidewalking = active;

			Physics.IgnoreLayerCollision(playerLayer, waterLayer, !active);
		}

		private void OnDestroy()
		{
			Physics.IgnoreLayerCollision(playerLayer, waterLayer, true);

			HiddenPath.RemoveRevealer(transform);
		}
	}
}
