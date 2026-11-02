using System.Collections.Generic;
using System.Linq;
using Art;
using Audio;
using Audio.Interfaces;
using Character.Interfaces;
using Data.ScriptableObjects;
using DependencyInjection;
using Events.Interfaces;
using Home;
using Home.Interfaces;
using Interaction.Interfaces;
using Sky.Interfaces;
using UnityEngine;
using Utilities;


namespace Companions
{
	// The witch's familiar. Trots or bounds along at her heel and hops after her up and down ledges. When the
	// witch stops she sits, grooms now and then, loafs, lies down and dozes off, sooner at night; at night near
	// home she curls up in her Cat Bed. She rides the broom loafing on its seat and pops back to the witch with a
	// puff of magic if left far behind. Pets get a roll and a purr, too many get told off, and every creature
	// she hasn't met yet gets a hiss.
	// A rigged cat from the Art Set (with a CatRigProfile) is crossfaded by state name; without art the
	// placeholder shapes are animated by hand.
	public class CatCompanion : MonoBehaviour, IInteractable
	{
		private enum Mode
		{
			Follow,
			Hop,
			Ride,
			ToBed,
			Bed,
		}

		private enum Stage
		{
			None,
			Sit,
			Loaf,
			LieDown,
			Sleep,
		}

		[SerializeField] private string catName = "Sable";

		[SerializeField] private Transform body;

		[SerializeField] private Transform tail;

		[SerializeField] private Transform head;

		// Matches the witch's ordinary pace, so she trots alongside rather than falling behind and bounding
		[SerializeField] private float walkSpeed = 6f;

		[SerializeField] private float runSpeed = 9f;

		// The fastest the walk clip plays: a quick trot. Faster than that her paws slide a little rather than blur
		private const float MaxWalkRate = 6f;

		[SerializeField] private float followDistance = 1.8f;

		[SerializeField] private float teleportDistance = 18f;

		[Header("Resting")]
		[SerializeField] private float sitAfter = 1.2f;

		[SerializeField] private float loafAfter = 20f;

		[SerializeField] private float lieDownAfter = 32f;

		[SerializeField] private float sleepAfter = 42f;

		[Tooltip("Rest timings are multiplied by this at night, so she nods off sooner")]
		[SerializeField] private float nightRestFactor = 0.4f;

		[Header("Hopping")]
		[Tooltip("Height change that needs a hop rather than a step")]
		[SerializeField] private float stepHeight = 0.4f;

		[SerializeField] private float hopReach = 6f;

		[SerializeField] private float hopRise = 3.5f;

		[Header("Moods")]
		[SerializeField] private int petsBeforeGrumpy = 4;

		[SerializeField] private float petWindow = 10f;

		[SerializeField] private float hissRange = 5f;

		[Tooltip("How near home the witch must be at night for Sable to go to her bed")]
		[SerializeField] private float bedRange = 12f;

		// Each creature is hissed at once a session
		private static readonly HashSet<CompanionDefinition> hissedAt = new();

		private static readonly int SpeedParameter = Animator.StringToHash("Speed");

		private static readonly int SittingParameter = Animator.StringToHash("IsSitting");

		private IEventBus eventBus;

		private IEventBusSender eventSender;

		private IAudioService audioService;

		private IDayCycle dayCycle;

		private IHomeService home;

		private IObserverTarget witch;

		private LayerMask groundMask;

		private Mode mode;

		private Stage stage;

		private CatRigProfile.Side side = CatRigProfile.Side.Left;

		private Vector3 velocity;

		private float idleTime;

		private float stepPhase;

		private float purrUntil;

		private Vector3 bodyRestScale;

		private readonly Queue<float> pets = new();

		// A one-shot reaction (a groom, a roll, a hiss) that plays over whatever she's doing
		private CatRigProfile.Role? action;

		private string actionState;

		private float actionUntil;

		private string stageState;

		private float stageStateUntil;

		private float nextFidget;

		private Vector3 hopFrom;

		private Vector3 hopTo;

		private float hopHeight;

		private float hopDuration;

		private float hopTime;

		private float hopCooldown;

		private float stuckTime;

		private Vector3 heelForward;

