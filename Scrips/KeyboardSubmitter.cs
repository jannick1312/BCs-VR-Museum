using Godot;
using System.Text;

public partial class KeyboardSubmitter : Node
{
	[Export] public NodePath InputScreenBridgePath;
	[Export] public NodePath InputScreenBridgePath2;
	[Export] public NodePath OutputScreenBridgePath;
	[Export] public NodePath VisibilityControllerPath;

	private InputScreenBridge _inputScreen;
	private InputScreenBridge _inputScreen2;
	private OutputScreenBridge _outputScreen;
	private VisibilityController _visibility;

	private LineEdit _inputLineEdit;
	private LineEdit _inputLineEdit2;
	private LineEdit _activeLineEdit;

	private HttpRequest _httpRequest;

	private string _serverUrl = "http://192.168.1.21:5050/search_one";

	public override async void _Ready()
	{
		for (int i = 0; i < 8; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		_httpRequest = GetNode<HttpRequest>("HTTPRequest");

		_inputScreen = GetNode<InputScreenBridge>(InputScreenBridgePath);
		_inputScreen2 = GetNode<InputScreenBridge>(InputScreenBridgePath2);
		_outputScreen = GetNode<OutputScreenBridge>(OutputScreenBridgePath);
		_visibility = GetNode<VisibilityController>(VisibilityControllerPath);

		_inputLineEdit = _inputScreen.InputLineEdit;
		_inputLineEdit2 = _inputScreen2.InputLineEdit;

		_activeLineEdit = null;

		_inputLineEdit.FocusEntered += () => SetActiveInput(_inputLineEdit);
		_inputLineEdit2.FocusEntered += () => SetActiveInput(_inputLineEdit2);

		_inputLineEdit.GuiInput += inputEvent => OnInputGuiInput(inputEvent, _inputLineEdit);
		_inputLineEdit2.GuiInput += inputEvent => OnInputGuiInput(inputEvent, _inputLineEdit2);

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
			_serverUrl,
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
		json.Parse(responseText);

		var data = json.Data.AsGodotDictionary();
		string imageUrl = data["image_url"].ToString();

		_outputScreen.SetOutputImageFromUrl(imageUrl);
		_visibility.ShowOutput();
	}
}
