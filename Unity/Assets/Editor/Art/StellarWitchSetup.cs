using System.Linq;
using Data.ScriptableObjects;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static WitchCraftEditor.WitchCraftBuildKit;
using Object = UnityEngine.Object;

namespace WitchCraftEditor
{
	// Makes the Stellar Witch model the player's art: a local copy of her prefab with her wand in her right hand and
	// her hair physics (UnityChanSpringBone, when installed), driven by a generated Animator Controller.
	// The base layer is Synty's Base Locomotion (idle, walk, run, sprint, jump, fall, land, and a crouch for the
	// broom); an upper body layer plays the Human Spellcasting clips: a ready stance while a spell is readied and a
	// cast for each click, picked by CastStyle. Everything is written inside the packs' own (git-ignored) folders.
	public static class StellarWitchSetup
	{
		public const string WitchFolder = "Assets/ThirdParty/AssetStore/StellarWitch3D";

		private const string LocomotionFolder = "Assets/ThirdParty/Synty/AnimationBaseLocomotion/Animations/Polygon/Feminine";

		private const string CastingFolder = "Assets/ThirdParty/AssetStore/Human Animations";

		private const string SpellFolder = CastingFolder + "/Animations/Female/Combat/Spellcasting";

		private const string OutputFolder = WitchFolder + "/WitchCraft";

		private const string ControllerPath = OutputFolder + "/StellarWitch.controller";

		public const string VariantPath = OutputFolder + "/StellarWitchPlayer.prefab";

		// Head bone height in game, next to a 1.6 m capsule
		private const float TargetHeadHeight = 1.42f;

		/// <summary>
		/// CastStyle values the casting layer understands; CharacterAnimator picks one per spell.
		/// </summary>
		public static readonly string[] CastClips =
		{
			"MagicAttacks/Directional/HumanF@MagicAttackDirect1H01_R - Cast",
			"MagicAttacks/Directional/HumanF@MagicAttackDirect2H01 - Cast",
			"MagicAttacks/Call/HumanF@MagicAttackCall1H01_L - Cast",
			"MagicAttacks/Omnidirectional/HumanF@MagicAttackOmni01 - Cast",
		};

		private static GameObject WitchPrefab => AssetDatabase.LoadAssetAtPath<GameObject>($"{WitchFolder}/Prefabs/StellarWitch.prefab");

		private static GameObject WandPrefab => AssetDatabase.LoadAssetAtPath<GameObject>($"{WitchFolder}/Prefabs/MagicWand.prefab");

		public static bool PackPresent => WitchPrefab != null && Clip($"{LocomotionFolder}/Idle/A_Idle_Standing_Femn.fbx") != null;

		[MenuItem("Witch Craft/Art/Set Up Stellar Witch", false, 41)]
		private static void Menu()
		{
			if (!Run())
				EditorUtility.DisplayDialog("Stellar Witch", "The Stellar Witch model or Synty Base Locomotion wasn't found in Assets/ThirdParty.", "OK");
		}

		/// <summary>
		/// Sets the witch up as the player's art. False when the packs aren't on this machine.
		/// </summary>
		public static bool Run()
		{
			if (!PackPresent)
				return false;

			EnsureFolder(OutputFolder);

			var controller = BuildController(out var missing);
			var variant = BuildVariant(controller, out var headHeight, out var wand);

			var scale = TargetHeadHeight / Mathf.Max(0.01f, headHeight);

			FillArtSet(variant, scale);

			Debug.Log($"{nameof(StellarWitchSetup)}: head {headHeight:0.000} → scale {scale:0.000}, wand {(wand ? "in hand" : "missing")}" +
				(missing.Length > 0 ? $", missing clips: {missing}" : ""));

			return true;
		}

		#region Controller

		private static AnimationClip Clip(string fbxPath) =>
			AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>().FirstOrDefault(clip => !clip.name.StartsWith("__preview__"));

		private static AnimatorController BuildController(out string missing)
		{
			var lost = new System.Collections.Generic.List<string>();

			AnimationClip Load(string path)
			{
				var clip = Clip(path);

				if (clip == null)
					lost.Add(System.IO.Path.GetFileNameWithoutExtension(path));

				return clip;
			}

			AnimationClip Move(string path) => Load($"{LocomotionFolder}/{path}.fbx");
			AnimationClip Spell(string path) => Load($"{SpellFolder}/{path}.fbx");

			// Recreated from scratch each time: blend trees and layers are awkward to rebuild in place, and the
			// variant is saved again with the new controller straight after
			AssetDatabase.DeleteAsset(ControllerPath);
			var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

			foreach (var (name, type) in new[]
			{
				("Velocity", AnimatorControllerParameterType.Float),
				("VerticalVelocity", AnimatorControllerParameterType.Float),
				("IsGrounded", AnimatorControllerParameterType.Bool),
				("IsFlying", AnimatorControllerParameterType.Bool),
				("Jump", AnimatorControllerParameterType.Trigger),
				("SpellReady", AnimatorControllerParameterType.Bool),
				("Cast", AnimatorControllerParameterType.Trigger),
				("CastStyle", AnimatorControllerParameterType.Int),
			})
				controller.AddParameter(name, type);

			var grounded = controller.parameters.First(parameter => parameter.name == "IsGrounded");
			grounded.defaultBool = true;
			controller.parameters = controller.parameters.Select(parameter => parameter.name == "IsGrounded" ? grounded : parameter).ToArray();

			BuildLocomotion(controller, Move);
			BuildCasting(controller, Spell);

			EditorUtility.SetDirty(controller);
			AssetDatabase.SaveAssets();

			missing = string.Join(", ", lost);

			return controller;
		}

