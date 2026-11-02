using UnityEngine;
using Camera.Interfaces;
using Character.Input.Interfaces;
using Character.Interfaces;
using DependencyInjection;
using Events.Interfaces;
using LocalPlayer.Interfaces;
using Settings.Interfaces;


namespace Camera
{
	// Third person orbit rig: the root follows the target, the yaw and pitch pivots orbit and the camera sits on a
	// boom that is pulled in when it would clip into the world. A station can take over with a fixed viewpoint.
	public class CameraController : MonoBehaviour, ICameraController
	{
		public Vector3 Forward => Vector3.ProjectOnPlane(yawPivot.forward, Vector3.up).normalized;

		public Vector3 Right => Vector3.ProjectOnPlane(yawPivot.right, Vector3.up).normalized;

		public Vector3 LookDirection => pitchPivot.forward;

		public Ray AimRay => new(cameraTransform.position, cameraTransform.forward);

		public float Sensitivity
		{
			get => settings.LookSensitivity;
			set => settings.LookSensitivity = value;
		}

		public float Distance
		{
			get => distance;
			set => distance = Mathf.Clamp(value, minDistance, maxDistance);
		}

		private ICameraInputSource inputSource;

		private IEventBus eventBus;

		private IPlayerContext playerContext;

		private IGameSettings settings;

		private ICameraTarget cameraTarget;

		private IObserverTarget observed;

		[SerializeField] private Transform pitchPivot;

		[SerializeField] private Transform yawPivot;

		[SerializeField] private Transform cameraTransform;

		[SerializeField] private float distance = 5.1f;

		[SerializeField] private float minDistance = 2.1f;

		[SerializeField] private float maxDistance = 10.5f;

		[SerializeField] private float followSharpness = 14f;

		[Tooltip("How quickly the camera follows height changes in the air. Low keeps jumps from bobbing the view")]
		[SerializeField] private float airborneHeightSharpness = 2.5f;

		[Tooltip("The furthest the witch can rise or fall before the camera catches up regardless")]
		[SerializeField] private float maxHeightLag = 2.5f;

		[SerializeField] private float collisionRadius = 0.25f;

		[SerializeField] private LayerMask collisionMask;

		private float pitch = 15f;

		private float yaw;

		private float boomLength;

		private Transform overrideViewpoint;

		private float overrideBlend;

		private Vector3 lastViewpointPosition;

		private Quaternion lastViewpointRotation = Quaternion.identity;

		private UnityEngine.Camera viewCamera;

		private int playerLayerMask;


		private void Awake()
		{
			inputSource = ServiceLocator.Get<ICameraInputSource>();

			eventBus = ServiceLocator.Get<IEventBus>();

			playerContext = ServiceLocator.Get<IPlayerContext>();

			settings = ServiceLocator.Get<IGameSettings>();

			if (inputSource == null)
				Debug.LogError($"{nameof(inputSource)} is null");

			if (eventBus == null)
				Debug.LogError($"{nameof(eventBus)} is null");

			if (playerContext == null)
				Debug.LogError($"{nameof(playerContext)} is null");

			ServiceLocator.Register<ICameraController>(this);

			boomLength = distance;

			viewCamera = cameraTransform.GetComponent<UnityEngine.Camera>();

			playerLayerMask = LayerMask.GetMask("Player");

			AddEvents();
		}

		private void OnDestroy()
		{
			ServiceLocator.Unregister<ICameraController>(this);

			eventBus.ActiveTargetChanged -= SetCameraTarget;
			eventBus.CameraOverrideChanged -= SetOverride;
		}

		private void AddEvents()
		{
			eventBus.ActiveTargetChanged += SetCameraTarget;
			eventBus.CameraOverrideChanged += SetOverride;
		}

		private void SetCameraTarget(IObserverTarget target)
		{
			cameraTarget = target.Camera;

			observed = target;

			yaw = target.Transform.eulerAngles.y;

			transform.position = cameraTarget.Position;
		}

		private void SetOverride(Transform viewpoint)
		{
			overrideViewpoint = viewpoint;
		}

		private void LateUpdate()
		{
			if (cameraTarget == null)
				return;

			UpdatePosition();

			UpdateRotation();

			UpdateZoom();

			UpdateBoom();
		}

		private void UpdatePosition()
		{
			var target = cameraTarget.Position;

			var current = transform.position;

			var blend = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);

			// Follow height lazily mid-jump so the view stays steady, but snappily on the ground and on the broom
			var airborne = observed != null && !observed.Movement.IsGrounded && !observed.Broom.IsMounted;

			var heightBlend = airborne ? 1f - Mathf.Exp(-airborneHeightSharpness * Time.deltaTime) : blend;

			var height = Mathf.Lerp(current.y, target.y, heightBlend);
			height = Mathf.Clamp(height, target.y - maxHeightLag, target.y + maxHeightLag);

			var flat = Vector3.Lerp(new Vector3(current.x, 0f, current.z), new Vector3(target.x, 0f, target.z), blend);

			transform.position = new Vector3(flat.x, height, flat.z);
		}

		private void UpdateRotation()
		{
			var cameraInput = inputSource.GetCameraInput();

			var sensitivity = settings.LookSensitivity;

			yaw += cameraInput.Look.x * sensitivity;

			pitch -= cameraInput.Look.y * sensitivity * (settings.InvertY ? -1f : 1f);

			pitch = Mathf.Clamp(pitch, -35f, 75f);

			pitchPivot.localRotation =
				Quaternion.Euler(pitch, 0, 0);

			yawPivot.localRotation =
				Quaternion.Euler(0, yaw, 0);
		}

		private void UpdateZoom()
		{
			if (playerContext.ScrollCaptured)
				return;

			var zoom = inputSource.GetCameraInput().Zoom;

			if (Mathf.Abs(zoom) > 0.01f)
				distance = Mathf.Clamp(distance - Mathf.Sign(zoom) * 0.75f, minDistance, maxDistance);
		}

		private void UpdateBoom()
		{
			var origin = pitchPivot.position;

			var back = -pitchPivot.forward;

			var target = distance;

			if (Physics.SphereCast(origin, collisionRadius, back, out var hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
				target = Mathf.Max(0.6f, hit.distance);

			// Pull in instantly so the camera never clips, ease back out
			boomLength = target < boomLength ? target : Mathf.Lerp(boomLength, target, 1f - Mathf.Exp(-4f * Time.deltaTime));

			var orbitPosition = origin + back * boomLength;

			// Squeezed against a wall the camera would sit inside the witch, so stop drawing her
			if (viewCamera != null)
				viewCamera.cullingMask = boomLength < 1.4f ? viewCamera.cullingMask & ~playerLayerMask : viewCamera.cullingMask | playerLayerMask;

			var orbitRotation = pitchPivot.rotation;

			overrideBlend = Mathf.MoveTowards(overrideBlend, overrideViewpoint != null ? 1f : 0f, Time.deltaTime * 2.5f);

			if (overrideBlend <= 0f)
			{
				cameraTransform.SetPositionAndRotation(orbitPosition, orbitRotation);
				return;
			}

			// Remember where the last viewpoint was, so blending back out still works if it has been destroyed
			if (overrideViewpoint != null)
			{
				lastViewpointPosition = overrideViewpoint.position;
				lastViewpointRotation = overrideViewpoint.rotation;
			}

			var eased = Mathf.SmoothStep(0f, 1f, overrideBlend);

			cameraTransform.SetPositionAndRotation(
				Vector3.Lerp(orbitPosition, lastViewpointPosition, eased),
				Quaternion.Slerp(orbitRotation, lastViewpointRotation, eased));
		}
	}
}
