using Godot;
using System.Text;
using Server;

public partial class KeyboardSubmitter : Node
{
	[Export] public NodePath InputScreenBridgePath;
	[Export] public NodePath OutputScreenBridgePath;
	[Export] public NodePath VisibilityControllerPath;

	[Export] public string MediaFolderPath = @"C:\Users\dbis-\Desktop\BCs\media";

	private InputScreenBridge _inputScreen;
	private OutputScreenBridge _outputScreen;
	private VisibilityController _visibility;

	private LineEdit _inputLineEdit;
	private LineEdit _activeLineEdit;

	private HttpRequest _httpRequest;

	private SceneTreeTimer _requestTimeout;

	private ServerUrlStore _serverUrlStore;

	public override async void _Ready()
	{
		for (int i = 0; i < 12; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		_httpRequest = GetNodeOrNull<HttpRequest>("HTTPRequest");

		_inputScreen = GetNodeOrNull<InputScreenBridge>(InputScreenBridgePath);
		_outputScreen = GetNodeOrNull<OutputScreenBridge>(OutputScreenBridgePath);
		_visibility = GetNodeOrNull<VisibilityController>(VisibilityControllerPath);

		_serverUrlStore = GetTree().Root.FindChild("ServerUrlStore", true, false) as ServerUrlStore;

		_inputLineEdit = _inputScreen.InputLineEdit;

		_inputLineEdit.FocusEntered += () => SetActiveInput(_inputLineEdit);
		_inputLineEdit.GuiInput += inputEvent => OnInputGuiInput(inputEvent, _inputLineEdit);

		_httpRequest.RequestCompleted += OnRequestCompleted;
	}

	private void SetActiveInput(LineEdit lineEdit)
	{
		_activeLineEdit = lineEdit;
		_visibility.ShowKeyboard();
	}

	private void OnInputGuiInput(InputEvent inputEvent, LineEdit lineEdit)
	{
		if (inputEvent is InputEventMouseButton mouseButton && mouseButton.Pressed)
		{
			_activeLineEdit = lineEdit;
			_visibility.ShowKeyboard();
		}
	}

	public async void SubmitText()
	{
		if (_activeLineEdit == null)
			return;

		if (_httpRequest.GetHttpClientStatus() != HttpClient.Status.Disconnected)
		{
			_httpRequest.CancelRequest();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		string text = _activeLineEdit.Text;

		if (string.IsNullOrWhiteSpace(text))
		{
			_activeLineEdit.ReleaseFocus();
			_visibility.HideKeyboard();
			return;
		}

		string json = ServerRequestFactory.BuildRequestBody(text, _serverUrlStore.Mode);

		string requestUrl = ServerRequestFactory.BuildRequestUrl( _serverUrlStore.CurrentServerUrl, _serverUrlStore.Mode);

		string[] headers = { "Content-Type: application/json" };

		Error err = _httpRequest.Request(
			requestUrl,
			headers,
			HttpClient.Method.Post,
			json
		);

		StartRequestTimeout();

		_activeLineEdit.Clear();
		_activeLineEdit.ReleaseFocus();
		_visibility.HideKeyboard();
	}

	private async void StartRequestTimeout()
	{
		_requestTimeout = GetTree().CreateTimer(5.0);

		await ToSignal(
			_requestTimeout,
			SceneTreeTimer.SignalName.Timeout
		);

		if (_httpRequest == null)
			return;

		if (_httpRequest.GetHttpClientStatus() != HttpClient.Status.Disconnected)
			_httpRequest.CancelRequest();
	}

	private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		_requestTimeout = null;

		string responseText = Encoding.UTF8.GetString(body);

		ServerResult serverResult = ServerResponseParser.Parse(responseText, _serverUrlStore.Mode, _serverUrlStore.CurrentServerUrl, MediaFolderPath);

		if (!serverResult.Success)
		{
			GD.PrintErr(serverResult.ErrorMessage);
			return;
		}

		if (serverResult.IsUrlResult)
			_outputScreen.SetOutputImageFromUrl(serverResult.ImageUrl);
		else if (serverResult.IsLocalPathResult)
			_outputScreen.SetOutputImageFromLocalPath(serverResult.LocalImagePath);

		_visibility.ShowOutput();
	}
}