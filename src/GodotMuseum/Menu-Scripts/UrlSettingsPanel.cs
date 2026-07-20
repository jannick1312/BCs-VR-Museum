using Godot;
using Logger;

namespace BCSVRMuseum.Menu_Scripts;

public partial class UrlSettingsPanel : Node
{
	private static readonly Color CheckingColor = new(1.0f, 0.5f, 0.0f);
	private static readonly Color ValidColor = new(0.2f, 0.8f, 0.3f);
	private static readonly Color InvalidColor = new(1.0f, 0.2f, 0.2f);

	private readonly EventLogger _logger = new(nameof(UrlSettingsPanel));

	private Label _currentUrlValueLabel;
	private Node3D _keyboard;
	private CollisionShape3D _keyboardCollisionShape;
	private Button _revertButton;
	private SearchSettingsStore _searchSettingsStore;
	private ServerValidationController _serverValidationController;
	private Button _submitButton;
	private LineEdit _urlInput;

	public override void _Ready()
	{
		var root = GetParent();

		_urlInput = (LineEdit)root.FindChild("URLInput", true, false);
		_currentUrlValueLabel = (Label)root.FindChild("URLCurrentlyValue", true, false);
		_submitButton = (Button)root.FindChild("Submit", true, false);
		_revertButton = (Button)root.FindChild("Revert", true, false);

		_searchSettingsStore = (SearchSettingsStore)GetTree().Root.FindChild("SearchSettingsStore", true, false);
		var searchUseCaseFactory = (SearchUseCaseFactory)GetTree().Root.FindChild("SearchUseCaseFactory", true, false);
		_serverValidationController = new ServerValidationController(_searchSettingsStore, searchUseCaseFactory, _searchSettingsStore.EntryState);
		_keyboard = GetTree().Root.GetNode<Node3D>("Main/MenuNode/2DIn3DKeyboard");
		_keyboardCollisionShape = (CollisionShape3D)_keyboard.FindChild("CollisionShape3D", true, false);

		_urlInput.FocusEntered += OnUrlInputFocusEntered;
		_urlInput.FocusExited += OnUrlInputFocusExited;
		_urlInput.TextSubmitted += _ => DismissUrlInput();
		_submitButton.Pressed += OnSubmitPressed;
		_revertButton.Pressed += OnRevertPressed;
		_serverValidationController.StatusChanged += OnServerValidationStatusChanged;

		SetKeyboardVisible(false);
		UpdateCurrentUrlLabel();
		ValidateCurrentUrl();
	}

	public override void _ExitTree()
	{
		if (_serverValidationController == null)
			return;

		_serverValidationController.StatusChanged -= OnServerValidationStatusChanged;
		_serverValidationController.Dispose();
	}

	public override void _Input(InputEvent @event)
	{
		if (!_urlInput.HasFocus())
			return;

		if ((@event is InputEventMouseButton { Pressed: true } mouseButton && !IsInsideUrlInput(mouseButton.Position)) || (@event is InputEventScreenTouch { Pressed: true } screenTouch && !IsInsideUrlInput(screenTouch.Position)))
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
		_currentUrlValueLabel.Text = _searchSettingsStore.CurrentIp;
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
		_urlInput.ReleaseFocus();
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

		_keyboardCollisionShape.Disabled = !visible;
	}

	private void ValidateCurrentUrl()
	{
		_ = _serverValidationController.ValidateCurrentServerAsync();
	}

	private void OnServerValidationStatusChanged(ServerValidationStatus status)
	{
		var color = status switch
		{
			ServerValidationStatus.Checking => CheckingColor,
			ServerValidationStatus.Valid => ValidColor,
			_ => InvalidColor
		};

		_currentUrlValueLabel.AddThemeColorOverride("font_color", color);
	}
}
