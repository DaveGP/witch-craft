using Camera.Interfaces;
using Data.Enums;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using Spells.Interfaces;
using UnityEngine;
using Utilities;

namespace Spells.Effects
{
	// Shared plumbing for spell effects living on the player: services, readied state and aiming.
	public abstract class SpellEffect : MonoBehaviour, ISpellEffect
	{
		public abstract SpellEffectType Type { get; }

		public abstract string Status { get; }

		public virtual bool IsAiming => true;

		public virtual bool CapturesScroll => false;

		protected bool IsReadied { get; private set; }

		protected SpellDefinition Spell { get; private set; }

		protected Color SpellColour => Spell != null ? Spell.Color : Color.white;

		protected IEventBusSender EventBus { get; private set; }

		protected GameDatabase Database { get; private set; }

		protected ICameraController CameraController => cameraController ??= ServiceLocator.Get<ICameraController>();

		[SerializeField] protected float range = 22f;

		private ICameraController cameraController;

		protected virtual void Awake()
		{
			EventBus = ServiceLocator.Get<IEventBusSender>();
			Database = ServiceLocator.Get<GameDatabase>();

			if (EventBus == null)
				Debug.LogError($"{nameof(EventBus)} is null");

			if (Database == null)
				Debug.LogError($"{nameof(Database)} is null");
		}

		public void Begin(SpellDefinition spell)
		{
			Spell = spell;
			IsReadied = true;

			OnBegin();
		}

		public void End()
		{
			IsReadied = false;

			OnEnd();
		}

		protected virtual void OnBegin() { }

		protected virtual void OnEnd() { }

		public abstract void Cast();

		public virtual void AltCast() { }

		public virtual void Adjust(float rotate, float scroll) { }

		/// <summary>
		/// Casts the camera's aim ray, limited to the spell's range from the witch.
		/// </summary>
		protected bool Aim(out RaycastHit hit, LayerMask mask)
		{
			hit = default;

			if (CameraController == null)
				return false;

			var ray = CameraController.AimRay;

			// The camera sits behind the witch; measure range from her, not from it
			var cameraToWitch = Vector3.Dot(transform.position - ray.origin, ray.direction);

			var maxDistance = Mathf.Max(0f, cameraToWitch) + range;

			return Physics.Raycast(ray, out hit, maxDistance, mask, QueryTriggerInteraction.Ignore);
		}

		protected bool Aim(out RaycastHit hit) => Aim(out hit, LayerUtility.AimMask);
	}
}
