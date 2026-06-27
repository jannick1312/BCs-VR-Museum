using Godot;
using Infrastructure.Logging;

namespace BCSVRMuseum.Menu_Scripts;

public partial class UrlSettingsPanel : Node
{
	private readonly EventLogger _logger = new(nameof(UrlSettingsPanel));
	private SearchSettingsStore _searchSettingsStore;
	private SearchUseCaseFactory _searchUseCaseFactory;

    private LineEdit _urlInput;
    private Label _currentUrlLabel;
    private Label _currentUrlValueLabel;
    private Button _submitButton;
    private Button _revertButton;
    private CheckBox _deployedCheckBox;
    private Node3D _keyboard;
    private CollisionShape3D _keyboardCollisionShape;
    private int _validationVersion;

	private static readonly Color CheckingColor = new(1.0f, 0.5f, 0.0f);
	private static readonly Color ValidColor = new(0.2f, 0.8f, 0.3f);
	private static readonly Color InvalidColor = new(1.0f, 0.2f, 0.2f);

	public override void _Ready()
	{
		var root = GetParent();

		_urlInput = root.FindChild("URLinput", true, false) as LineEdit;
		_currentUrlLabel = root.FindChild("URLcurrently", true, false) as Label;
		_currentUrlValueLabel = root.FindChild("URLcurrentlyValue", true, false) as Label;
		_submitButton = root.FindChild("Submit", true, false) as Button;
		_revertButton = root.FindChild("Revert", true, false) as Button;
		_deployedCheckBox = root.FindChild("Check", true, false) as CheckBox;
		
		_searchSettingsStore = GetTree().Root.FindChild("SearchSettingsStore", true, false) as SearchSettingsStore;
		_searchUseCaseFactory = GetTree().Root.FindChild("SearchUseCaseFactory", true, false) as SearchUseCaseFactory;
		_keyboard = GetTree().Root.GetNodeOrNull<Node3D>("Main/MenuNode/2Din3DKeyboard");
		_keyboardCollisionShape = _keyboard?.FindChild("CollisionShape3D", true, false) as CollisionShape3D;

		if (_urlInput != null)
		{
			_urlInput.FocusEntered += OnUrlInputFocusEntered;
			_urlInput.FocusExited += OnUrlInputFocusExited;
			_urlInput.TextSubmitted += _ => DismissUrlInput();
		}
		_submitButton?.Pressed += OnSubmitPressed;
		_revertButton?.Pressed += OnRevertPressed;
		if (_deployedCheckBox != null)
		{
			_deployedCheckBox.Toggled += OnDeployedToggled;
			if (_searchSettingsStore != null) _deployedCheckBox.ButtonPressed = _searchSettingsStore.Deployed;
		}

		SetKeyboardVisible(false);
		UpdateCurrentUrlLabel();
		ValidateCurrentUrl();
	}

	public override void _Input(InputEvent @event)
	{
		if (_urlInput == null || !_urlInput.HasFocus())
			return;

		if (@event is InputEventMouseButton { Pressed: true } mouseButton && !IsInsideUrlInput(mouseButton.Position))
			DismissUrlInput();
		else if (@event is InputEventScreenTouch { Pressed: true } screenTouch && !IsInsideUrlInput(screenTouch.Position))
			DismissUrlInput();
	}

	private void OnSubmitPressed()
	{
		var input = _urlInput.Text.Trim();

		if (string.IsNullOrWhiteSpace(input))
		{
			_logger.Warning("URL submit ignored because input is empty.");
			return;
		}
		_searchSettingsStore.SetServerIp(input);
		_urlInput.Clear();
		SetKeyboardVisible(false);
		UpdateCurrentUrlLabel();
		ValidateCurrentUrl();
	}

	private void OnRevertPressed()
	{
		_searchSettingsStore.RevertServerUrl();
		_urlInput.Clear();
		SetKeyboardVisible(false);
		UpdateCurrentUrlLabel();
		ValidateCurrentUrl();
	}

	private void UpdateCurrentUrlLabel()
	{
		if (_searchSettingsStore == null)
			return;
		if (_currentUrlValueLabel != null)
			_currentUrlValueLabel.Text = _searchSettingsStore.CurrentIp;
		else
			_currentUrlLabel?.Text = "Currently using:\n" + _searchSettingsStore.CurrentIp;
	}
	
	private void OnDeployedToggled(bool toggledOn)
	{
		_searchSettingsStore.SetDeployed(toggledOn);
		UpdateCurrentUrlLabel();
		ValidateCurrentUrl();
	}

	private void OnUrlInputFocusEntered()
	{
		SetKeyboardVisible(true);
	}

	private void OnUrlInputFocusExited()
	{
		SetKeyboardVisible(false);
	}

	private void DismissUrlInput()
	{
		_urlInput?.ReleaseFocus();
		SetKeyboardVisible(false);
	}

	private bool IsInsideUrlInput(Vector2 position)
	{
		return _urlInput.GetGlobalRect().HasPoint(position);
	}

	private void SetKeyboardVisible(bool visible)
	{
		_keyboard.Visible = visible;
		_keyboard.ProcessMode = visible ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;

		_keyboardCollisionShape?.Disabled = !visible;
	}

	private async void ValidateCurrentUrl()
	{
		if (_searchUseCaseFactory == null)
			return;

		var version = ++_validationVersion;

		SetCurrentUrlColor(CheckingColor);

		var valid = await _searchUseCaseFactory.GetValidateServer().ExecuteAsync();

		if (version != _validationVersion)
			return;

		SetCurrentUrlColor(valid ? ValidColor : InvalidColor);
	}

	private void SetCurrentUrlColor(Color color)
	{
		if (_currentUrlValueLabel != null)
			_currentUrlValueLabel.AddThemeColorOverride("font_color", color);
		else
			_currentUrlLabel?.AddThemeColorOverride("font_color", color);
	}
}