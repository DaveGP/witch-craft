using Character.Movement.Interfaces;
using Data.Enums;
using UnityEngine;


namespace Character.Animation
{
	// Procedural animation for the placeholder witch: squash and stretch on jumps and landings,
	// a running bob and lean, and a seated pose on the broom. Swap for an Animator once a rigged model lands.
	public class CharacterAnimator : MonoBehaviour
	{
		[SerializeField] private Transform body;

		[SerializeField] private Transform hat;

		private ICharacterMovementState movementState;

		private Vector3 restScale;

		private Vector3 restPosition;

		private float squash;

		private float squashVelocity;

		private float bobPhase;


		private void Awake()
		{
			movementState = GetComponentInParent<PlayerCharacter>();

			if (body == null)
				Debug.LogError($"{nameof(body)} is null");

			if (movementState == null)
				Debug.LogError($"{nameof(movementState)} is null");

			restScale = body.localScale;
			restPosition = body.localPosition;
		}

		private void Start()
		{
			movementState.Jumped += () => squash = -0.25f;
			movementState.Landed += () => squash = 0.3f;
		}

		private void LateUpdate()
		{
			UpdateSquash();

			UpdateBody();
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
