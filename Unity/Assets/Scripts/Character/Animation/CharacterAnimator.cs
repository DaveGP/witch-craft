using Art;
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

		private ICharacterMovementState movementState;

		private ISpellCasterState spells;

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
			{
				rig = animator;
				rigParameters = rig != null ? new AnimatorParameters(rig) : null;
			}

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
