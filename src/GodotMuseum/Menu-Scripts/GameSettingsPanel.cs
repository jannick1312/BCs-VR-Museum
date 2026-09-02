using Godot;
using Models;

namespace BCSVRMuseum.Menu_Scripts;

/// <summary>
/// Sets movement, hand colour, and media mode from the menu.
/// </summary>
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

	/// <summary>
	/// Finds the settings controls, uses their values, and connects events.
	/// </summary>
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

	/// <summary>
	/// Sets the selected walking speed.
	/// </summary>
	/// <param name="value">The movement slider value.</param>
	private void OnMovementSliderChanged(double value)
	{
		_movementDirect.Set("max_speed", MapSliderValue(value, MinMovementSpeed, MaxMovementSpeed));
	}

	/// <summary>
	/// Sets the selected turning speed.
	/// </summary>
	/// <param name="value">The turn slider value.</param>
	private void OnTurnSliderChanged(double value)
	{
		_movementTurn.Set("smooth_turn_speed", MapSliderValue(value, MinTurnSpeed, MaxTurnSpeed));
	}

	/// <summary>
	/// Sets the selected skin colour on tracked hands.
	/// </summary>
	/// <param name="value">The skin colour slider value.</param>
	private void OnSkinColourSliderChanged(double value)
	{
		var skinColour = MapSkinColour(value);

		ApplyHandMaterialColour(_leftHandTrackingMesh, skinColour);
		ApplyHandMaterialColour(_rightHandTrackingMesh, skinColour);
	}

	/// <summary>
	/// Selects the next media mode and refreshes the mode buttons.
	/// </summary>
	private void CycleMode()
	{
		_gameSettingsStore.CycleMediaMode();
		UpdateModeButtons();
	}

	/// <summary>
	/// Restores all game settings to their default values.
	/// </summary>
	private void ResetToDefaults()
	{
		_movementSlider.Value = DefaultSliderValue;
		_turnSlider.Value = DefaultSliderValue;
		_skinColourSlider.Value = DefaultSkinColourValue;
		_gameSettingsStore.SetMediaMode(MediaMode.ImagesAnd3D);
		UpdateModeButtons();
	}

	/// <summary>
	/// Shows the button for the current media mode.
	/// </summary>
	private void UpdateModeButtons()
	{
		var currentMode = _gameSettingsStore.CurrentMediaMode;

		SetModeButtonVisible(_imagesAnd3DButton, currentMode == MediaMode.ImagesAnd3D);
		SetModeButtonVisible(_imagesButton, currentMode == MediaMode.Images);
		SetModeButtonVisible(_objects3DButton, currentMode == MediaMode.Objects3D);
	}

	/// <summary>
	/// Changes a slider value to a value between the minimum and maximum.
	/// </summary>
	/// <param name="value">The slider value from 1 to 10.</param>
	/// <param name="minOutput">The output value at the lower end.</param>
	/// <param name="maxOutput">The output value at the upper end.</param>
	/// <returns>The mapped output value.</returns>
	private static float MapSliderValue(double value, float minOutput, float maxOutput)
	{
		var normalized = Mathf.InverseLerp(1.0f, 10.0f, (float)value);
		return Mathf.Lerp(minOutput, maxOutput, normalized);
	}

	/// <summary>
	/// Blends skin colours based on the slider value.
	/// </summary>
	/// <param name="value">The skin colour slider value.</param>
	/// <returns>The selected skin colour.</returns>
	private static Color MapSkinColour(double value)
	{
		var normalized = Mathf.InverseLerp(1.0f, 10.0f, (float)value);
		return LightSkinColour.Lerp(DarkSkinColour, normalized);
	}

	/// <summary>
	/// Sets the colour of a tracked hand.
	/// </summary>
	/// <param name="handMesh">The hand mesh.</param>
	/// <param name="colour">The colour to apply.</param>
	private static void ApplyHandMaterialColour(Node handMesh, Color colour)
	{
		var material = handMesh.Get("material").AsGodotObject() as StandardMaterial3D;
		material?.AlbedoColor = colour;
	}

	/// <summary>
	/// Shows or hides a media mode button.
	/// </summary>
	/// <param name="button">The mode button to update.</param>
	/// <param name="visible">If the button should be active and visible.</param>
	private static void SetModeButtonVisible(Button button, bool visible)
	{
		button.Visible = visible;
		button.ProcessMode = visible ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
	}
}



// Codex helped implement the slider mapping.
