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
	private HttpRequest _httpRequest;

	private string _serverUrl = "http://192.168.1.21:5050/search_one";

	public override async void _Ready()
	{
		for (int i = 0; i < 8; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		_httpRequest = GetNode<HttpRequest>("HTTPRequest");

		_inputScreen = GetNode<InputScreenBridge>(InputScreenBridgePath);
		_outputScreen = GetNode<OutputScreenBridge>(OutputScreenBridgePath);
		_visibility = GetNode<VisibilityController>(VisibilityControllerPath);

		_inputLineEdit = _inputScreen.InputLineEdit;

		_inputLineEdit.FocusEntered += _visibility.ShowKeyboard;
		_inputLineEdit.GuiInput += OnInputGuiInput;

		_httpRequest.RequestCompleted += OnRequestCompleted;
	}

	private void OnInputGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton mouseButton && mouseButton.Pressed)
			_visibility.ShowKeyboard();
	}

	public void SubmitText()
	{
		string text = _inputLineEdit.Text;

		if (string.IsNullOrWhiteSpace(text))
		{
			_inputLineEdit.ReleaseFocus();
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

		_inputLineEdit.Clear();
		_inputLineEdit.ReleaseFocus();
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