		// Arrived at the witch's heel; she won't fuss over every small shift until it's a real step away
		private bool settled;

		private bool hopToBed;

		private PlacedPiece bed;

		private float bedTripTime;

		private CompanionRecruit[] recruits;

		private float nextRecruitCheck;

		private ArtSwap artSwap;

		private GameObject rigArt;

		private Animator rig;

		private CatRigProfile profile;

		private Transform rigRoot;

		private AnimatorParameters rigParameters;

		private string rigState;

		private int walkSpeedParameter;

		private int jumpSpeedParameter;

		public string Prompt => IsAsleep ? $"Pet {catName} (asleep)" : $"Pet {catName}";

		public bool CanInteract => mode != Mode.Ride && mode != Mode.Hop;

		// She's always close by, so anything else in reach comes first
		public bool InBackground => true;

		public Vector3 Position => transform.position;

		public bool IsAsleep => stage == Stage.Sleep && mode is Mode.Follow or Mode.Bed;

		public bool IsHopping => mode == Mode.Hop;

		public bool IsInBed => mode == Mode.Bed;

		/// <summary>
		/// What she's doing, for the playtest and debugging: Walk, Sit, Groom, Sleep, Hop, Bed...
		/// </summary>
		public string Activity => action?.ToString() ?? mode switch
		{
			Mode.Hop => "Hop",
			Mode.Ride => "Ride",
			Mode.ToBed => "ToBed",
			_ => stage != Stage.None ? stage.ToString() : velocity.sqrMagnitude > 0.05f ? "Walk" : "Idle",
		};

		/// <summary>
		/// The Animator state last crossfaded to, when she has a rig.
		/// </summary>
		public string RigState => rig != null ? rigState : null;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetSession() => hissedAt.Clear();

		private void Awake()
		{
			eventBus = ServiceLocator.Get<IEventBus>();
			eventSender = ServiceLocator.Get<IEventBusSender>();

			if (eventBus == null)
				Debug.LogError($"{nameof(eventBus)} is null");

			groundMask = LayerMask.GetMask("Ground", "Environment");

			artSwap = GetComponent<ArtSwap>();

			if (body != null)
				bodyRestScale = body.localScale;

			eventBus.ActiveTargetChanged += SetWitch;
		}

		private void OnDestroy()
		{
			eventBus.ActiveTargetChanged -= SetWitch;
		}

		private void SetWitch(IObserverTarget target)
		{
			witch = target;
		}

		#region Petting

		public void Interact(GameObject interactor)
		{
			audioService ??= ServiceLocator.Get<IAudioService>();

			if (IsAsleep)
			{
				Act(CatRigProfile.Role.SleepBlink, 2.5f);
				audioService?.PlayAt(Sound.Purr, transform.position, 0.4f);
				eventSender.Notify($"{catName} opens one eye, blinks slowly at you and goes back to sleep.");
				return;
			}

			while (pets.Count > 0 && pets.Peek() < Time.time - petWindow)
				pets.Dequeue();

			pets.Enqueue(Time.time);

			if (pets.Count >= petsBeforeGrumpy)
			{
				pets.Clear();
				Act(CatRigProfile.Role.ThreatShort, 2.5f);
				audioService?.PlayAt(Sound.Hiss, transform.position, 0.5f);
				eventSender.Notify($"{catName} flattens her ears and swats at your hand. Enough!");
				return;
			}

			Act(CatRigProfile.Role.RollOver, 3.5f);
			purrUntil = Time.time + 3f;

			audioService?.PlayAt(Sound.Purr, transform.position, 0.9f);

			FxUtility.Burst(transform.position + Vector3.up * 0.6f, new Color(1f, 0.5f, 0.7f), 12, 1.2f, 0.2f);

			eventSender.Notify(Random.value < 0.5f ? $"{catName} rolls over and purrs." : $"{catName} rolls over for belly rubs. It's a trap.");
		}

		// Starts a one-shot reaction, lasting one play of its clip (or a placeholder's guess)
		private void Act(CatRigProfile.Role role, float placeholderLength)
		{
			if (stage == Stage.None)
				side = RandomSide();

			var state = profile != null ? profile.Pick(role, side) : null;

			action = role;
			actionState = state?.Name;
			actionUntil = Time.time + (state != null ? Mathf.Min(state.Length, 5f) : placeholderLength);
			velocity = Vector3.zero;
		}