		// Walk, run and sprint blend on the witch's speed, at the speeds CharacterStats moves her
		private static void BuildLocomotion(AnimatorController controller, System.Func<string, AnimationClip> clip)
		{
			var machine = controller.layers[0].stateMachine;

			var locomotion = controller.CreateBlendTreeInController("Locomotion", out var tree, 0);
			tree.blendType = BlendTreeType.Simple1D;
			tree.blendParameter = "Velocity";
			tree.useAutomaticThresholds = false;

			var stats = AssetDatabase.LoadAssetAtPath<CharacterStats>("Assets/Data/CharacterStats.asset");
			var walk = stats != null ? stats.WalkSpeed : 2.5f;
			var run = stats != null ? stats.RunSpeed : 5.5f;
			var sprint = stats != null ? stats.SprintSpeed : 8.5f;

			tree.AddChild(clip("Idle/A_Idle_Standing_Femn"), 0f);
			tree.AddChild(clip("Locomotion/Walk/A_Walk_F_Femn"), walk);
			tree.AddChild(clip("Locomotion/Run/A_Run_F_Femn"), run);
			tree.AddChild(clip("Locomotion/Sprint/A_Sprint_F_Femn"), sprint);

			machine.defaultState = locomotion;

			var jump = State(machine, "Jump", clip("InAir/A_Jump_Running_Femn"), new Vector3(300, 120));
			var fall = State(machine, "Fall", clip("InAir/A_InAir_FallShort_Femn"), new Vector3(560, 120));
			var land = State(machine, "Land", clip("InAir/A_Land_IdleSoft_Femn"), new Vector3(560, 0));
			// Riding the broom: a sitting clip from Mixamo when there is one, else a crouch. Either way CharacterAnimator
			// lowers her hips onto the stick
			var broom = State(machine, "Broom", MixamoClip("Sitting Idle", "Sit") ?? clip("Idle/A_Idle_Crouching_Femn"), new Vector3(300, -120));

			// The jump is the game's, so skip the clip's crouch and start at the push off
			var takeoff = Transition(locomotion, jump, 0.08f);
			takeoff.AddCondition(AnimatorConditionMode.If, 0, "Jump");
			takeoff.offset = 0.15f;

			Transition(jump, fall, 0.2f, 0.85f);

			Transition(jump, land, 0.1f, 0.35f).AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

			// Stepping off a ledge
			var drop = Transition(locomotion, fall, 0.25f);
			drop.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
			drop.AddCondition(AnimatorConditionMode.Less, -3f, "VerticalVelocity");

			Transition(fall, land, 0.08f).AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");

			// Straight back into a run if she lands moving
			Transition(land, locomotion, 0.15f, 0.3f);

			var mount = machine.AddAnyStateTransition(broom);
			mount.duration = 0.25f;
			mount.canTransitionToSelf = false;
			mount.AddCondition(AnimatorConditionMode.If, 0, "IsFlying");

			Transition(broom, locomotion, 0.25f).AddCondition(AnimatorConditionMode.IfNot, 0, "IsFlying");
		}

