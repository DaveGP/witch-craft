using Data.ScriptableObjects;
using DependencyInjection;
using Spells.Interfaces;
using UnityEngine;
using Utilities;


namespace Interaction
{
	using Interfaces;

	// A carved standing stone holding one glyph, usually at the end of a platforming climb.
	public class GlyphShrine : MonoBehaviour, IInteractable
	{
		[SerializeField] private GlyphDefinition glyph;

		[SerializeField] private GameObject rune;

		[SerializeField] private Light glow;

		private ISpellBook spellBook;

		public string Prompt => $"Study the {glyph.DisplayName} glyph";

		public bool CanInteract => !spellBook.KnowsGlyph(glyph);

		public Vector3 Position => transform.position;

		private void Awake()
		{
			spellBook = ServiceLocator.Get<ISpellBook>();

			if (spellBook == null)
				Debug.LogError($"{nameof(spellBook)} is null");

			if (glyph == null)
				Debug.LogError($"{name}: {nameof(glyph)} is null");
		}

		private void Start()
		{
			Refresh();
		}

		public void Interact(GameObject interactor)
		{
			if (!spellBook.LearnGlyph(glyph))
				return;

			FxUtility.Burst(rune != null ? rune.transform.position : transform.position + Vector3.up * 2f, glyph.Color, 60, 5f, 0.25f);

			Refresh();
		}

		private void Refresh()
		{
			var claimed = spellBook.KnowsGlyph(glyph);

			if (rune != null)
				rune.SetActive(!claimed);

			if (glow != null)
				glow.intensity = claimed ? 0.3f : 3f;
		}
	}
}