		private bool Acting => action != null && Time.time < actionUntil;

		#endregion

		private void Update()
		{
			if (witch == null)
				return;

			dayCycle ??= ServiceLocator.Get<IDayCycle>();
			home ??= ServiceLocator.Get<IHomeService>();

			BindRig();

			if (action != null && Time.time >= actionUntil)
				action = null;

			if (witch.Broom.IsMounted && witch.BroomSeat != null)
				Ride();
			else if (mode == Mode.Ride)
				Dismount();

			switch (mode)
			{
				case Mode.Hop:
					UpdateHop();
					break;
				case Mode.ToBed:
					UpdateToBed();
					break;
				case Mode.Bed:
					UpdateBed();
					break;
				case Mode.Follow:
					Follow();
					break;
			}

			CheckRecruits();

			Animate();
		}

		#region Following

		private void Follow()
		{
			var witchPosition = witch.Transform.position;

			var toWitch = witchPosition - transform.position;
			toWitch.y = 0f;

			var distance = toWitch.magnitude;

			if (distance > teleportDistance || Mathf.Abs(witchPosition.y - transform.position.y) > hopReach && witch.Movement.IsGrounded)
			{
				Teleport(witchPosition - witch.Transform.forward * followDistance);
				return;
			}

			if (ShouldGoToBed())
			{
				mode = Mode.ToBed;
				bedTripTime = 0f;
				EndRest();
				return;
			}

			// Aim for a spot just behind and beside the witch rather than her feet. Which way is "behind" only
			// changes while she walks, so turning on the spot doesn't send the cat circling round her
			if (Flat(witch.Movement.Velocity).sqrMagnitude > 0.25f || heelForward == Vector3.zero)
				heelForward = Flat(witch.Transform.forward).normalized;

			var heelRight = Vector3.Cross(Vector3.up, heelForward);
			var heel = witchPosition - heelForward * followDistance + heelRight * 0.7f;

			var toHeel = heel - transform.position;
			toHeel.y = 0f;

			// Once settled she only gets up when the witch walks off, not when she turns on the spot
			if (toHeel.magnitude > 1.5f)
				settled = false;

			var wantsToMove = stage == Stage.None && !settled
				? toHeel.magnitude > 0.4f && distance > followDistance * 0.6f
				: distance > followDistance + 2f;

			if (!wantsToMove && velocity.sqrMagnitude < 0.05f)
				settled = true;

			// A grumpy swat or a hiss plays out; anything else is dropped to keep up
			if (wantsToMove && Acting && action is not (CatRigProfile.Role.ThreatShort or CatRigProfile.Role.ThreatLong) && distance > followDistance + 3f)
				action = null;

			var desired = Vector3.zero;

			if (wantsToMove && !Acting)
			{
				var speed = toHeel.magnitude > 4f ? runSpeed : walkSpeed;
				desired = toHeel.normalized * Mathf.Min(speed, toHeel.magnitude * 3f);
			}

			velocity = Vector3.MoveTowards(velocity, desired, 25f * Time.deltaTime);

			if (velocity.sqrMagnitude > 0.05f)
				EndRest();

			hopCooldown -= Time.deltaTime;

			var next = transform.position + velocity * Time.deltaTime;

			if (velocity.sqrMagnitude > 0.01f)
			{
				var grounded = Physics.Raycast(next + Vector3.up * 1.5f, Vector3.down, out var hit, 4f, groundMask, QueryTriggerInteraction.Ignore);

				// A ledge up, a drop or a gap: hop after the witch if there's somewhere to land, else wait
				if (!grounded || Mathf.Abs(hit.point.y - transform.position.y) > stepHeight)
				{
					if (hopCooldown <= 0f && witch.Movement.IsGrounded)
					{
						// Her heel if it's in reach, else a little way towards it
						if (Ground(heel, out var landing) && TryHop(landing))
							return;

						if (Ground(transform.position + velocity.normalized * 1.5f, out landing) && TryHop(landing))
							return;
					}

					velocity = Vector3.zero;

					// Nowhere to hop to (stranded on a roof, say): magic her back to the witch
					stuckTime += Time.deltaTime;

					if (stuckTime > 2.5f && witch.Movement.IsGrounded)
					{
						stuckTime = 0f;
						Teleport(heel);
						return;
					}
				}
				else
				{
					stuckTime = 0f;

					transform.position = new Vector3(next.x, Mathf.Lerp(transform.position.y, hit.point.y, 1f - Mathf.Exp(-20f * Time.deltaTime)), next.z);
				}
			}

			var look = velocity.sqrMagnitude > 0.05f ? velocity : toWitch;

			if (look.sqrMagnitude > 0.01f && !Acting && stage < Stage.LieDown)
				Face(look, 8f);

			// Waiting at a ledge she can't hop isn't resting
			if (!wantsToMove && velocity.sqrMagnitude < 0.05f)
				Rest();
			else
				idleTime = 0f;
		}

