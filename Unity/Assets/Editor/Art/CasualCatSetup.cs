using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Art;
using Data.ScriptableObjects;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static WitchCraftEditor.WitchCraftBuildKit;
using Object = UnityEngine.Object;


namespace WitchCraftEditor
{
	// Turns the Casual Cat - Black pack into Sable: converts its materials to URP, builds an Animator Controller
	// with a state per clip (named after the clip, so the cat can crossfade by name as the pack's demo does),
	// measures the rig and saves a prefab variant with a CatRigProfile. Everything generated stays in the pack's
	// folder, which is local-only. Fills the Art Set's cat and cat bed slots when they are empty.
	public static class CasualCatSetup
	{
		public const string PackFolder = "Assets/ThirdParty/AssetStore/CasualCat-Black";

		private const string OutputFolder = PackFolder + "/WitchCraft";

		private const string ControllerPath = OutputFolder + "/SableCat.controller";

		public const string VariantPath = OutputFolder + "/SableCat.prefab";

		// Sable is a small cat next to a 1.4 m witch
		private const float TargetHeadHeight = 0.32f;

		private const float BedWidth = 0.8f;

		private const int Samples = 120;

		private static readonly string[] Paws = { "foot.L", "foot.R", "hand.L", "hand.R" };

		private static GameObject CatPrefab => AssetDatabase.LoadAssetAtPath<GameObject>($"{PackFolder}/Prefabs/CatBlack.prefab");

		private static GameObject CushionPrefab => AssetDatabase.LoadAssetAtPath<GameObject>($"{PackFolder}/Prefabs/Cushion.prefab");

		public static bool PackPresent => CatPrefab != null;

		[MenuItem("Witch Craft/Art/Set Up Casual Cat", false, 40)]
		private static void Menu()
		{
			if (!Run())
				EditorUtility.DisplayDialog("Casual Cat", $"The Casual Cat - Black pack wasn't found in {PackFolder}.", "OK");
		}

		/// <summary>
		/// Sets the pack up for Sable. False when the pack isn't on this machine.
		/// </summary>
		public static bool Run()
		{
			if (!PackPresent)
				return false;

			EnsureFolder(OutputFolder);

			var converted = UrpMaterialConverter.ConvertFolder(PackFolder);

			var clips = AssetDatabase.FindAssets("t:AnimationClip", new[] { $"{PackFolder}/Animations/CatB" })
				.Select(guid => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid)))
				.Where(clip => clip != null && Regex.IsMatch(clip.name, @"^\d\d"))
				.OrderBy(clip => clip.name)
				.ToList();

			var states = Classify(clips);
			var controller = BuildController(clips, states);

			var measured = Measure(clips);
			var variant = BuildVariant(controller, states, measured);

			var scale = TargetHeadHeight / measured.HeadHeight;

			FillArtSet(variant, scale, measured.Yaw);

			Debug.Log($"{nameof(CasualCatSetup)}: {clips.Count} clips, {converted} materials converted, head {measured.HeadHeight:0.000} → scale {scale:0.000}, yaw {measured.Yaw:0}°, " +
				$"walk {measured.WalkSpeed:0.000} m/s at scale 1 ({measured.WalkSpeed * scale:0.00} m/s in game), jump airborne {measured.Takeoff:0.00}-{measured.Landing:0.00}, lift {measured.Lift:0.000}, leap {measured.Distance:0.000}");

