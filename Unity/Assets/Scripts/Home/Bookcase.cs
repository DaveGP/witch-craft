using System.Collections;
using System.Linq;
using Books.Interfaces;
using DependencyInjection;
using Events.Interfaces;
using Interaction.Interfaces;
using UnityEngine;


namespace Home
{
	// Every bookshelf in the home doubles as the witch's library of found storybooks. Each book found stands on top
	// of it in its cover colour, in the order the books are listed, so the shelf fills up as she explores.
	public class Bookcase : MonoBehaviour, IInteractable
	{
		[SerializeField] private Transform storybooks;

		private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

		private static readonly int Color = Shader.PropertyToID("_Color");

		private IBookCollection books;

		private int shown = -1;

		public string Prompt => "Browse your storybooks";

		public bool CanInteract => true;

		public Vector3 Position => transform.position;

		/// <summary>
		/// How many storybooks stand on this shelf, for the playtest.
		/// </summary>
		public int ShownCount => storybooks != null ? storybooks.Cast<Transform>().Count(spine => spine.gameObject.activeSelf) : 0;

		/// <summary>
		/// Whether this book's spine stands on the shelf.
		/// </summary>
		public bool Shows(int index) => storybooks != null && index < storybooks.childCount && storybooks.GetChild(index).gameObject.activeSelf;

		private void Awake()
		{
			books = ServiceLocator.Get<IBookCollection>();
		}

		private IEnumerator Start()
		{
			Refresh();

			// Art from the Art Set arrives with the piece; wait a frame so the books stand on its top, not the placeholder's
			yield return null;
			Settle();
		}

		private void Update()
		{
			if (books != null && books.Count != shown)
				Refresh();
		}

		public void Interact(GameObject interactor)
		{
			ServiceLocator.Get<IEventBusSender>()?.OpenBookcase();
		}

		private void Refresh()
		{
			if (books == null || storybooks == null)
				return;

			shown = books.Count;

			var block = new MaterialPropertyBlock();

			for (var i = 0; i < storybooks.childCount; i++)
			{
				var spine = storybooks.GetChild(i);
				var book = i < books.AllBooks.Count ? books.AllBooks[i] : null;
				var found = book != null && books.Has(book);

				spine.gameObject.SetActive(found);

				if (!found)
					continue;

				var renderer = spine.GetComponent<Renderer>();
				renderer.GetPropertyBlock(block);
				block.SetColor(BaseColor, book.Color);
				block.SetColor(Color, book.Color);
				renderer.SetPropertyBlock(block);
			}
		}

		// Stand the books on whatever is showing: the placeholder shelf or the art that replaced it
		private void Settle()
		{
			if (storybooks == null)
				return;

			var renderers = GetComponentsInChildren<Renderer>()
				.Where(renderer => renderer.enabled && !renderer.transform.IsChildOf(storybooks))
				.ToArray();

			if (renderers.Length == 0)
				return;

			var bounds = renderers[0].bounds;

			foreach (var renderer in renderers.Skip(1))
				bounds.Encapsulate(renderer.bounds);

			storybooks.position = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
		}
	}
}