		private void Teleport(Vector3 position)
		{
			FxUtility.Burst(transform.position + Vector3.up * 0.3f, new Color(0.7f, 0.6f, 1f), 16, 2f);

			transform.position = position;

			SnapToGround();

			velocity = Vector3.zero;
			mode = Mode.Follow;
			EndRest();

			FxUtility.Burst(transform.position + Vector3.up * 0.3f, new Color(0.7f, 0.6f, 1f), 16, 2f);
		}

		private void SnapToGround()
		{
			if (Ground(transform.position, out var point))
				transform.position = point;
		}

		private bool Ground(Vector3 position, out Vector3 point)
		{
			var found = Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out var hit, 10f, groundMask, QueryTriggerInteraction.Ignore);
			point = found ? hit.point : position;
			return found;
		}

		private void Face(Vector3 direction, float sharpness)
		{
			direction.y = 0f;

			if (direction.sqrMagnitude > 0.0001f)
				transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction.normalized, Vector3.up), 1f - Mathf.Exp(-sharpness * Time.deltaTime));
		}

		#endregion

		#region Resting

		private float RestFactor => dayCycle != null && dayCycle.IsNight ? nightRestFactor : 1f;

		private void Rest()
		{
			idleTime += Time.deltaTime;

			var factor = RestFactor;

			var next = idleTime > sleepAfter * factor ? Stage.Sleep
				: idleTime > lieDownAfter * factor ? Stage.LieDown
				: idleTime > loafAfter * factor ? Stage.Loaf
				: idleTime > sitAfter ? Stage.Sit
				: Stage.None;

			SetStage(next);

			// Sitting is when she finds time to wash or have a scratch
			if (stage == Stage.Sit && !Acting && Time.time > nextFidget)
			{
				nextFidget = Time.time + Random.Range(5f, 9f);

				var roll = Random.value;

				if (roll < 0.4f)
					Act(CatRigProfile.Role.Groom, 3f);
				else if (roll < 0.6f)
					Act(CatRigProfile.Role.Scratch, 2f);
			}

			if (stage == Stage.Sleep && Time.time > stageStateUntil)
				PickSleep();
		}

		private void SetStage(Stage next)
		{
			if (next == stage)
				return;

			// A rest keeps one side throughout, so sitting, loafing and sleeping all lean the same way
			if (stage == Stage.None)
			{
				side = RandomSide();
				nextFidget = Time.time + Random.Range(3f, 6f);
			}

			// Washing and scratching belong to sitting up
			if (next > Stage.Sit && action is CatRigProfile.Role.Groom or CatRigProfile.Role.Scratch)
				action = null;

			stage = next;

			stageState = stage switch
			{
				Stage.Sit => PickName(CatRigProfile.Role.Sit),
				Stage.Loaf => PickName(CatRigProfile.Role.Loaf),
				Stage.LieDown => PickName(CatRigProfile.Role.LieDown),
				_ => null,
			};

			if (stage == Stage.Sleep)
				PickSleep();
		}

		// Sleep moves between the side's variants (long sleep, twitchy paws, a tail flick...)
		private void PickSleep()
		{
			var state = profile != null ? profile.Pick(CatRigProfile.Role.Sleep, side) : null;

			for (var tries = 0; tries < 3 && state != null && state.Name == stageState; tries++)
				state = profile.Pick(CatRigProfile.Role.Sleep, side);

			stageState = state?.Name;

			// Short loops play a few times so she isn't fidgeting every couple of seconds
			var length = state != null ? state.Length : 10f;
			stageStateUntil = Time.time + length * Mathf.Max(1, Mathf.CeilToInt(8f / Mathf.Max(0.1f, length)));
		}

		private string PickName(CatRigProfile.Role role) => profile != null ? profile.Pick(role, side)?.Name : null;

		private void EndRest()
		{
			idleTime = 0f;
			stage = Stage.None;
			stageState = null;

			if (action is CatRigProfile.Role.Groom or CatRigProfile.Role.Scratch or CatRigProfile.Role.SleepBlink)
				action = null;
		}

		private static CatRigProfile.Side RandomSide() => Random.value < 0.5f ? CatRigProfile.Side.Left : CatRigProfile.Side.Right;

		#endregion

		#region Hopping

		// Hops to a spot within reach, true when it's started
		private bool TryHop(Vector3 target)
		{
			var flat = target - transform.position;
			var rise = flat.y;
			flat.y = 0f;

			if (flat.magnitude > hopReach || rise > hopRise || rise < -hopReach)
				return false;

			StartHop(target);
			return true;
		}

		private void StartHop(Vector3 target)
		{
			hopFrom = transform.position;
			hopTo = target;
			hopTime = 0f;

			var flat = target - hopFrom;
			var rise = flat.y;
			flat.y = 0f;

			hopDuration = Mathf.Clamp(0.35f + flat.magnitude * 0.07f + Mathf.Abs(rise) * 0.06f, 0.4f, 0.9f);

			// The arc clears the higher end; the jump clip lifts the paws a little itself
			hopHeight = Mathf.Max(rise, 0f) + 0.35f;

			if (profile != null)
				hopHeight = Mathf.Max(Mathf.Max(rise, 0f) + 0.05f, hopHeight - profile.JumpLift * ArtScale);

			mode = Mode.Hop;
			velocity = Vector3.zero;
			action = null;
			EndRest();

			if (flat.sqrMagnitude > 0.01f)
				transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);

			PlayJump(hopDuration);
		}

		private void UpdateHop()
		{
			hopTime += Time.deltaTime / hopDuration;

			var t = Mathf.Clamp01(hopTime);

			transform.position = Vector3.Lerp(hopFrom, hopTo, t) + Vector3.up * (4f * hopHeight * t * (1f - t));

			if (t < 1f)
				return;

			mode = Mode.Follow;
			hopCooldown = 0.4f;

			if (hopToBed)
			{
				hopToBed = false;
				EnterBed();
			}

			FxUtility.Burst(transform.position + Vector3.up * 0.05f, new Color(0.85f, 0.8f, 0.7f), 6, 0.8f, 0.12f);
		}

		#endregion

		#region Broom

		private void Ride()
		{
			if (mode != Mode.Ride)
			{
				mode = Mode.Ride;
				action = null;
				EndRest();
				FxUtility.Burst(transform.position, new Color(0.7f, 0.6f, 1f), 10, 1.5f);
			}

			transform.SetParent(witch.BroomSeat, false);
			transform.localPosition = Vector3.zero;
			transform.localRotation = Quaternion.identity;
		}

		// Off the seat with a hop down beside the witch
		private void Dismount()
		{
			transform.SetParent(null, true);
			transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

			mode = Mode.Follow;

			var beside = witch.Transform.position - witch.Transform.forward * followDistance + witch.Transform.right * 0.7f;

			if (Ground(beside, out var landing) || Ground(witch.Transform.position, out landing))
				StartHop(landing);
			else
				Teleport(witch.Transform.position);
		}

		#endregion

		#region Cat bed

		private bool ShouldGoToBed()
		{
			if (dayCycle == null || !dayCycle.IsNight || home == null)
				return false;

			bed = home.Pieces.FirstOrDefault(piece => piece != null && piece.Definition != null && piece.Definition.Id == "cat_bed");

			return bed != null && Flat(witch.Transform.position - bed.transform.position).magnitude < bedRange;
		}

		private void UpdateToBed()
		{
			if (bed == null || !ShouldGoToBed())
			{
				mode = Mode.Follow;
				return;
			}

			bedTripTime += Time.deltaTime;

			var toBed = Flat(bed.transform.position - transform.position);

			if (toBed.magnitude < 0.6f)
			{
				// Up onto the cushion
				StartHop(BedSurface());
				hopToBed = true;
				return;
			}

			// Something in the way for too long: magic her there
			if (bedTripTime > 10f)
			{
				FxUtility.Burst(transform.position + Vector3.up * 0.3f, new Color(0.7f, 0.6f, 1f), 16, 2f);
				transform.position = BedSurface();
				EnterBed();
				return;
			}

			velocity = Vector3.MoveTowards(velocity, toBed.normalized * Mathf.Min(walkSpeed, toBed.magnitude * 3f), 25f * Time.deltaTime);

			var next = transform.position + velocity * Time.deltaTime;

			if (Physics.Raycast(next + Vector3.up * 1.5f, Vector3.down, out var hit, 4f, groundMask, QueryTriggerInteraction.Ignore)
				&& Mathf.Abs(hit.point.y - transform.position.y) <= stepHeight)
				transform.position = new Vector3(next.x, Mathf.Lerp(transform.position.y, hit.point.y, 1f - Mathf.Exp(-20f * Time.deltaTime)), next.z);
			else
				velocity = Vector3.zero;

			Face(velocity, 8f);
		}

		private void EnterBed()
		{
			mode = Mode.Bed;
			velocity = Vector3.zero;
			stage = Stage.None;
			idleTime = 0f;
			SetStage(Stage.LieDown);
		}

		private void UpdateBed()
		{
			if (bed == null || !ShouldGoToBed())
			{
				LeaveBed();
				return;
			}

			idleTime += Time.deltaTime;

			if (idleTime > 2.5f)
				SetStage(Stage.Sleep);

			if (stage == Stage.Sleep && Time.time > stageStateUntil)
				PickSleep();
		}

		private void LeaveBed()
		{
			mode = Mode.Follow;

			var away = Flat(witch.Transform.position - transform.position);
			var spot = transform.position + (away.sqrMagnitude > 0.01f ? away.normalized : transform.forward) * 0.8f;

			if (Ground(spot, out var landing))
				StartHop(landing);
		}

		// Where she lies: the top of the bed's art when it has some, else of its collider
		private Vector3 BedSurface()
		{
			var centre = bed.transform.position;
			var swap = bed.GetComponentInChildren<ArtSwap>();

			if (swap != null && swap.Art != null)
			{
				var renderers = swap.Art.GetComponentsInChildren<Renderer>();

				if (renderers.Length > 0)
				{
					var bounds = renderers[0].bounds;

					foreach (var renderer in renderers.Skip(1))
						bounds.Encapsulate(renderer.bounds);

					// Cushions dip in the middle
					return new Vector3(centre.x, Mathf.Lerp(bounds.min.y, bounds.max.y, 0.6f), centre.z);
				}
			}

			var top = centre.y;
			var ray = new Ray(centre + Vector3.up * 3f, Vector3.down);

			foreach (var collider in bed.GetComponentsInChildren<Collider>())
				if (!collider.isTrigger && collider.Raycast(ray, out var hit, 5f))
					top = Mathf.Max(top, hit.point.y);

			return new Vector3(centre.x, top, centre.z);
		}

		private static Vector3 Flat(Vector3 value) => new(value.x, 0f, value.z);

		#endregion

		#region Strangers

		// A hiss for every creature she hasn't met yet, once a session
		private void CheckRecruits()
		{
			if (Time.time < nextRecruitCheck || mode != Mode.Follow || Acting)
				return;

			nextRecruitCheck = Time.time + 0.5f;

			recruits ??= FindObjectsByType<CompanionRecruit>(FindObjectsInactive.Include, FindObjectsSortMode.None);

			foreach (var recruit in recruits)
			{
				if (recruit == null || !recruit.isActiveAndEnabled || recruit.Companion == null || hissedAt.Contains(recruit.Companion))
					continue;

				if (Flat(recruit.transform.position - transform.position).magnitude > hissRange)
					continue;

				hissedAt.Add(recruit.Companion);

				EndRest();

				var toRecruit = Flat(recruit.transform.position - transform.position);

				if (toRecruit.sqrMagnitude > 0.01f)
					transform.rotation = Quaternion.LookRotation(toRecruit.normalized, Vector3.up);

				Act(CatRigProfile.Role.ThreatLong, 3f);

				audioService ??= ServiceLocator.Get<IAudioService>();
				audioService?.PlayAt(Sound.Hiss, transform.position, 0.8f);

				eventSender.Notify($"{catName} puffs up and hisses at {recruit.Companion.DisplayName}. She'll come round.");
				return;
			}
		}

		#endregion

		#region Animation

		private float ArtScale => rigArt != null ? rigArt.transform.lossyScale.x : 1f;

		private void BindRig()
		{
			var art = artSwap != null ? artSwap.Art : null;

			if (art == rigArt)
				return;

			rigArt = art;
			rig = art != null ? art.GetComponentInChildren<Animator>() : null;
			profile = art != null ? art.GetComponentInChildren<CatRigProfile>() : null;
			rigParameters = rig != null ? new AnimatorParameters(rig) : null;
			rigState = null;
			rigRoot = null;

			if (profile == null)
				return;

			walkSpeedParameter = Animator.StringToHash(profile.WalkSpeedParameter);
			jumpSpeedParameter = Animator.StringToHash(profile.JumpSpeedParameter);
			rigRoot = art.GetComponentsInChildren<Transform>(true).FirstOrDefault(bone => bone.name == profile.RootBone);
		}

		private void Animate()
		{
			if (profile != null && rig != null)
				AnimateRig();
			else
				AnimatePlaceholder();
		}

		private void AnimateRig()
		{
			if (mode == Mode.Hop)
				return;

			if (Acting && actionState != null)
			{
				Play(actionState, 0.2f);
				return;
			}

			if (mode == Mode.Ride)
			{
				Play(profile.Pick(CatRigProfile.Role.Loaf, CatRigProfile.Side.Centre)?.Name, 0.3f);
				return;
			}

			var speed = velocity.magnitude;

			if (speed > 0.1f)
			{
				// Walks, matched to the ground at a stroll and sped up to a trot to keep pace with the witch. Only a
				// sprint to catch up bounds: the leap held at a steady speed reads as a glide
				var walk = profile.WalkSpeed * ArtScale;
				var rate = speed / Mathf.Max(0.01f, walk);

				if (speed <= walkSpeed * 1.25f || !profile.Has(CatRigProfile.Role.Jump))
				{
					rigParameters.SetFloat(walkSpeedParameter, Mathf.Clamp(rate, 0.5f, MaxWalkRate));
					Play(profile.Pick(CatRigProfile.Role.Walk, CatRigProfile.Side.Centre)?.Name, 0.2f);
				}
				else
				{
					Bound(speed);
				}

				return;
			}

			var resting = stageState ?? (stage != Stage.None ? PickName(stage == Stage.Sit ? CatRigProfile.Role.Sit : CatRigProfile.Role.Loaf) : null);

			Play(resting ?? profile.Pick(CatRigProfile.Role.Idle, CatRigProfile.Side.Centre)?.Name, 0.4f);
		}

		// Running is a string of leaps: the jump clip's airborne stretch, looped and sped up to match
		private void Bound(float speed)
		{
			var jump = profile.Pick(CatRigProfile.Role.Jump, CatRigProfile.Side.Centre);
			var leapTime = (profile.JumpLanding - profile.JumpTakeoff) * jump.Length;
			var leapSpeed = profile.JumpDistance * ArtScale / Mathf.Max(0.01f, leapTime);

			rigParameters.SetFloat(jumpSpeedParameter, Mathf.Clamp(speed / Mathf.Max(0.01f, leapSpeed), 0.8f, 3f));

			var info = rig.GetCurrentAnimatorStateInfo(0);
			var inLeap = rigState == jump.Name && info.IsName(jump.Name) && !rig.IsInTransition(0) && info.normalizedTime % 1f < profile.JumpLanding;

			if (rigState != jump.Name || (!inLeap && !rig.IsInTransition(0)))
			{
				rig.CrossFadeInFixedTime(jump.Name, 0.1f, 0, profile.JumpTakeoff * jump.Length);
				rigState = jump.Name;
			}
		}

		// Plays the jump so its airborne stretch spans the hop
		private void PlayJump(float duration)
		{
			if (profile == null || rig == null)
				return;

			var jump = profile.Pick(CatRigProfile.Role.Jump, CatRigProfile.Side.Centre);

			if (jump == null)
				return;

			var airborne = (profile.JumpLanding - profile.JumpTakeoff) * jump.Length;

			rigParameters.SetFloat(jumpSpeedParameter, Mathf.Clamp(airborne / duration, 0.5f, 3f));
			rig.CrossFadeInFixedTime(jump.Name, 0.06f, 0, profile.JumpTakeoff * jump.Length);
			rigState = jump.Name;
		}

		private void Play(string state, float fade)
		{
			if (string.IsNullOrEmpty(state) || state == rigState)
				return;

			rig.CrossFadeInFixedTime(state, fade);
			rigState = state;
		}

		private void LateUpdate()
		{
			// Clips that travel (the leap moves the whole skeleton forward) stay on the cat, who moves herself
			if (rigRoot == null || rigArt == null)
				return;

			var local = rigArt.transform.InverseTransformPoint(rigRoot.position);
			rigRoot.position = rigArt.transform.TransformPoint(new Vector3(0f, local.y, 0f));
		}

		private void AnimatePlaceholder()
		{
			// A rig without a profile gets the simple parameters, if its controller has them
			rigParameters?.SetFloat(SpeedParameter, velocity.magnitude);
			rigParameters?.SetBool(SittingParameter, stage != Stage.None || mode is Mode.Ride or Mode.Bed);

			if (body == null)
				return;

			var moving = velocity.magnitude;

			stepPhase += Time.deltaTime * moving * 3.5f;

			var bounce = moving > 0.1f ? Mathf.Abs(Mathf.Sin(stepPhase)) * 0.06f : 0f;
			var purr = Time.time < purrUntil ? Mathf.Sin(Time.time * 40f) * 0.01f : 0f;

			var lying = stage >= Stage.Loaf || mode is Mode.Ride or Mode.Bed;
			var sitting = stage == Stage.Sit || action is CatRigProfile.Role.Groom or CatRigProfile.Role.Scratch;

			var shape = bodyRestScale;

			if (mode == Mode.Hop)
				shape = new Vector3(bodyRestScale.x * 0.9f, bodyRestScale.y * 0.9f, bodyRestScale.z * 1.2f);
			else if (Acting && action is CatRigProfile.Role.ThreatShort or CatRigProfile.Role.ThreatLong)
				shape = bodyRestScale * 1.2f;
			else if (lying)
				shape = new Vector3(bodyRestScale.x * 1.1f, bodyRestScale.y * 0.75f, bodyRestScale.z);
			else if (sitting)
				shape = new Vector3(bodyRestScale.x, bodyRestScale.y * 1.15f, bodyRestScale.z * 0.75f);

			body.localPosition = new Vector3(0f, bodyRestScale.y * 0.5f + bounce + purr, 0f);
			body.localScale = Vector3.Lerp(body.localScale, shape, 1f - Mathf.Exp(-6f * Time.deltaTime));

			var roll = Acting && action == CatRigProfile.Role.RollOver ? Mathf.Sin(Time.time * 4f) * 40f : 0f;
			body.localRotation = Quaternion.Euler(0f, 0f, roll);

			if (head != null)
			{
				var nod = IsAsleep ? 25f : Mathf.Sin(Time.time * 0.7f) * 8f;
				head.localRotation = Quaternion.Euler(nod, IsAsleep ? 0f : Mathf.Sin(Time.time * 0.4f) * 20f, 0f);
			}

			WagTail(IsAsleep ? 0.5f : moving > 0.1f ? 8f : 2f);
		}

		private void WagTail(float speed)
		{
			if (tail != null)
				tail.localRotation = Quaternion.Euler(-35f, Mathf.Sin(Time.time * speed) * 30f, 0f);
		}

		#endregion
	}
}
