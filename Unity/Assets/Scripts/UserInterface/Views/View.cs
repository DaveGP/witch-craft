using System.Collections;
using Character.Input.Interfaces;
using Data.Enums;
using DependencyInjection;
using GameState.Interfaces;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UserInterface.Views
{
	// A full screen menu. Opening one takes input focus from the witch until it closes.
	public abstract class View : MonoBehaviour
	{
		public bool IsOpen { get; private set; }

		protected RectTransform Root { get; private set; }

		/// <summary>
		/// Stations push their own input mode, so their views don't.
		/// </summary>
		protected virtual bool TakesFocus => true;

		private IInputModeService inputModeService;
		private CanvasGroup visuals;
		private Coroutine transition;
		private Selectable[] outgoingControls;


		public void Build(RectTransform root)
		{
			Root = root;

			inputModeService = ServiceLocator.Get<IInputModeService>();

			OnBuild();
			visuals = Root.gameObject.AddComponent<CanvasGroup>();

			Root.gameObject.SetActive(false);
		}

		public void Open()
		{
			if (IsOpen)
			{
				OnOpen();
				return;
			}

			foreach (var view in GetComponents<View>())
				if (!view.IsOpen)
					view.FinishClose();

			IsOpen = true;

			Root.gameObject.SetActive(true);

			if (TakesFocus)
				inputModeService.Push(InputMode.Menu);

			OnOpen();

			visuals.interactable = true;
			visuals.blocksRaycasts = true;
			transition = StartCoroutine(Fade(true));
			StartCoroutine(FocusFirst());
		}

		/// <summary>
		/// Gamepads need something selected to navigate from. Waits a frame for rebuilt content to settle.
		/// </summary>
		protected IEnumerator FocusFirst()
		{
			yield return null;

			var controlScheme = ServiceLocator.Get<IControlScheme>();

			if (controlScheme == null || !controlScheme.UsingGamepad || EventSystem.current == null || !IsOpen)
				yield break;

			Selectable first = null;
			foreach (var control in Root.GetComponentsInChildren<Selectable>())
			{
				if (!control.isActiveAndEnabled || !control.IsInteractable())
					continue;

				first = control;
				break;
			}

			if (first != null)
				EventSystem.current.SetSelectedGameObject(first.gameObject);
		}

		public void Close()
		{
			if (!IsOpen)
				return;

			IsOpen = false;
			visuals.interactable = false;
			visuals.blocksRaycasts = false;

			if (transition != null)
				StopCoroutine(transition);

			var controls = new System.Collections.Generic.List<Selectable>();
			foreach (var control in Root.GetComponentsInChildren<Selectable>())
			{
				if (!control.isActiveAndEnabled)
					continue;

				control.enabled = false;
				controls.Add(control);
			}

			outgoingControls = controls.ToArray();

			transition = StartCoroutine(Fade(false));

			if (TakesFocus)
				inputModeService.Pop();

			OnClose();
		}

		private IEnumerator Fade(bool opening)
		{
			var start = Time.unscaledTime;
			var duration = opening ? 0.15f : 0.10f;
			var initial = opening ? 0.65f : visuals.alpha;

			while (Time.unscaledTime - start < duration)
			{
				visuals.alpha = Mathf.Lerp(initial, opening ? 1f : 0f, (Time.unscaledTime - start) / duration);
				yield return null;
			}

			transition = null;
			if (opening)
				visuals.alpha = 1f;
			else
				FinishClose();
		}

		private void FinishClose()
		{
			if (outgoingControls == null)
				return;

			if (transition != null)
			{
				StopCoroutine(transition);
				transition = null;
			}

			Root.gameObject.SetActive(false);
			foreach (var control in outgoingControls)
				if (control != null)
					control.enabled = true;

			outgoingControls = null;
		}

		/// <summary>
		/// Escape was pressed while this view is open.
		/// </summary>
		public virtual void Cancel() => Close();

		protected abstract void OnBuild();

		protected virtual void OnOpen() { }

		protected virtual void OnClose() { }
	}
}
