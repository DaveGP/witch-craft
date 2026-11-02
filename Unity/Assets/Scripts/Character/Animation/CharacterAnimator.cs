using Art;
using Audio.Interfaces;
using DependencyInjection;
using Character.Movement.Interfaces;
using Data.Enums;
using Data.ScriptableObjects;
using Spells.Interfaces;
using UnityEngine;
using Utilities;


namespace Character.Animation
{
	// Procedural animation for the placeholder witch: squash and stretch on jumps and landings,
	// a running bob and lean, and a seated pose on the broom. When the Art Set gives the witch a rigged model,
	// its Animator is driven too, using Crewfall's locomotion parameter names, plus SpellReady while a spell is
	// readied and a Cast trigger with a CastStyle for each spell cast.
	public class CharacterAnimator : MonoBehaviour
	{
		[SerializeField] private Transform body;

		[SerializeField] private Transform hat;

		[SerializeField] private Transform broom;

		private ICharacterMovementState movementState;

		private ISpellCasterState spells;

		private IAudioService audioService;

		/// <summary>
		/// A rigged witch's foot came down, for footstep sounds (and the playtest).
		/// </summary>
		public event System.Action FootPlanted;

		private Foot leftFoot;

		private Foot rightFoot;

		private Transform hips;

		private Transform art;

		private Vector3 artRestPosition;

		private float seatOffset;

		private float lastStep;

		private Vector3 restScale;

		private Vector3 restPosition;

		private float squash;

		private float squashVelocity;

		private float bobPhase;

		private ArtSwap artSwap;

		private Animator rig;

		private AnimatorParameters rigParameters;


		private void Awake()
		{
			movementState = GetComponentInParent<PlayerCharacter>();

			if (body == null)
				Debug.LogError($"{nameof(body)} is null");

			if (movementState == null)
				Debug.LogError($"{nameof(movementState)} is null");

			restScale = body.localScale;
			restPosition = body.localPosition;

			artSwap = GetComponent<ArtSwap>();

			spells = GetComponentInParent<ISpellCasterState>();
		}

		private void Start()
		{
			movementState.Jumped += () =>
			{
				squash = -0.25f;
				rigParameters?.SetTrigger(CharacterAnimationParameters.Jump);
			};
			movementState.Landed += () => squash = 0.3f;

			if (spells != null)
				spells.SpellCast += Cast;
		}

		private void OnDestroy()
		{
			if (spells != null)
				spells.SpellCast -= Cast;

			if (audioService != null)
				audioService.FootstepsFromAnimation = false;
		}

		private void Cast(SpellDefinition spell)
		{
			if (rigParameters == null || spell == null)
				return;

			rigParameters.SetInteger(CharacterAnimationParameters.CastStyle, (int)CastStyleFor(spell.Effect));
			rigParameters.SetTrigger(CharacterAnimationParameters.Cast);
		}

		/// <summary>
		/// The gesture each spell is cast with, matching the casting layer's CastStyle states.
		/// </summary>
		public static CastStyle CastStyleFor(SpellEffectType effect) => effect switch
		{
			SpellEffectType.Gust or SpellEffectType.Blink => CastStyle.Push,
			SpellEffectType.Bubble or SpellEffectType.RainCall => CastStyle.Call,
			SpellEffectType.Bloom => CastStyle.Sweep,
			_ => CastStyle.Point
		};

		private void LateUpdate()
		{
			UpdateSquash();

			UpdateBody();

			UpdateRig();
		}

		private void UpdateRig()
		{
			var animator = artSwap != null ? artSwap.ArtAnimator : null;

			if (animator != rig)
				BindRig(animator);

			// The audio service starts after the witch does, so it's found when first needed
			audioService ??= ServiceLocator.Get<IAudioService>();

			if (audioService != null)
				audioService.FootstepsFromAnimation = rigParameters != null && leftFoot != null;

			if (rigParameters == null)
				return;

			var local = movementState.LocalVelocity;
			var flying = movementState.Mode == MovementMode.Broom;

			rigParameters.SetFloat(CharacterAnimationParameters.ForwardVelocity, local.z);
			rigParameters.SetFloat(CharacterAnimationParameters.StrafeVelocity, local.x);
			rigParameters.SetFloat(CharacterAnimationParameters.Velocity, new Vector2(local.x, local.z).magnitude);
			rigParameters.SetFloat(CharacterAnimationParameters.VerticalVelocity, movementState.Velocity.y);
			rigParameters.SetBool(CharacterAnimationParameters.Grounded, movementState.IsGrounded || flying);
			rigParameters.SetBool(CharacterAnimationParameters.Flying, flying);

			// Crewfall's controller reads Gait 4 as crouching, so flying shows as idle unless IsFlying is wired up
			rigParameters.SetInteger(CharacterAnimationParameters.Gait, flying ? 0 : (int)movementState.Gait);
			rigParameters.SetInteger(CharacterAnimationParameters.MovementDirection, (int)MovementDirectionUtility.FromLocalVelocity(local));

			rigParameters.SetBool(CharacterAnimationParameters.SpellReady, spells?.ActiveSpell != null && !flying);

			UpdateFootsteps(flying, new Vector2(local.x, local.z).magnitude);
			UpdateSeat(flying);
		}

