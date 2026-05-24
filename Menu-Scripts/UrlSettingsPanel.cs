using Godot;

public partial class UrlSettingsPanel : Node
{
	private ServerUrlStore _serverUrlStore;

    private LineEdit _urlInput;
    private Label _currentUrlLabel;
    private Button _submitButton;
    private Button _revertButton;
    private CheckBox _deployedCheckBox;

	public override async void _Ready()
	{
		for (int i = 0; i < 8; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		_serverUrlStore = GetTree().Root.FindChild("ServerUrlStore", true, false) as ServerUrlStore;

		Node root = GetParent();

        _urlInput = root.FindChild("URLinput", true, false) as LineEdit;
        _currentUrlLabel = root.FindChild("URLcurrently", true, false) as Label;
        _submitButton = root.FindChild("Submit", true, false) as Button;
        _revertButton = root.FindChild("Revert", true, false) as Button;
        _deployedCheckBox = root.FindChild("Check", true, false) as CheckBox;

        _submitButton.Pressed += OnSubmitPressed;
        _revertButton.Pressed += OnRevertPressed;
        _deployedCheckBox.Toggled += OnDeployedToggled;

        _deployedCheckBox.ButtonPressed = _serverUrlStore.Deployed;

		UpdateCurrentUrlLabel();
	}

	private void OnSubmitPressed()
	{
		string input = _urlInput.Text.Trim();

		if (string.IsNullOrWhiteSpace(input))
			return;

		string newUrl = "http://" + input;

		_serverUrlStore.SetServerUrl(newUrl);

		_urlInput.Clear();
		UpdateCurrentUrlLabel();
	}

	private void OnRevertPressed()
	{
		_serverUrlStore.RevertServerUrl();

		_urlInput.Clear();
		UpdateCurrentUrlLabel();
	}

	private void UpdateCurrentUrlLabel()
	{
		_currentUrlLabel.Text = "Currently using:\n" + _serverUrlStore.CurrentServerUrl;
	}
	
	private void OnDeployedToggled(bool toggledOn)
	{
		_serverUrlStore.SetDeployed(toggledOn);

		UpdateCurrentUrlLabel();
	}
}
