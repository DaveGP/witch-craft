using Data.Enums;
using DependencyInjection;
using GameState.Interfaces;
using UnityEngine;

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

		public void Build(RectTransform root)
		{
			Root = root;

			inputModeService = ServiceLocator.Get<IInputModeService>();

			OnBuild();

			Root.gameObject.SetActive(false);
		}

		public void Open()
		{
			if (IsOpen)
			{
				OnOpen();
				return;
			}

			IsOpen = true;

			Root.gameObject.SetActive(true);

			if (TakesFocus)
				inputModeService.Push(InputMode.Menu);

			OnOpen();
		}

		public void Close()
		{
			if (!IsOpen)
				return;

			IsOpen = false;

			Root.gameObject.SetActive(false);

			if (TakesFocus)
				inputModeService.Pop();

			OnClose();
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
