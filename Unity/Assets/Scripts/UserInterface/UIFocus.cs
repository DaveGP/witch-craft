using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UserInterface
{
	// uGUI selection is also used by mouse clicks. A drawn ring keeps the selected control
	// visible regardless of the active device and independent of its persistent value.
	public sealed class UIFocus : MonoBehaviour, ISelectHandler, IDeselectHandler
	{
		private GameObject ring;

		public static void Attach(Selectable selectable)
		{
			var owner = selectable.gameObject.AddComponent<UIFocus>();
			var border = UIFactory.Image(selectable.transform, UITheme.Gold, "Focus");
			border.sprite = selectable.targetGraphic is Image target ? target.sprite : null;
			border.type = Image.Type.Sliced;
			border.fillCenter = false;
			border.raycastTarget = false;
			UIFactory.Place(border.rectTransform, Vector2.zero, Vector2.one, new Vector2(-4, -4), new Vector2(4, 4));
			var shadow = border.gameObject.AddComponent<Shadow>();
			shadow.effectColor = UITheme.Night;
			shadow.effectDistance = new Vector2(2, -2);
			owner.ring = border.gameObject;
			owner.ring.SetActive(false);
		}

		public void OnSelect(BaseEventData eventData)
		{
			ring.SetActive(true);
		}

		public void OnDeselect(BaseEventData eventData)
		{
			ring.SetActive(false);
		}

		private void OnDisable()
		{
			if (ring != null)
				ring.SetActive(false);
		}
	}
}
