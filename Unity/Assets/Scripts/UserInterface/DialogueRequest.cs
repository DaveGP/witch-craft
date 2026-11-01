using System;
using System.Collections.Generic;
using UnityEngine;

namespace UserInterface
{
	public class DialogueRequest
	{
		public readonly string Title;

		public readonly string Body;

		public readonly Color Colour;

		public readonly IReadOnlyList<DialogueOption> Options;

		public DialogueRequest(string title, string body, Color colour, IReadOnlyList<DialogueOption> options)
		{
			Title = title;
			Body = body;
			Colour = colour;
			Options = options;
		}
	}

	public class DialogueOption
	{
		public readonly string Label;

		/// <summary>
		/// Runs after the choice is made. Null just closes the dialogue.
		/// </summary>
		public readonly Action Action;

		/// <summary>
		/// Leave the dialogue up while the action runs, for actions that show a follow-up.
		/// </summary>
		public readonly bool KeepOpen;

		public DialogueOption(string label, Action action, bool keepOpen = false)
		{
			Label = label;
			Action = action;
			KeepOpen = keepOpen;
		}
	}
}
