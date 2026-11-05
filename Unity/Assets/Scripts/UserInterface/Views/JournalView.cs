using Companions.Interfaces;
using Data.ScriptableObjects;
using DependencyInjection;
using UnityEngine;
using UnityEngine.UI;

namespace UserInterface.Views
{
	// The witch's notes on everyone she's trying to win over: what's done, and what they've asked for now. What they'll
	// ask next stays a surprise.
	public class JournalView : View
	{
		private IQuestLog quests;

		private RectTransform list;

		protected override void OnBuild()
		{
			quests = ServiceLocator.Get<IQuestLog>();

			var shade = UIFactory.Image(Root, new Color(0.05f, 0.03f, 0.08f, 0.6f), "Shade");
			UIFactory.Fill(shade.rectTransform);

			var panel = UIFactory.Image(Root, new Color(0.25f, 0.2f, 0.28f, 0.97f), "Journal");
			UIFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-560, -420), new Vector2(560, 420));

			var heading = UIFactory.Label(panel.transform, "Journal", 38, UIFactory.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
			UIFactory.Place(heading.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -86), new Vector2(-36, -24));

			var hint = UIFactory.Label(panel.transform, "J or Esc to close", 18, UIFactory.TextMuted, TextAnchor.UpperRight);
			UIFactory.Place(hint.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(36, -60), new Vector2(-36, -30));

			var area = UIFactory.Rect("List", panel.transform);
			UIFactory.Place(area, Vector2.zero, Vector2.one, new Vector2(36, 36), new Vector2(-36, -100));

			list = UIFactory.ScrollList(area, out var scroll, 14f);
			UIFactory.Fill((RectTransform)scroll.transform);
		}

		protected override void OnOpen()
		{
			UIFactory.Clear(list);

			var active = quests.Active;

			if (active.Count == 0)
			{
				var empty = UIFactory.Label(list, "Nobody to win over just now. The authors of the island's storybooks are worth looking for.", 23, UIFactory.TextMuted);
				UIFactory.Layout(empty.gameObject, preferredHeight: 70);
				return;
			}

			foreach (var companion in active)
				Entry(companion);
		}

		private void Entry(CompanionDefinition companion)
		{
			var row = UIFactory.Image(list, new Color(0.34f, 0.28f, 0.38f), "Companion");
			UIFactory.Vertical(row.gameObject, 6f, 16);

			var done = quests.HasStarted(companion) ? quests.StepIndex(companion) : 0;

			var lines = $"<b><color=#{UIFactory.Hex(companion.Color)}>{companion.DisplayName} the {companion.Species}</color></b>  <size=17>{done} of {companion.Quests.Count} done</size>";

			for (var i = 0; i < done; i++)
				lines += $"\n<size=19><color=#{UIFactory.Hex(UIFactory.TextMuted)}>✓ {companion.Quests[i].Objective}</color></size>";

			lines += $"\n<size=21>▸ {quests.NextStep(companion)}</size>";

			var label = UIFactory.Label(row.transform, lines, 25);

			var height = 62 + done * 28;
			UIFactory.Layout(label.gameObject, preferredHeight: height);
			UIFactory.Layout(row.gameObject, preferredHeight: height + 32);
		}
	}
}
