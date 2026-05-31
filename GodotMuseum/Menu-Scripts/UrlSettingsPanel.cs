using Godot;
namespace BCSVRMuseum.Menu_Scripts;

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
		for (var i = 0; i < 8; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		_serverUrlStore = GetTree().Root.FindChild("ServerUrlStore", true, false) as ServerUrlStore;

		var root = GetParent();

		_urlInput = root.FindChild("URLinput", true, false) as LineEdit;
		_currentUrlLabel = root.FindChild("URLcurrently", true, false) as Label;
		_submitButton = root.FindChild("Submit", true, false) as Button;
		_revertButton = root.FindChild("Revert", true, false) as Button;
		_deployedCheckBox = root.FindChild("Check", true, false) as CheckBox;

		if (_submitButton != null) _submitButton.Pressed += OnSubmitPressed;
		if (_revertButton != null) _revertButton.Pressed += OnRevertPressed;
		if (_deployedCheckBox != null)
		{
			_deployedCheckBox.Toggled += OnDeployedToggled;
			if (_serverUrlStore != null) _deployedCheckBox.ButtonPressed = _serverUrlStore.Deployed;
		}

		UpdateCurrentUrlLabel();
	}

	private void OnSubmitPressed()
	{
		var input = _urlInput.Text.Trim();

		if (string.IsNullOrWhiteSpace(input))
			return;

		_serverUrlStore.SetServerIp(input);

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
		_currentUrlLabel.Text = "Currently using:\n" + _serverUrlStore.CurrentIp;
	}
	
	private void OnDeployedToggled(bool toggledOn)
	{
		_serverUrlStore.SetDeployed(toggledOn);

		UpdateCurrentUrlLabel();
	}
}