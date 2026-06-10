using Godot;
namespace BCSVRMuseum.Menu_Scripts;

public partial class UrlSettingsPanel : Node
{
	private SearchSettingsStore _searchSettingsStore;

    private LineEdit _urlInput;
    private Label _currentUrlLabel;
    private Button _submitButton;
    private Button _revertButton;
    private CheckBox _deployedCheckBox;

	public override void _Ready()
	{
		var root = GetParent();

		_urlInput = root.FindChild("URLinput", true, false) as LineEdit;
		_currentUrlLabel = root.FindChild("URLcurrently", true, false) as Label;
		_submitButton = root.FindChild("Submit", true, false) as Button;
		_revertButton = root.FindChild("Revert", true, false) as Button;
		_deployedCheckBox = root.FindChild("Check", true, false) as CheckBox;
		
		_searchSettingsStore = GetTree().Root.FindChild("SearchSettingsStore", true, false) as SearchSettingsStore;

		if (_submitButton != null) _submitButton.Pressed += OnSubmitPressed;
		if (_revertButton != null) _revertButton.Pressed += OnRevertPressed;
		if (_deployedCheckBox != null)
		{
			_deployedCheckBox.Toggled += OnDeployedToggled;
			if (_searchSettingsStore != null) _deployedCheckBox.ButtonPressed = _searchSettingsStore.Deployed;
		}

		UpdateCurrentUrlLabel();
	}

	private void OnSubmitPressed()
	{
		var input = _urlInput.Text.Trim();

		if (string.IsNullOrWhiteSpace(input))
			return;

		_searchSettingsStore.SetServerIp(input);
		_urlInput.Clear();
		UpdateCurrentUrlLabel();
	}

	private void OnRevertPressed()
	{
		_searchSettingsStore.RevertServerUrl();
		_urlInput.Clear();
		UpdateCurrentUrlLabel();
	}

	private void UpdateCurrentUrlLabel()
	{
		_currentUrlLabel.Text = "Currently using:\n" + _searchSettingsStore.CurrentIp;
	}
	
	private void OnDeployedToggled(bool toggledOn)
	{
		_searchSettingsStore.SetDeployed(toggledOn);
		UpdateCurrentUrlLabel();
	}
}