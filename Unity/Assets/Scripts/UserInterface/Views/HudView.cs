using System.Collections.Generic;
using System.Linq;
using Character.Interfaces;
using Character.Input.Interfaces;
using Companions.Interfaces;
using Data.Enums;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using GameState.Interfaces;
using Home.Interfaces;
using Interaction.Interfaces;
using Items.Interfaces;
using Potions.Interfaces;
using Sky.Interfaces;
using Spells.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace UserInterface.Views
{
	// Always-on overlay: interaction prompt, toasts, spell hotbar, broom charge and active potions.
	public class HudView : MonoBehaviour
	{
		private const float ToastLifetime = 4.5f;

		private const int MaxToasts = 5;

		private IEventBus eventBus;

		private IInputModeService inputModeService;

		private ISpellBook spellBook;

		private IPotionEffects potionEffects;

		private IInventory inventory;

		private ICompanionRoster roster;

		private IControlScheme controlScheme;

		private Text help;

		private IInteractable focus;

		private IObserverTarget target;

		private RectTransform root;

		private Text prompt;

		private Text status;

		private Text crosshair;

		private Text potions;

		private Text homeInfo;

		private GameObject objectivePanel;

		private Text objective;

		private RectTransform toastList;

		private GameObject broomPanel;

		private Image broomFill;

		private readonly List<(Image back, Text label)> slots = new();

		private readonly List<(Text text, float born)> toasts = new();

		public void Build(RectTransform parent)
		{
			eventBus = ServiceLocator.Get<IEventBus>();
			inputModeService = ServiceLocator.Get<IInputModeService>();
			spellBook = ServiceLocator.Get<ISpellBook>();
			potionEffects = ServiceLocator.Get<IPotionEffects>();
			inventory = ServiceLocator.Get<IInventory>();
			roster = ServiceLocator.Get<ICompanionRoster>();
			controlScheme = ServiceLocator.Get<IControlScheme>();

			root = parent;

			BuildPrompt();
			BuildHotbar();
			BuildToasts();
			BuildBroom();
			BuildCorners();

			eventBus.ActiveTargetChanged += SetTarget;
			eventBus.InteractionFocusChanged += SetFocus;
			eventBus.Notified += Toast;
			eventBus.ItemCollected += Collected;
			eventBus.SpellSlotsChanged += RefreshSlots;
			eventBus.SpellLearned += SpellLearned;
			eventBus.ObjectiveChanged += SetObjective;

			if (controlScheme != null)
				controlScheme.Changed += RefreshPrompts;

			SetFocus(null);
			RefreshSlots();
			RefreshPrompts();
		}

		// The event bus and control scheme outlive the scene, so let go of them when the scene reloads
		private void OnDestroy()
		{
			if (eventBus != null)
			{
				eventBus.ActiveTargetChanged -= SetTarget;
				eventBus.InteractionFocusChanged -= SetFocus;
				eventBus.Notified -= Toast;
				eventBus.ItemCollected -= Collected;
				eventBus.SpellSlotsChanged -= RefreshSlots;
				eventBus.SpellLearned -= SpellLearned;
				eventBus.ObjectiveChanged -= SetObjective;
			}

			if (controlScheme != null)
				controlScheme.Changed -= RefreshPrompts;
		}

		private void SetTarget(IObserverTarget value) => target = value;

		private void SpellLearned(SpellDefinition spell) => RefreshSlots();

		private void BuildPrompt()
		{
			prompt = UIFactory.Label(root, "", 26, UIFactory.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
			UIFactory.Place(prompt.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-400, 190), new Vector2(400, 230));
			prompt.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);

			status = UIFactory.Label(root, "", 20, UIFactory.Accent, TextAnchor.MiddleCenter);
			UIFactory.Place(status.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-600, 120), new Vector2(600, 150));
			status.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1, -1);

			crosshair = UIFactory.Label(root, "◦", 34, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter);
			UIFactory.Place(crosshair.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-20, -20), new Vector2(20, 20));
		}

		private void BuildHotbar()
		{
			var bar = UIFactory.Rect("Hotbar", root);
			UIFactory.Place(bar, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-330, 30), new Vector2(330, 110));
			UIFactory.Horizontal(bar.gameObject, 10f);

			for (var i = 0; i < ISpellBook.SlotCount; i++)
			{
				var back = UIFactory.Image(bar, UIFactory.Panel, $"Slot{i + 1}");

				var key = UIFactory.Label(back.transform, $"{i + 1}", 16, UIFactory.TextMuted);
				UIFactory.Place(key.rectTransform, Vector2.zero, Vector2.one, new Vector2(8, 4), new Vector2(-4, -4));

				var label = UIFactory.Label(back.transform, "", 18, UIFactory.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
				UIFactory.Fill(label.rectTransform, 6f);

				slots.Add((back, label));
			}
		}

		private void BuildToasts()
		{
			toastList = UIFactory.Rect("Toasts", root);
			UIFactory.Place(toastList, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-400, -260), new Vector2(400, -30));

			var group = UIFactory.Vertical(toastList.gameObject, 6f);
			group.childAlignment = TextAnchor.UpperCenter;
		}

		private void BuildBroom()
		{
			var panel = UIFactory.Image(root, UIFactory.Panel, "Broom");
			UIFactory.Place(panel.rectTransform, Vector2.zero, Vector2.zero, new Vector2(30, 30), new Vector2(300, 90));
			UIFactory.Vertical(panel.gameObject, 4f, 10);

			UIFactory.Label(panel.transform, "Broom magic", 16, UIFactory.TextMuted);
			broomFill = UIFactory.Bar(panel.transform, new Color(0, 0, 0, 0.5f), new Color(0.6f, 0.75f, 1f));

			broomPanel = panel.gameObject;
		}

		private void BuildCorners()
		{
			potions = UIFactory.Label(root, "", 20, UIFactory.Text);
			UIFactory.Place(potions.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30, -300), new Vector2(420, -30));
			potions.gameObject.AddComponent<Shadow>();

			homeInfo = UIFactory.Label(root, "", 20, UIFactory.Text, TextAnchor.UpperRight);
			UIFactory.Place(homeInfo.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-520, -120), new Vector2(-30, -30));
			homeInfo.gameObject.AddComponent<Shadow>();

			var panel = UIFactory.Image(root, new Color(0.13f, 0.09f, 0.17f, 0.82f), "Objective");
			UIFactory.Place(panel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-520, -230), new Vector2(-30, -130));

			var accent = UIFactory.Image(panel.transform, UIFactory.Accent, "Accent");
			UIFactory.Place(accent.rectTransform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(5, 0));

			var heading = UIFactory.Label(panel.transform, "SABLE SUGGESTS", 14, UIFactory.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
			UIFactory.Place(heading.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20, -30), new Vector2(-16, -10));

			objective = UIFactory.Label(panel.transform, "", 20, UIFactory.Text);
			UIFactory.Place(objective.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 10), new Vector2(-16, -32));

			objectivePanel = panel.gameObject;
			objectivePanel.SetActive(false);

			help = UIFactory.Label(root, "", 16, UIFactory.TextMuted, TextAnchor.LowerRight);
			UIFactory.Place(help.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-640, 24), new Vector2(-30, 80));
			help.gameObject.AddComponent<Shadow>();
		}

		private static string Clock()
		{
			var dayCycle = ServiceLocator.Get<IDayCycle>();

			if (dayCycle == null)
				return "";

			// Shown in ten minute steps so the clock ticks calmly rather than racing
			var minutes = Mathf.FloorToInt(dayCycle.Hour * 6f) * 10;

			var weather = ServiceLocator.Get<IWeather>();

			var sky = weather?.Current switch
			{
				Data.Enums.WeatherKind.Rain => "  ·  Rain",
				Data.Enums.WeatherKind.Cloudy => "  ·  Cloudy",
				_ => ""
			};

			return $"Day {dayCycle.Day}  ·  {minutes / 60:00}:{minutes % 60:00}  ·  {dayCycle.PeriodName}{sky}";
		}

		private string HomeSummary()
		{
			var home = ServiceLocator.Get<IHomeService>();

			return home != null ? $"Home level {home.Level}  ·  Comfort {home.Comfort}  ·  {roster.Residents.Count} companions" : "";
		}

		private void SetObjective(string text)
		{
			objective.text = text;
			objectivePanel.SetActive(!string.IsNullOrEmpty(text));
		}

		private void SetFocus(IInteractable interactable)
		{
			focus = interactable;

			var button = Gamepad ? "(X)" : "[E]";

			prompt.text = interactable != null ? $"<color=#{UIFactory.Hex(UIFactory.Accent)}>{button}</color> {interactable.Prompt}" : "";
		}

		private bool Gamepad => controlScheme != null && controlScheme.UsingGamepad;

		private void RefreshPrompts()
		{
			help.text = Gamepad
				? "Left stick move · A jump · X interact · Y broom · D-pad ready spell 1/2 · RT cast\nView satchel · Right stick click spellbook · Start menu"
				: "WASD move · Space jump · Shift sprint · E interact · F broom\n1-4 ready spell · Click cast · Tab satchel · B spellbook · Esc menu";

			SetFocus(focus);
		}

		private void Collected(ItemDefinition item, int amount)
		{
			Toast($"<color=#{UIFactory.Hex(item.Color)}>+{amount} {item.DisplayName}</color>  ({inventory.Count(item)})");
		}

		private void Toast(string message)
		{
			if (toasts.Count >= MaxToasts)
				RemoveToast(0);

			var back = UIFactory.Image(toastList, new Color(0.1f, 0.07f, 0.13f, 0.8f), "Toast");
			UIFactory.Horizontal(back.gameObject, 0f, 10);
			back.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

			var text = UIFactory.Label(back.transform, message, 21, UIFactory.Text, TextAnchor.MiddleCenter);
			UIFactory.Layout(text.gameObject, preferredWidth: Mathf.Min(760, message.Length * 11 + 20));

			toasts.Add((text, Time.unscaledTime));
		}

		private void RemoveToast(int index)
		{
			Destroy(toasts[index].text.transform.parent.gameObject);
			toasts.RemoveAt(index);
		}

		private void RefreshSlots()
		{
			for (var i = 0; i < slots.Count; i++)
			{
				var spell = spellBook.GetSlot(i);

				var readied = target?.Spells?.ActiveSpell == spell && spell != null;

				slots[i].label.text = spell != null ? spell.DisplayName : "";
				slots[i].label.color = spell != null ? Color.Lerp(spell.Color, Color.white, 0.4f) : UIFactory.TextMuted;
				slots[i].back.color = readied ? Color.Lerp(UIFactory.Panel, spell.Color, 0.55f) : UIFactory.Panel;
			}
		}

		private void Update()
		{
			var gameplay = inputModeService.Mode == InputMode.Gameplay;

			root.gameObject.SetActive(gameplay);

			for (var i = toasts.Count - 1; i >= 0; i--)
			{
				var age = Time.unscaledTime - toasts[i].born;

				if (age > ToastLifetime)
				{
					RemoveToast(i);
					continue;
				}

				toasts[i].text.color = new Color(1f, 1f, 1f, Mathf.Clamp01((ToastLifetime - age) * 2f));
			}

			if (target == null)
				return;

			status.text = target.Spells.Status;

			crosshair.enabled = target.Spells.ActiveSpell != null && target.Spells.IsAiming;

			broomPanel.SetActive(target.Broom.IsMounted || target.Broom.Charge01 < 0.999f);
			broomFill.fillAmount = target.Broom.Charge01;
			broomFill.color = potionEffects.IsActive(PotionEffect.Skyward) ? UIFactory.Accent : new Color(0.6f, 0.75f, 1f);

			potions.text = string.Join("\n", potionEffects.Active.Select(pair => $"{pair.Key}  {Mathf.CeilToInt(pair.Value)}s"));

			homeInfo.text = $"{Clock()}\n<size=17><color=#{UIFactory.Hex(UIFactory.TextMuted)}>{HomeSummary()}</color></size>";

			if (Time.frameCount % 10 == 0)
				RefreshSlots();
		}
	}
}
