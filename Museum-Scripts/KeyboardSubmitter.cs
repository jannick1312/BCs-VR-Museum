using Godot;
using System.Text;

public partial class KeyboardSubmitter : Node
{
	[Export] public NodePath InputScreenBridgePath;
	[Export] public NodePath OutputScreenBridgePath;
	[Export] public NodePath VisibilityControllerPath;

	private InputScreenBridge _inputScreen;
	private OutputScreenBridge _outputScreen;
	private VisibilityController _visibility;

	private LineEdit _inputLineEdit;
	private LineEdit _activeLineEdit;

	private HttpRequest _httpRequest;

	private ServerUrlStore _serverUrlStore;

	public override async void _Ready()
	{
		for (int i = 0; i < 12; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		_httpRequest = GetNodeOrNull<HttpRequest>("HTTPRequest");

		if (_httpRequest == null)
		{
			_httpRequest = new HttpRequest();
			_httpRequest.Name = "HTTPRequest";
			AddChild(_httpRequest);
		}

		_inputScreen = GetNodeOrNull<InputScreenBridge>(InputScreenBridgePath);
		_outputScreen = GetNodeOrNull<OutputScreenBridge>(OutputScreenBridgePath);
		_visibility = GetNodeOrNull<VisibilityController>(VisibilityControllerPath);

		_serverUrlStore = GetTree().Root.FindChild("ServerUrlStore", true, false) as ServerUrlStore;

		_inputLineEdit = _inputScreen.InputLineEdit;

		if (_inputLineEdit == null)
		{
			return;
		}

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

	public void SubmitText()
	{
		if (_activeLineEdit == null)
			return;

		string text = _activeLineEdit.Text;

		if (string.IsNullOrWhiteSpace(text))
		{
			_activeLineEdit.ReleaseFocus();
			_visibility.HideKeyboard();
			return;
		}

		string safeText = text.Replace("\\", "\\\\").Replace("\"", "\\\"");
		string json = "{\"text\":\"" + safeText + "\"}";
		string[] headers = { "Content-Type: application/json" };

		_httpRequest.Request(
			_serverUrlStore.CurrentServerUrl,
			headers,
			HttpClient.Method.Post,
			json
		);

		_activeLineEdit.Clear();
		_activeLineEdit.ReleaseFocus();
		_visibility.HideKeyboard();
	}

	private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		string responseText = Encoding.UTF8.GetString(body);

		Json json = new Json();

		if (json.Parse(responseText) != Error.Ok)
			return;

		var data = json.Data.AsGodotDictionary();

		if (!data.ContainsKey("image_url"))
			return;

		string imageUrl = data["image_url"].ToString();

		_outputScreen.SetOutputImageFromUrl(imageUrl);
		_visibility.ShowOutput();
	}
}
