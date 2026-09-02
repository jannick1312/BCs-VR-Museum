using Godot;

namespace BCSVRMuseum.Keyboard;

/// <summary>
/// Controls the virtual keyboard layout and special keys.
/// </summary>
public partial class KeyboardUi : Control
{
	private bool _altDown;
	private Control _alternate;
	private bool _capsDown;
	private Control _lowerCase;
	private KeyboardMode _mode = KeyboardMode.LowerCase;
	private bool _shiftDown;
	private Button _toggleAlt;
	private Button _toggleCaps;
	private Button _toggleShift;
	private Control _upperCase;

	/// <summary>
	/// Finds the keyboard controls and connects key events.
	/// </summary>
	public override void _Ready()
	{
		_toggleShift = GetNode<Button>("Panel/Standard/ToggleShift");
		_toggleCaps = GetNode<Button>("Panel/Standard/ToggleCaps");
		_toggleAlt = GetNode<Button>("Panel/Standard/ToggleAlt");

		_lowerCase = GetNode<Control>("Panel/LowerCase");
		_upperCase = GetNode<Control>("Panel/UpperCase");
		_alternate = GetNode<Control>("Panel/Alternate");

		_toggleShift.FocusMode = FocusModeEnum.None;
		_toggleShift.Pressed += OnToggleShiftPressed;

		_toggleCaps.FocusMode = FocusModeEnum.None;
		_toggleCaps.Pressed += OnToggleCapsPressed;

		_toggleAlt.FocusMode = FocusModeEnum.None;
		_toggleAlt.Pressed += OnToggleAltPressed;

		SetupAllKeys(this);
		UpdateVisible(true);
	}

	/// <summary>
	/// Connects all virtual keys below a node.
	/// </summary>
	/// <param name="root">The root node to scan.</param>
	private void SetupAllKeys(Node root)
	{
		foreach (var child in root.GetChildren())
		{
			if (child is VirtualKeyInputEvent key)
			{
				key.FocusMode = FocusModeEnum.None;
				key.KeyPressed += OnVirtualKeyPressed;
			}

			SetupAllKeys(child);
		}
	}

	/// <summary>
	/// Sends a virtual key press and clears a one-time Shift modifier.
	/// </summary>
	/// <param name="scanCodeText">The key name used to find the scan code.</param>
	/// <param name="unicode">The Unicode value produced by the key.</param>
	/// <param name="shift">If the emitted key event includes Shift.</param>
	private void OnVirtualKeyPressed(string scanCodeText, int unicode, bool shift)
	{
		SendKey(scanCodeText, unicode, shift);

		if (!_shiftDown)
			return;
		_shiftDown = false;
		UpdateVisible(false);
	}

	/// <summary>
	/// Emits a pressed key event through Godot's input system.
	/// </summary>
	/// <param name="scanCodeText">The key name used to find the scan code.</param>
	/// <param name="unicode">The Unicode value produced by the key.</param>
	/// <param name="shift">If Shift is pressed.</param>
	private static void SendKey(string scanCodeText, int unicode, bool shift)
	{
		var scanCode = Key.None;

		if (!string.IsNullOrEmpty(scanCodeText))
			scanCode = OS.FindKeycodeFromString(scanCodeText);

		var input = new InputEventKey
		{
			PhysicalKeycode = scanCode,
			Keycode = scanCode,
			Unicode = unicode != 0 ? unicode : (int)scanCode,
			Pressed = true,
			ShiftPressed = shift
		};

		Input.ParseInputEvent(input);
	}

	/// <summary>
	/// Toggles the Shift layout and clears the other modifiers.
	/// </summary>
	private void OnToggleShiftPressed()
	{
		_shiftDown = !_shiftDown;
		_capsDown = false;
		_altDown = false;

		UpdateVisible(false);
	}

	/// <summary>
	/// Toggles the Caps Lock layout and clears the other modifiers.
	/// </summary>
	private void OnToggleCapsPressed()
	{
		_capsDown = !_capsDown;
		_shiftDown = false;
		_altDown = false;

		UpdateVisible(false);
	}

	/// <summary>
	/// Toggles the alternate layout and clears the other modifiers.
	/// </summary>
	private void OnToggleAltPressed()
	{
		_altDown = !_altDown;
		_shiftDown = false;
		_capsDown = false;

		UpdateVisible(false);
	}

	/// <summary>
	/// Shows the keyboard layout for the active special keys.
	/// </summary>
	/// <param name="force">If the layout should refresh even when its mode is unchanged.</param>
	private void UpdateVisible(bool force)
	{
		SetToggleVisual(_toggleShift, _shiftDown);
		SetToggleVisual(_toggleCaps, _capsDown);
		SetToggleVisual(_toggleAlt, _altDown);

		KeyboardMode newMode;

		if (_altDown)
			newMode = KeyboardMode.Alternate;
		else if (_shiftDown || _capsDown)
			newMode = KeyboardMode.UpperCase;
		else
			newMode = KeyboardMode.LowerCase;

		if (!force && newMode == _mode)
			return;

		_mode = newMode;

		_lowerCase.Visible = _mode == KeyboardMode.LowerCase;

		_upperCase.Visible = _mode == KeyboardMode.UpperCase;

		_alternate.Visible = _mode == KeyboardMode.Alternate;
	}

	/// <summary>
	/// Updates the pressed state of a modifier button.
	/// </summary>
	/// <param name="button">The special key button to update.</param>
	/// <param name="active">If the special key is active.</param>
	private static void SetToggleVisual(Button button, bool active)
	{
		button.ButtonPressed = active;
	}

	/// <summary>
	/// Lists the virtual keyboard layouts.
	/// </summary>
	private enum KeyboardMode
	{
		LowerCase,
		UpperCase,
		Alternate
	}
}



// This keyboard is based on the keyboard from the Godot XR Tools add-on. Codex helped adapt it to C# as a starting point for this project.
