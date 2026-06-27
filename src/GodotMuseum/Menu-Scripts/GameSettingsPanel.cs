using Godot;

namespace BCSVRMuseum.Menu_Scripts;

public partial class GameSettingsPanel : Node
{
	private HSlider _movementSlider;
	private HSlider _turnSlider;
	private CheckBox _destructionCheckBox;
	private CheckBox _jumpCheckBox;
	private Button _imagesAnd3DButton;
	private Button _imagesButton;
	private Button _objects3DButton;
	private Button _defaultButton;
	private Node _movementDirect;
	private Node _movementTurn;
	private GameSettingsStore _gameSettingsStore;

	private const double DefaultSliderValue = 5.0;
	private const float MinMovementSpeed = 0.8f;
	private const float MaxMovementSpeed = 4.0f;
	private const float MinTurnSpeed = 0.8f;
	private const float MaxTurnSpeed = 4.0f;

	public override void _Ready()
	{
		var root = GetParent();

		_movementSlider = root.FindChild("Movement-Slider", true, false) as HSlider;
		_turnSlider = root.FindChild("Turn-Slider", true, false) as HSlider;
		_destructionCheckBox = root.FindChild("Destruction-Check", true, false) as CheckBox;
		_jumpCheckBox = root.FindChild("Jump-Check", true, false) as CheckBox;
		_imagesAnd3DButton = root.FindChild("Images3D", true, false) as Button;
		_imagesButton = root.FindChild("Images", true, false) as Button;
		_objects3DButton = root.FindChild("3D", true, false) as Button;
		_defaultButton = root.FindChild("Default", true, false) as Button;
		_movementDirect = GetTree().Root.FindChild("MovementDirect", true, false);
		_movementTurn = GetTree().Root.FindChild("MovementTurn", true, false);
		_gameSettingsStore = GetTree().Root.FindChild("GameSettingsStore", true, false) as GameSettingsStore;

		if (_movementSlider != null && _movementDirect != null)
		{
			_movementSlider.ValueChanged += OnMovementSliderChanged;
			OnMovementSliderChanged(_movementSlider.Value);
		}

		if (_turnSlider != null && _movementTurn != null)
		{
			_turnSlider.ValueChanged += OnTurnSliderChanged;
			OnTurnSliderChanged(_turnSlider.Value);
		}

		_destructionCheckBox?.Toggled += OnDestructionToggled;
		_jumpCheckBox?.Toggled += OnJumpToggled;
		_imagesAnd3DButton?.Pressed += CycleMode;
		_imagesButton?.Pressed += CycleMode;
		_objects3DButton?.Pressed += CycleMode;
		_defaultButton?.Pressed += ResetToDefaults;
		_gameSettingsStore?.Initialize(_destructionCheckBox?.ButtonPressed ?? false, _jumpCheckBox?.ButtonPressed ?? false, _gameSettingsStore.MediaMode);

		UpdateModeButtons();
	}

	private void OnMovementSliderChanged(double value)
	{
		_movementDirect.Set("max_speed", MapSliderValue(value, MinMovementSpeed, MaxMovementSpeed));
	}

	private void OnTurnSliderChanged(double value)
	{
		_movementTurn.Set("smooth_turn_speed", MapSliderValue(value, MinTurnSpeed, MaxTurnSpeed));
	}

	private void OnDestructionToggled(bool toggledOn)
	{
		_gameSettingsStore?.SetDestructionEnabled(toggledOn);
	}

	private void OnJumpToggled(bool toggledOn)
	{
		_gameSettingsStore?.SetJumpVignetteEnabled(toggledOn);
	}

	private void CycleMode()
	{
		_gameSettingsStore?.CycleMediaMode();
		UpdateModeButtons();
	}

	private void ResetToDefaults()
	{
		_movementSlider?.Value = DefaultSliderValue;

		_turnSlider?.Value = DefaultSliderValue;

		_destructionCheckBox?.ButtonPressed = false;

		_jumpCheckBox?.ButtonPressed = false;

		_gameSettingsStore?.SetMediaMode(GameMediaMode.ImagesAnd3D);
		UpdateModeButtons();
	}

	private void UpdateModeButtons()
	{
		var currentMode = _gameSettingsStore?.MediaMode ?? GameMediaMode.ImagesAnd3D;

		SetModeButtonVisible(_imagesAnd3DButton, currentMode == GameMediaMode.ImagesAnd3D);
		SetModeButtonVisible(_imagesButton, currentMode == GameMediaMode.Images);
		SetModeButtonVisible(_objects3DButton, currentMode == GameMediaMode.Objects3D);
	}

	private static float MapSliderValue(double value, float minOutput, float maxOutput)
	{
		var normalized = Mathf.InverseLerp(1.0f, 10.0f, (float)value);
		return Mathf.Lerp(minOutput, maxOutput, normalized);
	}

	private static void SetModeButtonVisible(Button button, bool visible)
	{
		button.Visible = visible;
		button.ProcessMode = visible ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
	}
}