		// Upper body only, so she keeps walking while she casts
		private static void BuildCasting(AnimatorController controller, System.Func<string, AnimationClip> clip)
		{
			controller.AddLayer("Casting");

			var layers = controller.layers;
			layers[1].defaultWeight = 1f;
			layers[1].avatarMask = AssetDatabase.LoadAssetAtPath<AvatarMask>($"{CastingFolder}/Models/Avatar Masks/Human Body Upper Mask.mask");
			controller.layers = layers;

			var machine = controller.layers[1].stateMachine;

			// No motion: the base layer shows through
			var empty = machine.AddState("Empty", new Vector3(300, 0));
			machine.defaultState = empty;

			var enter = State(machine, "Ready", clip("HumanF@CastingEnter01"), new Vector3(300, 120));
			var ready = State(machine, "ReadyIdle", clip("HumanF@CastingIdle01"), new Vector3(560, 120));
			var exit = State(machine, "PutAway", clip("HumanF@CastingExit01"), new Vector3(560, 0));

			Transition(empty, enter, 0.15f).AddCondition(AnimatorConditionMode.If, 0, "SpellReady");
			Transition(enter, ready, 0.15f, 0.85f);
			Transition(enter, exit, 0.15f).AddCondition(AnimatorConditionMode.IfNot, 0, "SpellReady");
			Transition(ready, exit, 0.15f).AddCondition(AnimatorConditionMode.IfNot, 0, "SpellReady");
			Transition(exit, empty, 0.2f, 0.8f);
			Transition(exit, enter, 0.15f).AddCondition(AnimatorConditionMode.If, 0, "SpellReady");

			for (var style = 0; style < CastClips.Length; style++)
			{
				var cast = State(machine, $"Cast{style}", clip(CastClips[style]), new Vector3(820, style * 80 - 80));

				var start = machine.AddAnyStateTransition(cast);
				start.duration = 0.06f;
				start.canTransitionToSelf = true;
				start.AddCondition(AnimatorConditionMode.If, 0, "Cast");
				start.AddCondition(AnimatorConditionMode.Equals, style, "CastStyle");

				Transition(cast, ready, 0.2f, 0.8f).AddCondition(AnimatorConditionMode.If, 0, "SpellReady");
				Transition(cast, empty, 0.25f, 0.8f).AddCondition(AnimatorConditionMode.IfNot, 0, "SpellReady");
			}
		}

		private const string MixamoFolder = "Assets/ThirdParty/Mixamo";

		// A clip downloaded from Mixamo into ThirdParty/Mixamo: the best name match, made Humanoid (so it retargets onto
		// her) and set to loop in place. Mixamo files arrive as Generic rigs, and their clips don't loop
		private static AnimationClip MixamoClip(params string[] names)
		{
			if (!AssetDatabase.IsValidFolder(MixamoFolder))
				return null;

			var files = AssetDatabase.FindAssets("t:Model", new[] { MixamoFolder }).Select(AssetDatabase.GUIDToAssetPath).ToList();

			var path = names
				.Select(name => files.FirstOrDefault(file => System.IO.Path.GetFileNameWithoutExtension(file).IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0))
				.FirstOrDefault(file => file != null);

			if (path == null)
				return null;

			var importer = (ModelImporter)AssetImporter.GetAtPath(path);

			if (importer.animationType != ModelImporterAnimationType.Human)
			{
				importer.animationType = ModelImporterAnimationType.Human;
				importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
				importer.SaveAndReimport();
			}

			var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;

			if (clips.Any(clip => !clip.loopTime || !clip.lockRootRotation || !clip.lockRootHeightY || !clip.lockRootPositionXZ))
			{
				foreach (var clip in clips)
				{
					clip.loopTime = true;
					clip.lockRootRotation = true;
					clip.lockRootHeightY = true;
					clip.lockRootPositionXZ = true;
					clip.keepOriginalOrientation = true;
					clip.keepOriginalPositionY = true;
					clip.keepOriginalPositionXZ = true;
				}

				importer.clipAnimations = clips;
				importer.SaveAndReimport();
			}

			return Clip(path);
		}

		private static AnimatorState State(AnimatorStateMachine machine, string name, Motion motion, Vector3 position)
		{
			var state = machine.AddState(name, position);
			state.motion = motion;
			return state;
		}

		private static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to, float duration, float exitTime = -1f)
		{
			var transition = from.AddTransition(to);
			transition.duration = duration;
			transition.hasExitTime = exitTime >= 0f;

			if (exitTime >= 0f)
				transition.exitTime = exitTime;

			return transition;
		}

		#endregion

		#region Variant

