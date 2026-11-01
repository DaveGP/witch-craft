using System;
using UnityEngine.InputSystem;


namespace Controls
{
	// Hand-built equivalent of an Input System generated wrapper, so the action map lives in code
	// and needs no asset import before the project compiles.
	public class PlayerControls : IDisposable
	{
		public PlayerActions Player { get; }

		private readonly InputActionMap playerMap;

		public PlayerControls()
		{
			playerMap = new InputActionMap("Player");

			Player = new PlayerActions(playerMap);
		}

		public void Dispose()
		{
			playerMap.Dispose();
		}

		public class PlayerActions
		{
			public InputAction Move { get; }
			public InputAction Look { get; }
			public InputAction Rotate { get; }
			public InputAction Scroll { get; }
			public InputAction Jump { get; }
			public InputAction Sprint { get; }
			public InputAction Descend { get; }
			public InputAction Interact { get; }
			public InputAction Cast { get; }
			public InputAction AltCast { get; }
			public InputAction Broom { get; }
			public InputAction Spell1 { get; }
			public InputAction Spell2 { get; }
			public InputAction Spell3 { get; }
			public InputAction Spell4 { get; }
			public InputAction Satchel { get; }
			public InputAction Spellbook { get; }
			public InputAction Cancel { get; }

			private readonly InputActionMap map;

			public PlayerActions(InputActionMap map)
			{
				this.map = map;

				Move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
				Move.AddCompositeBinding("2DVector")
					.With("Up", "<Keyboard>/w")
					.With("Down", "<Keyboard>/s")
					.With("Left", "<Keyboard>/a")
					.With("Right", "<Keyboard>/d");
				Move.AddCompositeBinding("2DVector")
					.With("Up", "<Keyboard>/upArrow")
					.With("Down", "<Keyboard>/downArrow")
					.With("Left", "<Keyboard>/leftArrow")
					.With("Right", "<Keyboard>/rightArrow");
				Move.AddBinding("<Gamepad>/leftStick");

				Look = map.AddAction("Look", InputActionType.Value, expectedControlLayout: "Vector2");
				Look.AddBinding("<Mouse>/delta");
				Look.AddBinding("<Gamepad>/rightStick").WithProcessor("scaleVector2(x=12,y=12)");

				Rotate = map.AddAction("Rotate", InputActionType.Value, expectedControlLayout: "Axis");
				Rotate.AddCompositeBinding("1DAxis")
					.With("Negative", "<Keyboard>/q")
					.With("Positive", "<Keyboard>/r");
				Rotate.AddCompositeBinding("1DAxis")
					.With("Negative", "<Gamepad>/leftShoulder")
					.With("Positive", "<Gamepad>/rightShoulder");

				Scroll = map.AddAction("Scroll", InputActionType.Value, expectedControlLayout: "Axis");
				Scroll.AddBinding("<Mouse>/scroll/y");
				Scroll.AddCompositeBinding("1DAxis")
					.With("Negative", "<Gamepad>/dpad/down")
					.With("Positive", "<Gamepad>/dpad/up");

				Jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
				Sprint = Button("Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
				Descend = Button("Descend", "<Keyboard>/leftCtrl", "<Keyboard>/c", "<Gamepad>/buttonEast");
				Interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonWest");
				Cast = Button("Cast", "<Mouse>/leftButton", "<Gamepad>/rightTrigger");
				AltCast = Button("AltCast", "<Mouse>/rightButton", "<Gamepad>/leftTrigger");
				Broom = Button("Broom", "<Keyboard>/f", "<Gamepad>/buttonNorth");
				Spell1 = Button("Spell1", "<Keyboard>/1", "<Gamepad>/dpad/left");
				Spell2 = Button("Spell2", "<Keyboard>/2", "<Gamepad>/dpad/right");
				Spell3 = Button("Spell3", "<Keyboard>/3");
				Spell4 = Button("Spell4", "<Keyboard>/4");
				Satchel = Button("Satchel", "<Keyboard>/tab", "<Gamepad>/select");
				Spellbook = Button("Spellbook", "<Keyboard>/b");
				Cancel = Button("Cancel", "<Keyboard>/escape", "<Gamepad>/start");
			}

			public void Enable() => map.Enable();

			public void Disable() => map.Disable();

			private InputAction Button(string name, params string[] bindings)
			{
				var action = map.AddAction(name, InputActionType.Button);

				foreach (var binding in bindings)
					action.AddBinding(binding);

				return action;
			}
		}
	}
}
