using Godot;
using Logger;

namespace BCSVRMuseum.Menu_Scripts;

/// <summary>
/// Manages the server address, checks, and status colours.
/// </summary>
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

	/// <summary>
	/// Finds the panel controls, connects events, and checks the current address.
	/// </summary>
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

	/// <summary>
	/// Disconnects and closes the server checker.
	/// </summary>
	public override void _ExitTree()
	{
		if (_serverValidationController == null)
			return;

		_serverValidationController.StatusChanged -= OnServerValidationStatusChanged;
		_serverValidationController.Dispose();
	}

	/// <summary>
	/// Closes the address input when the user presses outside it.
	/// </summary>
	/// <param name="event">The input event to inspect.</param>
	public override void _Input(InputEvent @event)
	{
		if (!_urlInput.HasFocus())
			return;

		if ((@event is InputEventMouseButton { Pressed: true } mouseButton && !IsInsideUrlInput(mouseButton.Position)) || (@event is InputEventScreenTouch { Pressed: true } screenTouch && !IsInsideUrlInput(screenTouch.Position)))
			DismissUrlInput();
	}

	/// <summary>
	/// Saves a non-empty server address and checks it.
	/// </summary>
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

	/// <summary>
	/// Restores the saved server address and checks it.
	/// </summary>
	private void OnRevertPressed()
	{
		_searchSettingsStore.RevertServerUrl();
		_urlInput.Clear();
		SetKeyboardVisible(false);
		UpdateCurrentUrlLabel();
		ValidateCurrentUrl();
	}

	/// <summary>
	/// Displays the active server address.
	/// </summary>
	private void UpdateCurrentUrlLabel()
	{
		_currentUrlValueLabel.Text = _searchSettingsStore.CurrentIp;
	}

	/// <summary>
	/// Shows the virtual keyboard when address input gains focus.
	/// </summary>
	private void OnUrlInputFocusEntered()
	{
		SetKeyboardVisible(true);
	}

	/// <summary>
	/// Hides the virtual keyboard when address input loses focus.
	/// </summary>
	private void OnUrlInputFocusExited()
	{
		SetKeyboardVisible(false);
	}

	/// <summary>
	/// Releases address input focus and hides the virtual keyboard.
	/// </summary>
	private void DismissUrlInput()
	{
		_urlInput.ReleaseFocus();
		SetKeyboardVisible(false);
	}

	/// <summary>
	/// Checks if the user pressed inside the address field.
	/// </summary>
	/// <param name="position">The position of the press.</param>
	/// <returns><see langword="true"/> if the press is inside the field and <see langword="false"/> otherwise.</returns>
	private bool IsInsideUrlInput(Vector2 position)
	{
		return _urlInput.GetGlobalRect().HasPoint(position);
	}

	/// <summary>
	/// Shows or hides the keyboard and turns its collision on or off.
	/// </summary>
	/// <param name="visible">If the keyboard should be active and visible.</param>
	private void SetKeyboardVisible(bool visible)
	{
		_keyboard.Visible = visible;
		_keyboard.ProcessMode = visible ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;

		_keyboardCollisionShape.Disabled = !visible;
	}

	/// <summary>
	/// Starts a check of the current server address.
	/// </summary>
	private void ValidateCurrentUrl()
	{
		_ = _serverValidationController.ValidateCurrentServerAsync();
	}

	/// <summary>
	/// Updates the address colour for the current check status.
	/// </summary>
	/// <param name="status">The current check status.</param>
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