		private static GameObject BuildVariant(AnimatorController controller, out float headHeight, out bool wand)
		{
			var instance = (GameObject)PrefabUtility.InstantiatePrefab(WitchPrefab);

			try
			{
				PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
				instance.name = "StellarWitchPlayer";
				instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

				// Her hair and cloth sway with UnityChanSpringBone (com.unity.springbone). With that package installed its
				// components resolve and stay; without it they're missing scripts, dropped so she still loads cleanly
				foreach (var child in instance.GetComponentsInChildren<Transform>(true))
					GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);

				var animator = instance.GetComponentInChildren<Animator>() ?? instance.AddComponent<Animator>();
				animator.runtimeAnimatorController = controller;
				animator.applyRootMotion = false;
				animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

				var head = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
				headHeight = head != null ? head.position.y - instance.transform.position.y : 1.4f;

				wand = AttachWand(animator);

				ReplaceToonMaterials();

				return PrefabUtility.SaveAsPrefabAsset(instance, VariantPath);
			}
			finally
			{
				Object.DestroyImmediate(instance);
			}
		}

		// The wand rests in her right palm, pointing along her fingers
		private static bool AttachWand(Animator animator)
		{
			var hand = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;

			if (hand == null || WandPrefab == null)
				return false;

			var fingers = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);

			var wand = (GameObject)PrefabUtility.InstantiatePrefab(WandPrefab);
			PrefabUtility.UnpackPrefabInstance(wand, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
			wand.name = "Wand";

			foreach (var collider in wand.GetComponentsInChildren<Collider>())
				Object.DestroyImmediate(collider);

			var along = fingers != null ? (fingers.position - hand.position).normalized : hand.right;
			var grip = fingers != null ? Vector3.Lerp(hand.position, fingers.position, 0.8f) : hand.position;

			wand.transform.SetParent(hand, true);
			wand.transform.position = grip;

			// The wand model points up its local Y; lay that along the fingers, then let the hand carry it
			wand.transform.rotation = Quaternion.FromToRotation(Vector3.up, along);

			return true;
		}

		// Her materials were made for Unity's toon shader (UTS3), whose package doesn't compile on this Unity's URP and
		// isn't installed. Every material in the pack is moved onto URP Simple Lit in place, keeping its own textures
		// (and the wand's glow): nothing in the project refers to the toon shader any more, it's cheap on the web, and
		// the pack's sample scene shows her properly too
		private static void ReplaceToonMaterials()
		{
			var simpleLit = Shader.Find("Universal Render Pipeline/Simple Lit");

			var materials = AssetDatabase.FindAssets("t:Material", new[] { $"{WitchFolder}/Materials" })
				.Select(guid => AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)))
				.Where(material => material != null && material.shader != null &&
					(material.shader.name.Contains("Toon") || material.shader.name.Contains("InternalErrorShader") || !material.shader.isSupported));

			foreach (var material in materials)
			{
				// Read before the shader changes: the saved properties are the only record of the toon set-up
				var colourMap = SavedTexture(material, "_MainTex");
				var colour = SavedColour(material, "_BaseColor", Color.white);
				var glow = SavedTexture(material, "_Emissive_Tex");
				var glowColour = SavedColour(material, "_Emissive_Color", Color.white);

				material.shader = simpleLit;
				material.SetTexture("_BaseMap", colourMap);
				material.SetColor("_BaseColor", colour);
				material.SetFloat("_Smoothness", 0.1f);

				// Skirts, sleeves and hair are single sheets
				material.SetFloat("_Cull", 0f);
				material.doubleSidedGI = true;

				if (glow != null)
				{
					material.SetTexture("_EmissionMap", glow);
					material.SetColor("_EmissionColor", glowColour);
					material.EnableKeyword("_EMISSION");
					material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
				}

				EditorUtility.SetDirty(material);
			}

			// Copies from before the pack's own materials were converted
			AssetDatabase.DeleteAsset($"{OutputFolder}/Materials");

			AssetDatabase.SaveAssets();
		}

		// A material's own saved textures and colours, read from its asset rather than through its shader, which may
		// be missing (then the shader's properties, and so GetTexture, know nothing)
		private static Texture SavedTexture(Material material, string name)
		{
			var properties = new SerializedObject(material).FindProperty("m_SavedProperties.m_TexEnvs");

			for (var i = 0; properties != null && i < properties.arraySize; i++)
			{
				var entry = properties.GetArrayElementAtIndex(i);

				if (entry.FindPropertyRelative("first").stringValue == name)
					return entry.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
			}

			return null;
		}

		private static Color SavedColour(Material material, string name, Color fallback)
		{
			var properties = new SerializedObject(material).FindProperty("m_SavedProperties.m_Colors");

			for (var i = 0; properties != null && i < properties.arraySize; i++)
			{
				var entry = properties.GetArrayElementAtIndex(i);

				if (entry.FindPropertyRelative("first").stringValue == name)
					return entry.FindPropertyRelative("second").colorValue;
			}

			return fallback;
		}

		#endregion

		private static void FillArtSet(GameObject variant, float scale)
		{
			var set = LoadOrCreate<ArtSet>(ArtSetPath);

			var entry = set.Entries.FirstOrDefault(existing => existing.Key == "witch");

			if (entry == null)
			{
				entry = new ArtEntry { Key = "witch" };
				set.Entries.Add(entry);
			}

			if (entry.Prefab == null)
			{
				entry.Prefab = variant;
				entry.Scale = scale;
				entry.Rotation = Vector3.zero;
				entry.Offset = Vector3.zero;
			}

			set.Entries = set.Entries.OrderBy(existing => existing.Key).ToList();
			set.NotifyChanged();

			EditorUtility.SetDirty(set);
			AssetDatabase.SaveAssets();
		}
	}
}
