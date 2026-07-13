using Godot;
using Models;

namespace BCSVRMuseum.Menu_Scripts;

public partial class GameSettingsPanel : Node
{
	private const double DefaultSliderValue = 5.0;
	private const double DefaultSkinColourValue = 5.0;
	private const float MinMovementSpeed = 0.8f;
	private const float MaxMovementSpeed = 4.0f;
	private const float MinTurnSpeed = 0.8f;
	private const float MaxTurnSpeed = 4.0f;
	private static readonly Color LightSkinColour = new(0.92f, 0.68f, 0.52f);
	private static readonly Color DarkSkinColour = new(0.24f, 0.13f, 0.08f);
	private Button _defaultButton;
	private GameSettingsStore _gameSettingsStore;
	private Button _imagesAnd3DButton;
	private Button _imagesButton;
	private MeshInstance3D _leftFallbackHandMesh;
	private Node _leftHandTrackingMesh;
	private Node _movementDirect;
	private HSlider _movementSlider;
	private Node _movementTurn;
	private Button _objects3DButton;
	private MeshInstance3D _rightFallbackHandMesh;
	private Node _rightHandTrackingMesh;
	private HSlider _skinColourSlider;
	private HSlider _turnSlider;

	public override void _Ready()
	{
		var root = GetParent();

		_movementSlider = (HSlider)root.FindChild("MovementSlider", true, false);
		_turnSlider = (HSlider)root.FindChild("TurnSlider", true, false);
		_skinColourSlider = (HSlider)root.FindChild("SkinColourSlider", true, false);
		_imagesAnd3DButton = (Button)root.FindChild("Images3D", true, false);
		_imagesButton = (Button)root.FindChild("Images", true, false);
		_objects3DButton = (Button)root.FindChild("3D", true, false);
		_defaultButton = (Button)root.FindChild("Default", true, false);
		_movementDirect = GetTree().Root.FindChild("MovementDirect", true, false);
		_movementTurn = GetTree().Root.FindChild("MovementTurn", true, false);
		_leftHandTrackingMesh = GetTree().Root.FindChild("LeftHandTrackingMesh", true, false);
		_rightHandTrackingMesh = GetTree().Root.FindChild("RightHandTrackingMesh", true, false);
		_leftFallbackHandMesh = (MeshInstance3D)GetTree().Root.FindChild("mesh_Hand_Nails_L", true, false);
		_rightFallbackHandMesh = (MeshInstance3D)GetTree().Root.FindChild("mesh_Hand_Nails_R", true, false);
		_leftFallbackHandMesh.MaterialOverride = _leftHandTrackingMesh.Get("material").AsGodotObject() as Material;
		_rightFallbackHandMesh.MaterialOverride = _rightHandTrackingMesh.Get("material").AsGodotObject() as Material;
		_gameSettingsStore = (GameSettingsStore)GetTree().Root.FindChild("GameSettingsStore", true, false);

		_movementSlider.ValueChanged += OnMovementSliderChanged;
		OnMovementSliderChanged(_movementSlider.Value);

		_turnSlider.ValueChanged += OnTurnSliderChanged;
		OnTurnSliderChanged(_turnSlider.Value);

		_skinColourSlider.ValueChanged += OnSkinColourSliderChanged;
		OnSkinColourSliderChanged(_skinColourSlider.Value);

		_imagesAnd3DButton.Pressed += CycleMode;
		_imagesButton.Pressed += CycleMode;
		_objects3DButton.Pressed += CycleMode;
		_defaultButton.Pressed += ResetToDefaults;

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
		_gameSettingsStore.SetMediaMode(MediaMode.ImagesAnd3D);
		UpdateModeButtons();
	}

	private void UpdateModeButtons()
	{
		var currentMode = _gameSettingsStore.CurrentMediaMode;

		SetModeButtonVisible(_imagesAnd3DButton, currentMode == MediaMode.ImagesAnd3D);
		SetModeButtonVisible(_imagesButton, currentMode == MediaMode.Images);
		SetModeButtonVisible(_objects3DButton, currentMode == MediaMode.Objects3D);
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
		material?.AlbedoColor = colour;
	}

	private static void SetModeButtonVisible(Button button, bool visible)
	{
		button.Visible = visible;
		button.ProcessMode = visible ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
	}
}
