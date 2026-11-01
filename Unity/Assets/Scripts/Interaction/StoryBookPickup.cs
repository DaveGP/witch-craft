using Audio;
using Audio.Interfaces;
using Books.Interfaces;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using UnityEngine;
using Utilities;


namespace Interaction
{
	using Interfaces;

	// A storybook glowing on a lectern. Picking it up opens it to read and sends it home to the bookcase.
	public class StoryBookPickup : MonoBehaviour, IInteractable
	{
		[SerializeField] private StoryBookDefinition book;

		[SerializeField] private GameObject visual;

		private IBookCollection books;

		private IEventBusSender eventBus;

		public string Prompt => $"Read \"{book.Title}\"";

		public bool CanInteract => !books.Has(book);

		public Vector3 Position => transform.position;

		private void Awake()
		{
			books = ServiceLocator.Get<IBookCollection>();
			eventBus = ServiceLocator.Get<IEventBusSender>();

			if (books == null)
				Debug.LogError($"{nameof(books)} is null");

			if (book == null)
				Debug.LogError($"{name}: {nameof(book)} is null");
		}

		private void Start()
		{
			Refresh();
		}

		public void Interact(GameObject interactor)
		{
			if (!books.Collect(book))
				return;

			FxUtility.Burst(visual != null ? visual.transform.position : transform.position + Vector3.up, book.Color, 40, 2.5f, 0.2f);

			ServiceLocator.Get<IAudioService>()?.Play(Sound.Learn, 0.7f, 0f);

			Refresh();

			eventBus.OpenBook(book);
		}

		// The lectern stays as a landmark; only the book itself goes home
		private void Refresh()
		{
			if (visual != null)
				visual.SetActive(!books.Has(book));
		}
	}
}
