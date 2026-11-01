using System;
using System.Collections.Generic;
using System.Linq;
using Alchemy;
using Character.Interfaces;
using Companions;
using Companions.Interfaces;
using Data.Enums;
using Data.Save;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using Home.Interfaces;
using Items.Interfaces;
using Persistence.Interfaces;
using Spells.Interfaces;
using UnityEngine;
using Utilities;
using World;


namespace Tutorial
{
	using Interfaces;

	// The first five minutes: Sable walks the witch through moving, discovering a spell, building, brewing and
	// befriending Pip. Each step is a check on game state rather than a scripted event, so doing things out of
	// order (or before being asked) still counts. A glowing beacon marks where to go.
	public class TutorialGuide : MonoBehaviour, ITutorialGuide, ISaveParticipant
	{
		private const float CheckInterval = 0.25f;

		private const float FarewellSeconds = 14f;

		public bool IsActive => stepIndex < steps.Count;

		private readonly List<Step> steps = new();

		private IEventBusSender eventBus;

		private ISaveService saveService;

		private ISpellBook spellBook;

		private IInventory inventory;

		private ICompanionRoster roster;

		private GameDatabase database;

		private IObserverTarget witch;

		private Transform beacon;

		private Vector3 startPosition;

		private bool hasStart;

		private bool builtSomething;

		private int stepIndex;

		private int announcedStep = -1;

		private float nextCheck;

		private float farewellUntil;

		private void Awake()
		{
			eventBus = ServiceLocator.Get<IEventBusSender>();
			saveService = ServiceLocator.Get<ISaveService>();
			spellBook = ServiceLocator.Get<ISpellBook>();
			inventory = ServiceLocator.Get<IInventory>();
			roster = ServiceLocator.Get<ICompanionRoster>();
			database = ServiceLocator.Get<GameDatabase>();

			if (eventBus == null)
				Debug.LogError($"{nameof(eventBus)} is null");

			ServiceLocator.Register<ITutorialGuide>(this);

			eventBus.ActiveTargetChanged += SetWitch;
			eventBus.PiecePlaced += Built;

			DefineSteps();
		}

		private void Start()
		{
			saveService.Register(this);

			beacon = BuildBeacon();
		}

		private void OnDestroy()
		{
			saveService?.Unregister(this);

			ServiceLocator.Unregister<ITutorialGuide>(this);

			eventBus.ActiveTargetChanged -= SetWitch;
			eventBus.PiecePlaced -= Built;
		}

		private void SetWitch(IObserverTarget target)
		{
			witch = target;
		}

		private void Built(Vector3 position)
		{
			builtSomething = true;
		}

		private void DefineSteps()
		{
			steps.Add(new Step(
				"Walk with WASD and look around with the mouse",
				"Sable: \"Morning! Stretch your legs, I'll follow.\"",
				() => hasStart && witch != null && Vector3.Distance(Flat(startPosition), Flat(witch.Transform.position)) > 4f,
				() => null));

			steps.Add(new Step(
				"Open your spellbook (B) and inscribe Terra + Forma",
				"Sable: \"Your spellbook's gathering dust. Put Terra and Forma on a page together and see what wakes up.\"",
				() => spellBook.KnowsSpell(database.FindSpell("conjure")),
				() => null));

			steps.Add(new Step(
				"Ready Conjure with 1",
				"Sable: \"Conjure! Press 1 to ready it.\"",
				() => witch != null && witch.Spells.ActiveSpell != null && witch.Spells.ActiveSpell.Effect == SpellEffectType.Conjure || builtSomething,
				() => null));

			steps.Add(new Step(
				"Conjure a floor on your plot: aim inside the golden border and click",
				"Sable: \"Build something inside the golden border. Scroll to pick a piece, Q/R to turn it.\"",
				() => builtSomething,
				() => ServiceLocator.Get<IHomeService>()?.Plot.transform.position + new Vector3(4f, 0f, 4f)));

			steps.Add(new Step(
				"Brew a Swiftness Brew at the alchemy bench (follow the recipe book)",
				"Sable: \"You've everything for a Swiftness Brew in your satchel. The bench is under the awning. Keep that fire in the green!\"",
				() => inventory.Count(database.FindItem("swiftness_brew")) > 0 || database.Items.Any(item => item.Category == ItemCategory.Potion && item.PotionEffect != PotionEffect.None && inventory.Count(item) > 0),
				() => FindAnyObjectByType<AlchemyStation>()?.transform.position));

			steps.Add(new Step(
				"Pip the frog at the pond (west) wants 3 Glowcaps. They glow in shady spots",
				"Sable: \"There's a frog by the pond who keeps hinting they'd like to move in. Glowcaps grow where it's shady.\"",
				() => roster.IsResident(database.FindCompanion("pip")),
				() => FindObjectsByType<CompanionRecruit>(FindObjectsSortMode.None).FirstOrDefault(recruit => recruit.name.StartsWith("Pip"))?.transform.position));
		}

