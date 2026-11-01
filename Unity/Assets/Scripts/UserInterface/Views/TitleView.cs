using System;
using System.Collections.Generic;
using DependencyInjection;
using Events.Interfaces;
using GameState;
using Identity.Interfaces;
using Persistence.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace UserInterface.Views
{
	// The front door: three save slots over a slow view of the island. Picking the slot that is already loaded
	// starts straight away; any other slot reloads the world from its save. A new witch (or one from a save made
	// before witches had names) is named first.
	public class TitleView : View
	{
		public Action OpenSettings { get; set; }

		private ISaveService saveService;

		private IEventBusSender eventBus;

		private RectTransform slotList;

		private Transform viewpoint;

		private int confirmingErase = -1;

		private int naming = -1;

		/// <summary>
		/// The name form, while a slot is being named.
		/// </summary>
		public NameForm Naming { get; private set; }

		protected override void OnBuild()
		{
			saveService = ServiceLocator.Get<ISaveService>();
			eventBus = ServiceLocator.Get<IEventBusSender>();

			var shade = UIFactory.Image(Root, new Color(0.08f, 0.04f, 0.12f, 0.35f), "Shade");
			UIFactory.Fill(shade.rectTransform);

			var title = UIFactory.Label(Root, "Witch Craft", 110, UIFactory.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
			UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(110, -240), new Vector2(-110, -90));
			title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(4, -4);

			var subtitle = UIFactory.Label(Root, "A cosy island, a cauldron, a cat, and a home to build.", 30, UIFactory.Text, TextAnchor.UpperLeft, FontStyle.Italic);
			UIFactory.Place(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(116, -300), new Vector2(-110, -240));
			subtitle.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);

			var panel = UIFactory.Image(Root, UIFactory.Panel, "Slots");
			UIFactory.Place(panel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(110, 90), new Vector2(900, 620));
			UIFactory.Vertical(panel.gameObject, 12f, 28);

			UIFactory.Layout(UIFactory.Label(panel.transform, "Choose a save", 26, UIFactory.Accent, TextAnchor.MiddleLeft, FontStyle.Bold).gameObject, preferredHeight: 40);

			slotList = UIFactory.Rect("SlotList", panel.transform);
			UIFactory.Vertical(slotList.gameObject, 10f);
			UIFactory.Layout(slotList.gameObject, flexibleHeight: 1);

			var footer = UIFactory.Row(panel.transform, 12f, 50f);
			UIFactory.Button(footer, "Settings", () => OpenSettings?.Invoke(), 22, new Color(0.3f, 0.25f, 0.4f), 50);

			if (Application.platform != RuntimePlatform.WebGLPlayer)
				UIFactory.Button(footer, "Quit", Quit, 22, new Color(0.3f, 0.25f, 0.4f), 50);

			viewpoint = new GameObject("TitleViewpoint").transform;
			viewpoint.position = new Vector3(15f, 7.5f, -17f);
			viewpoint.LookAt(new Vector3(-2f, 2f, 2f));
		}

		protected override void OnOpen()
		{
			confirmingErase = -1;
			naming = -1;

			eventBus.SetCameraOverride(viewpoint);

			Refresh();
		}

		protected override void OnClose()
		{
			eventBus.SetCameraOverride(null);
		}

		// The title is the bottom of the menu stack: Escape does nothing here
		public override void Cancel() { }

		private void Refresh()
		{
			UIFactory.Clear(slotList);

			Naming = null;

			if (naming >= 0)
			{
				var chosen = naming;
				var fresh = !saveService.Describe(chosen).Exists;

				Naming = new NameForm(slotList,
					fresh ? $"Slot {chosen + 1}: a new witch, a tiny hut and a cat. What is your witch called?" : $"Slot {chosen + 1}: this witch has no name yet. What is she called?",
					"", fresh ? "Begin" : "Continue", name => Begin(chosen, name), () => { naming = -1; Refresh(); });

				Naming.Field.ActivateInputField();
				return;
			}

			for (var slot = 0; slot < saveService.SlotCount; slot++)
				SlotRow(slot);

			if (IsOpen)
				StartCoroutine(FocusFirst());
		}

		private void SlotRow(int slot)
		{
			var summary = saveService.Describe(slot);

			var row = UIFactory.Image(slotList, UIFactory.PanelLight, $"Slot{slot + 1}");
			UIFactory.Horizontal(row.gameObject, 12f, 12, false).childAlignment = TextAnchor.MiddleLeft;
			UIFactory.Layout(row.gameObject, preferredHeight: 92);

			var who = string.IsNullOrEmpty(summary.WitchName) ? "" : $"  ·  {summary.WitchName}";

			var text = summary.Exists
				? $"<b>Slot {slot + 1}{who}</b>{(slot == saveService.CurrentSlot ? "   <size=16><color=#" + UIFactory.Hex(UIFactory.Accent) + ">last played</color></size>" : "")}\n" +
				  $"<size=18>Day {summary.Day}  ·  Home level {summary.HomeLevel}  ·  {Plural(summary.Companions, "companion")}  ·  {Plural(summary.Spells, "spell")}\n" +
				  $"<color=#{UIFactory.Hex(UIFactory.TextMuted)}>{summary.LastPlayed:d MMM, HH:mm}</color></size>"
				: $"<b>Slot {slot + 1}</b>\n<size=18><color=#{UIFactory.Hex(UIFactory.TextMuted)}>Empty. A new witch, a tiny hut and a cat.</color></size>";

			var label = UIFactory.Label(row.transform, text, 22, UIFactory.Text, TextAnchor.MiddleLeft);
			UIFactory.Layout(label.gameObject, flexibleWidth: 1, preferredHeight: 80);

			var chosen = slot;

			var play = UIFactory.Button(row.transform, summary.Exists ? "Continue" : "Start", () => Play(chosen), 22, new Color(0.45f, 0.28f, 0.52f), 56);
			UIFactory.Layout(play.gameObject, minWidth: 150, preferredWidth: 150, minHeight: 56, preferredHeight: 56);

			if (!summary.Exists)
				return;

			var erase = UIFactory.Button(row.transform, confirmingErase == slot ? "Really erase?" : "Erase", () => Erase(chosen), 18, new Color(0.45f, 0.25f, 0.28f), 56);
			UIFactory.Layout(erase.gameObject, minWidth: 150, preferredWidth: 150, minHeight: 56, preferredHeight: 56);
		}

		private static string Plural(int count, string word) => count == 1 ? $"1 {word}" : $"{count} {word}s";

		private void Play(int slot)
		{
			if (string.IsNullOrEmpty(saveService.Describe(slot).WitchName) && !(slot == saveService.CurrentSlot && ServiceLocator.Get<IWitchProfile>().HasName))
			{
				naming = slot;
				Refresh();
				return;
			}

			Load(slot);
		}

		private void Begin(int slot, string name)
		{
			var witch = ServiceLocator.Get<IWitchProfile>();

			// The loaded world is this slot's, so name her now; otherwise the slot takes the name as it loads
			if (slot == saveService.CurrentSlot)
				witch.Rename(name);
			else
				witch.SetPendingName(name);

			naming = -1;

			Load(slot);
		}

		private void Load(int slot)
		{
			if (slot == saveService.CurrentSlot)
			{
				saveService.SavingEnabled = true;
				Close();
				return;
			}

			saveService.SelectSlot(slot);
			saveService.SavingEnabled = true;

			GameFlow.Reload(false);
		}

		private void Erase(int slot)
		{
			if (confirmingErase != slot)
			{
				confirmingErase = slot;
				Refresh();
				return;
			}

			confirmingErase = -1;

			saveService.Erase(slot);

			// The loaded world belongs to that slot, so rebuild it fresh
			if (slot == saveService.CurrentSlot)
			{
				saveService.SelectSlot(slot);
				GameFlow.Reload(true);
				return;
			}

			Refresh();
		}

		private static void Quit()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
			Application.Quit();
#endif
		}
	}
}
