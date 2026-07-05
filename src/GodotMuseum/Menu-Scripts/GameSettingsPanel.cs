using Godot;

namespace BCSVRMuseum.Menu_Scripts;

public partial class GameSettingsPanel : Node
{
	private HSlider _movementSlider;
	private HSlider _turnSlider;
	private HSlider _skinColourSlider;
	private CheckBox _destructionCheckBox;
	private Button _imagesAnd3DButton;
	private Button _imagesButton;
	private Button _objects3DButton;
	private Button _defaultButton;
	private Node _movementDirect;
	private Node _movementTurn;
	private Node _leftHandTrackingMesh;
	private Node _rightHandTrackingMesh;
	private GameSettingsStore _gameSettingsStore;

	private const double DefaultSliderValue = 5.0;
	private const double DefaultSkinColourValue = 5.0;
	private const float MinMovementSpeed = 0.8f;
	private const float MaxMovementSpeed = 4.0f;
	private const float MinTurnSpeed = 0.8f;
	private const float MaxTurnSpeed = 4.0f;
	private static readonly Color LightSkinColour = new(0.92f, 0.68f, 0.52f, 1.0f);
	private static readonly Color DarkSkinColour = new(0.24f, 0.13f, 0.08f, 1.0f);

	public override void _Ready()
	{
		var root = GetParent();

		_movementSlider = (HSlider)root.FindChild("Movement-Slider", true, false);
		_turnSlider = (HSlider)root.FindChild("Turn-Slider", true, false);
		_skinColourSlider = (HSlider)root.FindChild("SkinColour-Slider", true, false);
		_destructionCheckBox = (CheckBox)root.FindChild("Destruction-Check", true, false);
		_imagesAnd3DButton = (Button)root.FindChild("Images3D", true, false);
		_imagesButton = (Button)root.FindChild("Images", true, false);
		_objects3DButton = (Button)root.FindChild("3D", true, false);
		_defaultButton = (Button)root.FindChild("Default", true, false);
		_movementDirect = GetTree().Root.FindChild("MovementDirect", true, false);
		_movementTurn = GetTree().Root.FindChild("MovementTurn", true, false);
		_leftHandTrackingMesh = GetTree().Root.FindChild("LeftHandTrackingMesh", true, false);
		_rightHandTrackingMesh = GetTree().Root.FindChild("RightHandTrackingMesh", true, false);
		_gameSettingsStore = (GameSettingsStore)GetTree().Root.FindChild("GameSettingsStore", true, false);

		_movementSlider.ValueChanged += OnMovementSliderChanged;
		OnMovementSliderChanged(_movementSlider.Value);

		_turnSlider.ValueChanged += OnTurnSliderChanged;
		OnTurnSliderChanged(_turnSlider.Value);

		_skinColourSlider.ValueChanged += OnSkinColourSliderChanged;
		OnSkinColourSliderChanged(_skinColourSlider.Value);

		_destructionCheckBox.Toggled += OnDestructionToggled;
		_imagesAnd3DButton.Pressed += CycleMode;
		_imagesButton.Pressed += CycleMode;
		_objects3DButton.Pressed += CycleMode;
		_defaultButton.Pressed += ResetToDefaults;
		_gameSettingsStore.Initialize(_destructionCheckBox.ButtonPressed, _gameSettingsStore.MediaMode);

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
		_gameSettingsStore.SetDestructionEnabled(toggledOn);
	}

	private void OnSkinColourSliderChanged(double value)
	{
		var skinColour = MapSkinColour(value);

		ApplyHandMaterialColour(_leftHandTrackingMesh, skinColour);
		ApplyHandMaterialColour(_rightHandTrackingMesh, skinColour);
	}

	private void CycleMode()
	{
		_gameSettingsStore.CycleMediaMode();
		UpdateModeButtons();
	}

	private void ResetToDefaults()
	{
		_movementSlider.Value = DefaultSliderValue;
		_turnSlider.Value = DefaultSliderValue;
		_skinColourSlider.Value = DefaultSkinColourValue;
		_destructionCheckBox.ButtonPressed = false;
		_gameSettingsStore.SetMediaMode(GameMediaMode.ImagesAnd3D);
		UpdateModeButtons();
	}

	private void UpdateModeButtons()
	{
		var currentMode = _gameSettingsStore.MediaMode;

		SetModeButtonVisible(_imagesAnd3DButton, currentMode == GameMediaMode.ImagesAnd3D);
		SetModeButtonVisible(_imagesButton, currentMode == GameMediaMode.Images);
		SetModeButtonVisible(_objects3DButton, currentMode == GameMediaMode.Objects3D);
	}

	private static float MapSliderValue(double value, float minOutput, float maxOutput)
	{
		var normalized = Mathf.InverseLerp(1.0f, 10.0f, (float)value);
		return Mathf.Lerp(minOutput, maxOutput, normalized);
	}

	private static Color MapSkinColour(double value)
	{
		var normalized = Mathf.InverseLerp(1.0f, 10.0f, (float)value);
		return LightSkinColour.Lerp(DarkSkinColour, normalized);
	}

	private static void ApplyHandMaterialColour(Node handMesh, Color colour)
	{
		var material = handMesh.Get("material").AsGodotObject() as StandardMaterial3D;
		material.AlbedoColor = colour;
	}

	private static void SetModeButtonVisible(Button button, bool visible)
	{
		button.Visible = visible;
		button.ProcessMode = visible ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
	}
}