using DependencyInjection;
using Events.Interfaces;
using Interaction.Interfaces;
using Spells.Interfaces;
using UnityEngine;


namespace Character.Actions
{
	using Interfaces;


	public class ActionController : MonoBehaviour, IActionController
	{
		[SerializeField]
		private float focusDistance = 1.1f;

		[SerializeField]
		private float focusRadius = 1.4f;

		[SerializeField]
		private float focusHeight = 0.9f;

		private readonly Collider[] overlaps = new Collider[16];

		private ISpellCaster spellCaster;

		private IEventBusSender eventBus;

		private IInteractable focus;

		private LayerMask interactableMask;


		private void Awake()
		{
			spellCaster = GetComponent<ISpellCaster>();

			eventBus = ServiceLocator.Get<IEventBusSender>();

			if (spellCaster == null)
				Debug.LogError($"{nameof(spellCaster)} is null");

			if (eventBus == null)
				Debug.LogError($"{nameof(eventBus)} is null");

			interactableMask = LayerMask.GetMask("Interactable");
		}

		private void FixedUpdate()
		{
			UpdateFocus();
		}

		private void UpdateFocus()
		{
			var centre = transform.position + Vector3.up * focusHeight + transform.forward * focusDistance;

			var count = Physics.OverlapSphereNonAlloc(centre, focusRadius, overlaps, interactableMask, QueryTriggerInteraction.Collide);

			IInteractable nearest = null;

			var nearestDistance = float.MaxValue;

			for (var i = 0; i < count; i++)
			{
				var interactable = overlaps[i].GetComponentInParent<IInteractable>();

				if (interactable == null || !interactable.CanInteract)
					continue;

				var distance = (interactable.Position - centre).sqrMagnitude;

				if (distance >= nearestDistance)
					continue;

				nearest = interactable;
				nearestDistance = distance;
			}

			if (nearest == focus)
				return;

			focus = nearest;

			eventBus.SetInteractionFocus(focus);
		}

		public void OnInteract()
		{
			if (focus == null || !focus.CanInteract)
				return;

			focus.Interact(gameObject);

			// Whatever we used may have changed (gathered, claimed), so re-announce it
			focus = null;
			UpdateFocus();
		}

		public void OnCast() => spellCaster.Cast();

		public void OnAltCast() => spellCaster.AltCast();

		public void OnUndo() => spellCaster.Undo();

		public void OnPick() => spellCaster.Pick();

		public void OnSelectSpell(int slot) => spellCaster.SelectSlot(slot);

		public void OnSpellAdjust(float rotate, float scroll) => spellCaster.Adjust(rotate, scroll);
	}
}