		private void BindRig(Animator animator)
		{
			if (art != null)
				art.localPosition = artRestPosition;

			rig = animator;
			rigParameters = rig != null ? new AnimatorParameters(rig) : null;

			var human = rig != null && rig.isHuman;

			leftFoot = human ? new Foot(rig.GetBoneTransform(HumanBodyBones.LeftFoot)) : null;
			rightFoot = human ? new Foot(rig.GetBoneTransform(HumanBodyBones.RightFoot)) : null;
			hips = human ? rig.GetBoneTransform(HumanBodyBones.Hips) : null;

			art = artSwap != null && artSwap.Art != null ? artSwap.Art.transform : null;
			artRestPosition = art != null ? art.localPosition : Vector3.zero;
			seatOffset = 0f;
		}

		// A step sounds as each foot comes down: lifted clear of where it rests, then back down to it
		private void UpdateFootsteps(bool flying, float speed)
		{
			if (leftFoot == null || rightFoot == null)
				return;

			var walking = movementState.IsGrounded && !flying && speed > 0.5f;
			var ground = transform.position.y;

			if (leftFoot.Planted(ground, walking) | rightFoot.Planted(ground, walking) && Time.time - lastStep > 0.12f)
			{
				lastStep = Time.time;
				audioService?.Footstep();
				FootPlanted?.Invoke();
			}
		}

		// On the broom she sits on the stick: her hips are brought down (or up) to it, whatever pose the clip has
		private void UpdateSeat(bool flying)
		{
			if (art == null || hips == null || broom == null)
				return;

			var target = flying ? broom.position.y + 0.1f - hips.position.y : -seatOffset;

			seatOffset += target * (1f - Mathf.Exp(-12f * Time.deltaTime));

			if (!flying && Mathf.Abs(seatOffset) < 0.001f)
				seatOffset = 0f;

			art.localPosition = artRestPosition + Vector3.up * seatOffset;
		}

		// One foot's rise and fall, measured from the lowest it has been lately
		private class Foot
		{
			private const float Lift = 0.06f;

			private const float Down = 0.02f;

			private readonly Transform bone;

			private float rest = float.MaxValue;

			private bool lifted;

			public Foot(Transform bone)
			{
				this.bone = bone;
			}

			public bool Planted(float ground, bool walking)
			{
				if (bone == null)
					return false;

				var height = bone.position.y - ground;

				// The resting height creeps up slowly and snaps down, so it follows slopes and steps
				rest = Mathf.Min(rest + 0.1f * Time.deltaTime, height);

				if (!walking)
				{
					lifted = false;
					return false;
				}

				if (height > rest + Lift)
					lifted = true;

				if (!lifted || height > rest + Down)
					return false;

				lifted = false;
				return true;
			}
		}

		private void UpdateSquash()
		{
			// Critically damped spring back to rest
			squash = Mathf.SmoothDamp(squash, 0f, ref squashVelocity, 0.08f);
		}

		private void UpdateBody()
		{
			var local = movementState.LocalVelocity;

			var horizontalSpeed = new Vector2(local.x, local.z).magnitude;

			var flying = movementState.Mode == MovementMode.Broom;

			var bob = 0f;

			if (movementState.IsGrounded && !flying && horizontalSpeed > 0.2f)
			{
				bobPhase += Time.deltaTime * Mathf.Lerp(8f, 16f, horizontalSpeed / 8f);
				bob = Mathf.Abs(Mathf.Sin(bobPhase)) * 0.08f;
			}

			var stretch = !movementState.IsGrounded && !flying ? Mathf.Clamp(Mathf.Abs(movementState.Velocity.y) * 0.01f, 0f, 0.12f) : 0f;

			var vertical = 1f - squash + stretch;
			var horizontal = 1f + squash * 0.5f - stretch * 0.5f;

			body.localScale = new Vector3(restScale.x * horizontal, restScale.y * vertical, restScale.z * horizontal);

			var seatDrop = flying ? -0.25f : 0f;

			body.localPosition = restPosition + Vector3.up * (bob + seatDrop);

			var lean = flying ? 0f : Mathf.Clamp(local.z * 1.5f, -6f, 12f);

			body.localRotation = Quaternion.Euler(lean, 0f, 0f);

			if (hat != null)
				hat.localRotation = Quaternion.Euler(-lean * 0.8f + Mathf.Sin(Time.time * 3f) * 3f, 0f, Mathf.Sin(Time.time * 2.1f) * 4f);
		}
	}
}