		private void Update()
		{
			if (witch != null && !hasStart)
			{
				startPosition = witch.Transform.position;
				hasStart = true;
			}

			if (farewellUntil > 0f && Time.time > farewellUntil)
			{
				farewellUntil = 0f;
				eventBus.SetObjective("");
			}

			if (!IsActive || Time.time < nextCheck)
				return;

			nextCheck = Time.time + CheckInterval;

			// Several steps can be done at once, so keep advancing while they are satisfied
			while (IsActive && steps[stepIndex].Done())
				stepIndex++;

			Announce();

			UpdateBeacon();
		}

		private void Announce()
		{
			if (announcedStep == stepIndex)
				return;

			announcedStep = stepIndex;

			if (!IsActive)
			{
				Finish();
				return;
			}

			var step = steps[stepIndex];

			eventBus.SetObjective(step.Objective);
			eventBus.Notify(step.Line);
		}

		private void Finish()
		{
			eventBus.SetObjective("Explore! Glyph shrines glow at the pond, atop the sky spire, in the crystal cave and on the sky island");
			eventBus.Notify("Sable: \"That's the basics. The island's yours. I'll be right behind you.\"");

			farewellUntil = Time.time + FarewellSeconds;

			if (beacon != null)
				beacon.gameObject.SetActive(false);
		}

		public void Skip()
		{
			stepIndex = steps.Count;
			announcedStep = -1;

			eventBus.SetObjective("");

			if (beacon != null)
				beacon.gameObject.SetActive(false);
		}

		private void UpdateBeacon()
		{
			if (beacon == null)
				return;

			var target = IsActive ? steps[stepIndex].Target() : null;

			beacon.gameObject.SetActive(target.HasValue);

			if (target.HasValue)
				beacon.position = target.Value;
		}

		private Transform BuildBeacon()
		{
			var root = new GameObject("TutorialBeacon").transform;

			var gem = GameObject.CreatePrimitive(PrimitiveType.Cube);
			gem.name = "Gem";
			Destroy(gem.GetComponent<Collider>());
			gem.transform.SetParent(root, false);
			gem.transform.localPosition = Vector3.up * 3.2f;
			gem.transform.localScale = Vector3.one * 0.45f;
			gem.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
			gem.GetComponent<Renderer>().sharedMaterial = database.GlowMaterial;
			RendererTint.Apply(gem, new Color(1f, 0.85f, 0.45f), 2f);
			gem.AddComponent<Bobbing>();

			var light = new GameObject("Light").AddComponent<Light>();
			light.transform.SetParent(root, false);
			light.transform.localPosition = Vector3.up * 2.5f;
			light.color = new Color(1f, 0.85f, 0.5f);
			light.range = 7f;
			light.intensity = 2.5f;

			FxUtility.Loop(root, Vector3.zero, new Color(1f, 0.85f, 0.5f), 12f, 1.5f, 0.6f, 0.1f);

			root.gameObject.SetActive(false);

			return root;
		}

		private static Vector3 Flat(Vector3 value) => new(value.x, 0f, value.z);

		public void Read(SaveData data)
		{
			stepIndex = Mathf.Clamp(data.TutorialStep, 0, steps.Count);

			// A finished tutorial stays quiet on later visits
			announcedStep = IsActive ? -1 : stepIndex;
		}

		public void Write(SaveData data)
		{
			data.TutorialStep = stepIndex;
		}

		private class Step
		{
			public readonly string Objective;

			public readonly string Line;

			public readonly Func<bool> Done;

			public readonly Func<Vector3?> Target;

			public Step(string objective, string line, Func<bool> done, Func<Vector3?> target)
			{
				Objective = objective;
				Line = line;
				Done = done;
				Target = target;
			}
		}
	}
}
