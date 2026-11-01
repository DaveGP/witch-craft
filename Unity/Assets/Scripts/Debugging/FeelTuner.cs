using System.Globalization;
using System.Text;
using Camera;
using Character.Movement;
using Spells.Effects;
using Data.Enums;
using DependencyInjection;
using GameState.Interfaces;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Debugging
{
	// Live sliders for how the witch moves, jumps and flies, and how the camera behaves. Toggle with ` (backquote).
	// In the editor the changes save into the CharacterStats and BroomStats assets; in a build they last the session.
	// "Copy values" puts the current numbers on the clipboard to paste into the assets or a message.
	public class FeelTuner : MonoBehaviour
	{
		private const float Width = 380f;

		private IInputModeService inputModeService;

		private MovementController movement;

		private BroomController broom;

		private BubbleSpell bubble;

		private CameraController cameraController;

		private bool open;

		private Vector2 scroll;

		private GUIStyle heading;

		private void Awake()
		{
			inputModeService = ServiceLocator.Get<IInputModeService>();
		}

		private void Update()
		{
			if (Keyboard.current == null || !Keyboard.current.backquoteKey.wasPressedThisFrame)
				return;

			open = !open;

			if (open)
			{
				movement = FindAnyObjectByType<MovementController>();
				broom = FindAnyObjectByType<BroomController>();
				bubble = FindAnyObjectByType<BubbleSpell>();
				cameraController = FindAnyObjectByType<CameraController>();

				inputModeService.Push(InputMode.Menu);
			}
			else
			{
				inputModeService.Pop();
			}
		}

		private void OnGUI()
		{
			if (!open || movement == null)
				return;

			heading ??= new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };

			var scale = Screen.height / 900f;
			GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

			GUILayout.BeginArea(new Rect(16, 16, Width, 868), GUI.skin.box);
			GUILayout.Label("Feel tuning  ( ` to close )", heading);

			scroll = GUILayout.BeginScrollView(scroll);

			var stats = movement.Stats;

			GUILayout.Label("Running", heading);
			stats.WalkSpeed = Slider("Walk speed", stats.WalkSpeed, 1f, 6f);
			stats.RunSpeed = Slider("Run speed", stats.RunSpeed, 2f, 10f);
			stats.SprintSpeed = Slider("Sprint speed", stats.SprintSpeed, 4f, 15f);
			stats.Acceleration = Slider("Ground acceleration", stats.Acceleration, 10f, 150f);
			stats.TurnSpeed = Slider("Turn speed (deg/s)", stats.TurnSpeed, 180f, 2000f);

			GUILayout.Label("Jumping", heading);
			stats.JumpHeight = Slider("Jump height", stats.JumpHeight, 0.5f, 4f);
			stats.AirJumpHeight = Slider("Air jump height", stats.AirJumpHeight, 0f, 3f);
			stats.AirJumps = Mathf.RoundToInt(Slider("Air jumps", stats.AirJumps, 0f, 3f));
			stats.AirAcceleration = Slider("Air control", stats.AirAcceleration, 5f, 100f);
			stats.CoyoteTime = Slider("Coyote time", stats.CoyoteTime, 0f, 0.3f);
			stats.JumpBufferTime = Slider("Jump buffer", stats.JumpBufferTime, 0f, 0.3f);
			stats.Gravity = Slider("Gravity", stats.Gravity, 10f, 60f);
			stats.FallGravityMultiplier = Slider("Fall gravity x", stats.FallGravityMultiplier, 1f, 3f);
			stats.LowJumpGravityMultiplier = Slider("Short hop gravity x", stats.LowJumpGravityMultiplier, 1f, 4f);
			stats.MaxFallSpeed = Slider("Max fall speed", stats.MaxFallSpeed, 10f, 60f);

			if (broom != null)
			{
				var broomStats = broom.Stats;

				GUILayout.Label("Broom", heading);
				broomStats.Speed = Slider("Speed", broomStats.Speed, 4f, 25f);
				broomStats.BoostSpeed = Slider("Boost speed", broomStats.BoostSpeed, 6f, 35f);
				broomStats.VerticalSpeed = Slider("Climb speed", broomStats.VerticalSpeed, 1f, 12f);
				broomStats.Acceleration = Slider("Acceleration", broomStats.Acceleration, 3f, 40f);
				broomStats.MaxCharge = Slider("Flight time (s)", broomStats.MaxCharge, 2f, 30f);
				broomStats.RechargeRate = Slider("Recharge per second", broomStats.RechargeRate, 0.5f, 10f);
			}

			if (bubble != null)
			{
				GUILayout.Label("Spells", heading);
				bubble.Duration = Slider("Bubble duration (s)", bubble.Duration, 0.5f, 10f);
				bubble.RiseSpeed = Slider("Bubble rise speed", bubble.RiseSpeed, 0.5f, 10f);
				GUILayout.Label($"  Bubble rises about {bubble.Duration * bubble.RiseSpeed:0.0} m");
			}

			if (cameraController != null)
			{
				GUILayout.Label("Camera", heading);
				cameraController.Sensitivity = Slider("Mouse sensitivity", cameraController.Sensitivity, 0.02f, 0.5f);
				cameraController.Distance = Slider("Distance", cameraController.Distance, 2.5f, 12f);
			}

			GUILayout.EndScrollView();

			if (GUILayout.Button("Copy values"))
				GUIUtility.systemCopyBuffer = Describe();

			GUILayout.EndArea();
		}

		private static float Slider(string label, float value, float min, float max)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label(label, GUILayout.Width(170));
			value = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(130));
			GUILayout.Label(value.ToString("0.00", CultureInfo.InvariantCulture), GUILayout.Width(50));
			GUILayout.EndHorizontal();

			return value;
		}

		private string Describe()
		{
			var stats = movement.Stats;

			var text = new StringBuilder();
			text.AppendLine("CharacterStats");
			text.AppendLine($"  WalkSpeed {stats.WalkSpeed:0.##}, RunSpeed {stats.RunSpeed:0.##}, SprintSpeed {stats.SprintSpeed:0.##}, Acceleration {stats.Acceleration:0.##}, TurnSpeed {stats.TurnSpeed:0}");
			text.AppendLine($"  JumpHeight {stats.JumpHeight:0.##}, AirJumpHeight {stats.AirJumpHeight:0.##}, AirJumps {stats.AirJumps}, AirAcceleration {stats.AirAcceleration:0.##}");
			text.AppendLine($"  CoyoteTime {stats.CoyoteTime:0.###}, JumpBufferTime {stats.JumpBufferTime:0.###}, Gravity {stats.Gravity:0.##}, FallGravityMultiplier {stats.FallGravityMultiplier:0.##}, LowJumpGravityMultiplier {stats.LowJumpGravityMultiplier:0.##}, MaxFallSpeed {stats.MaxFallSpeed:0.##}");

			if (broom != null)
			{
				var broomStats = broom.Stats;
				text.AppendLine("BroomStats");
				text.AppendLine($"  Speed {broomStats.Speed:0.##}, BoostSpeed {broomStats.BoostSpeed:0.##}, VerticalSpeed {broomStats.VerticalSpeed:0.##}, Acceleration {broomStats.Acceleration:0.##}, MaxCharge {broomStats.MaxCharge:0.##}, RechargeRate {broomStats.RechargeRate:0.##}");
			}

			if (bubble != null)
				text.AppendLine($"Bubble\n  Duration {bubble.Duration:0.##}, RiseSpeed {bubble.RiseSpeed:0.##}");

			if (cameraController != null)
				text.AppendLine($"Camera\n  Sensitivity {cameraController.Sensitivity:0.###}, Distance {cameraController.Distance:0.##}");

			return text.ToString();
		}
	}
}