			return true;
		}

		#region States

		private static readonly (string prefix, CatRigProfile.Role role)[] roles =
		{
			("Idle", CatRigProfile.Role.Idle), ("Walk", CatRigProfile.Role.Walk), ("Jump", CatRigProfile.Role.Jump),
			("Catloaf", CatRigProfile.Role.Loaf), ("Sit", CatRigProfile.Role.Sit), ("Grooming", CatRigProfile.Role.Groom),
			("Scratch", CatRigProfile.Role.Scratch), ("Sleep", CatRigProfile.Role.Sleep), ("Threat", CatRigProfile.Role.ThreatShort),
			("Lyingdown", CatRigProfile.Role.LieDown), ("Rollingover", CatRigProfile.Role.RollOver),
		};

		// Names look like 06SitL01, 14SleepL01_Blink or 03Catloaf: number, action, optional side and variant
		private static List<CatRigProfile.State> Classify(List<AnimationClip> clips)
		{
			var states = new List<CatRigProfile.State>();

			foreach (var clip in clips)
			{
				var action = clip.name.TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
				var match = roles.FirstOrDefault(entry => action.StartsWith(entry.prefix));

				if (match.prefix == null)
				{
					Debug.LogWarning($"{nameof(CasualCatSetup)}: no role for {clip.name}");
					continue;
				}

				var rest = action.Substring(match.prefix.Length);

				var side = rest.StartsWith("L") ? CatRigProfile.Side.Left
					: rest.StartsWith("R") ? CatRigProfile.Side.Right
					: CatRigProfile.Side.Centre;

				var role = match.role == CatRigProfile.Role.Sleep && action.Contains("Blink") ? CatRigProfile.Role.SleepBlink : match.role;

				states.Add(new CatRigProfile.State { Name = clip.name, Role = role, Side = side, Length = clip.length });
			}

			// Two threats a side: the longer one is the full puffed-up hiss
			foreach (var side in new[] { CatRigProfile.Side.Left, CatRigProfile.Side.Right })
			{
				var threats = states.Where(state => state.Role == CatRigProfile.Role.ThreatShort && state.Side == side).OrderBy(state => state.Length).ToList();

				for (var i = 1; i < threats.Count; i++)
					threats[i].Role = CatRigProfile.Role.ThreatLong;
			}

			return states;
		}

		private static AnimatorController BuildController(List<AnimationClip> clips, List<CatRigProfile.State> states)
		{
			// Rebuilt in place so the variant's reference (and GUID) survives
			var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) ?? AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

			foreach (var parameter in controller.parameters)
				controller.RemoveParameter(parameter);

			controller.AddParameter(new AnimatorControllerParameter { name = "WalkSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });
			controller.AddParameter(new AnimatorControllerParameter { name = "JumpSpeed", type = AnimatorControllerParameterType.Float, defaultFloat = 1f });

			var machine = controller.layers[0].stateMachine;

			foreach (var child in machine.states.ToArray())
				machine.RemoveState(child.state);

			AnimatorState idle = null;

			for (var i = 0; i < clips.Count; i++)
			{
				var clip = clips[i];
				var info = states.FirstOrDefault(state => state.Name == clip.name);

				var state = machine.AddState(clip.name, new Vector3(300f + i / 8 * 220f, i % 8 * 60f, 0f));
				state.motion = clip;

				if (info?.Role == CatRigProfile.Role.Walk)
				{
					state.speedParameterActive = true;
					state.speedParameter = "WalkSpeed";
				}
				else if (info?.Role == CatRigProfile.Role.Jump)
				{
					state.speedParameterActive = true;
					state.speedParameter = "JumpSpeed";
				}

				if (info?.Role == CatRigProfile.Role.Idle)
					idle = state;
			}

			if (idle != null)
				machine.defaultState = idle;

			EditorUtility.SetDirty(controller);
			AssetDatabase.SaveAssets();

			return controller;
		}

		#endregion

		#region Measuring

		private class Measurements
		{
			public float HeadHeight = 0.3f;
			public float Yaw;
			public float WalkSpeed = 0.5f;
			public float Takeoff = 0.3f;
			public float Landing = 0.6f;
			public float Lift;
			public float Distance;
		}

		// Samples the clips on a throwaway copy of the cat at the origin, unscaled
		private static Measurements Measure(List<AnimationClip> clips)
		{
			var result = new Measurements();
			var cat = Object.Instantiate(CatPrefab);
			cat.hideFlags = HideFlags.HideAndDontSave;

			try
			{
				var bones = cat.GetComponentsInChildren<Transform>(true).GroupBy(bone => bone.name).ToDictionary(group => group.Key, group => group.First());

				var head = bones.GetValueOrDefault("head");
				var tail = bones.GetValueOrDefault("tail01");
				var paws = Paws.Select(bones.GetValueOrDefault).Where(paw => paw != null).ToArray();

				if (head == null || tail == null || paws.Length == 0)
				{
					Debug.LogError($"{nameof(CasualCatSetup)}: the rig has no head, tail01 or paw bones");
					return result;
				}

				var idle = clips.FirstOrDefault(clip => clip.name.Contains("Idle"));
				var walk = clips.FirstOrDefault(clip => clip.name.Contains("Walk"));
				var jump = clips.FirstOrDefault(clip => clip.name.Contains("Jump"));

				if (idle != null)
					idle.SampleAnimation(cat, 0f);

				result.HeadHeight = Mathf.Max(0.01f, head.position.y - cat.transform.position.y);

				// Yaw that turns tail-to-head onto +Z
				var forward = head.position - tail.position;
				forward.y = 0f;
				result.Yaw = Vector3.SignedAngle(forward.normalized, Vector3.forward, Vector3.up);
				forward.Normalize();

				if (walk != null)
					result.WalkSpeed = MeasureWalk(cat, walk, paws, forward);

				if (jump != null)
					MeasureJump(cat, jump, paws, bones.GetValueOrDefault("Root"), result);
			}
			finally
			{
				Object.DestroyImmediate(cat);
			}

			return result;
		}

		// In-place walk: each paw sweeps back along its stride while it carries the cat, so stride over sweep time
		// is the speed the cat would travel at
		private static float MeasureWalk(GameObject cat, AnimationClip walk, Transform[] paws, Vector3 forward)
		{
			var speeds = new List<float>();
			var step = walk.length / Samples;

			foreach (var paw in paws)
			{
				var along = new float[Samples];

				for (var i = 0; i < Samples; i++)
				{
					walk.SampleAnimation(cat, i * step);
					along[i] = Vector3.Dot(paw.position, forward);
				}

				var sweeping = 0;

				for (var i = 0; i < Samples; i++)
					if (along[(i + 1) % Samples] < along[i])
						sweeping++;

				if (sweeping > 0)
					speeds.Add((along.Max() - along.Min()) / (sweeping * step));
			}

			return speeds.Count == 0 ? 0.5f : Mathf.Max(0.05f, speeds.Average());
		}

		// The pack's jump leaps forward by moving its Root bone; the cat pins Root in place and moves itself
		private static void MeasureJump(GameObject cat, AnimationClip jump, Transform[] paws, Transform root, Measurements result)
		{
			var step = jump.length / Samples;
			var lowest = new float[Samples];
			var travel = new float[Samples];

			for (var i = 0; i < Samples; i++)
			{
				jump.SampleAnimation(cat, i * step);
				lowest[i] = paws.Min(paw => paw.position.y);
				travel[i] = root != null ? new Vector2(root.position.x, root.position.z).magnitude : 0f;
			}

			var ground = lowest.Min();
			var lift = lowest.Max() - ground;

			result.Lift = lift;

			if (lift < 0.01f)
				return;

			var threshold = ground + lift * 0.15f;

			var first = System.Array.FindIndex(lowest, height => height > threshold);
			var last = System.Array.FindLastIndex(lowest, height => height > threshold);

			if (first < 0 || last <= first)
				return;

			result.Takeoff = (float)first / Samples;
			result.Landing = (float)(last + 1) / Samples;
			result.Distance = travel[Mathf.Min(last + 1, Samples - 1)] - travel[first];
		}

		#endregion

		#region Prefab and Art Set

		private static GameObject BuildVariant(AnimatorController controller, List<CatRigProfile.State> states, Measurements measured)
		{
			var instance = (GameObject)PrefabUtility.InstantiatePrefab(CatPrefab);

			try
			{
				var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
				animator.runtimeAnimatorController = controller;
				animator.applyRootMotion = false;

				var profile = instance.GetComponent<CatRigProfile>() ?? instance.AddComponent<CatRigProfile>();
				profile.States = states;
				profile.WalkSpeed = measured.WalkSpeed;
				profile.JumpTakeoff = measured.Takeoff;
				profile.JumpLanding = measured.Landing;
				profile.JumpLift = measured.Lift;
				profile.JumpDistance = measured.Distance;
				profile.HeadHeight = measured.HeadHeight;

				return PrefabUtility.SaveAsPrefabAsset(instance, VariantPath);
			}
			finally
			{
				Object.DestroyImmediate(instance);
			}
		}

		private static void FillArtSet(GameObject variant, float scale, float yaw)
		{
			var set = LoadOrCreate<ArtSet>(ArtSetPath);

			var cat = Entry(set, "cat");

			if (cat.Prefab == null)
			{
				cat.Prefab = variant;
				cat.Scale = scale;
				cat.Rotation = new Vector3(0f, yaw, 0f);
				cat.Offset = Vector3.zero;
			}

			var bed = Entry(set, "piece:cat_bed");

			if (bed.Prefab == null && CushionPrefab != null)
			{
				var bounds = Bounds(CushionPrefab);
				var bedScale = BedWidth / Mathf.Max(0.01f, Mathf.Max(bounds.size.x, bounds.size.z));

				bed.Prefab = CushionPrefab;
				bed.Scale = bedScale;
				bed.Rotation = Vector3.zero;

				// Centred on the placeholder, resting on the floor
				bed.Offset = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * bedScale;
			}

			set.Entries = set.Entries.OrderBy(entry => entry.Key).ToList();
			set.NotifyChanged();

			EditorUtility.SetDirty(set);
			AssetDatabase.SaveAssets();
		}

		private static ArtEntry Entry(ArtSet set, string key)
		{
			var entry = set.Entries.FirstOrDefault(existing => existing.Key == key);

			if (entry == null)
			{
				entry = new ArtEntry { Key = key };
				set.Entries.Add(entry);
			}

			return entry;
		}

		private static Bounds Bounds(GameObject prefab)
		{
			var instance = Object.Instantiate(prefab);
			instance.hideFlags = HideFlags.HideAndDontSave;

			try
			{
				var renderers = instance.GetComponentsInChildren<Renderer>();

				if (renderers.Length == 0)
					return new Bounds(Vector3.zero, Vector3.one);

				var bounds = renderers[0].bounds;

				foreach (var renderer in renderers.Skip(1))
					bounds.Encapsulate(renderer.bounds);

				return bounds;
			}
			finally
			{
				Object.DestroyImmediate(instance);
			}
		}

		#endregion
	}
}
