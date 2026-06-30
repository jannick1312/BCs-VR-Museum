using Godot;
namespace BCSVRMuseum.Keyboard;

public partial class KeyboardUi : Control
{
	private enum KeyboardMode
	{
		LowerCase,
		UpperCase,
		Alternate
	}

	private bool _shiftDown;
	private bool _capsDown;
	private bool _altDown;

	private KeyboardMode _mode = KeyboardMode.LowerCase;

	private Button _toggleShift;
	private Button _toggleCaps;
	private Button _toggleAlt;

	private Control _lowerCase;
	private Control _upperCase;
	private Control _alternate;

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

	private void OnVirtualKeyPressed(string scanCodeText, int unicode, bool shift)
	{
		SendKey(scanCodeText, unicode, shift);

		if (!_shiftDown)
			return;
		_shiftDown = false;
		UpdateVisible(false);
	}

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

	private void OnToggleShiftPressed()
	{
		_shiftDown = !_shiftDown;
		_capsDown = false;
		_altDown = false;

		UpdateVisible(false);
	}

	private void OnToggleCapsPressed()
	{
		_capsDown = !_capsDown;
		_shiftDown = false;
		_altDown = false;

		UpdateVisible(false);
	}

	private void OnToggleAltPressed()
	{
		_altDown = !_altDown;
		_shiftDown = false;
		_capsDown = false;

		UpdateVisible(false);
	}

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

	private static void SetToggleVisual(Button button, bool active)
	{
		button.ButtonPressed = active;
	}
}