using Godot;
using System.IO;
using System.Text;

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

		if (_httpRequest.GetHttpClientStatus() !=
			HttpClient.Status.Disconnected)
		{
			GD.Print("Cancelling previous request...");
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

		string json;
		string requestUrl;

		if (_serverUrlStore.Deployed)
		{
			string safeText = text.Replace("\\", "\\\\").Replace("\"", "\\\"");
			json = "{\"text\":\"" + safeText + "\"}";
			requestUrl = _serverUrlStore.CurrentServerUrl + "search_one";
		}
		else
		{
			json = BuildVitrivrQuery(text);
			requestUrl = _serverUrlStore.CurrentServerUrl;
		}

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

	private string BuildVitrivrQuery(string text)
	{
		var payload = new Godot.Collections.Dictionary
		{
			["inputs"] = new Godot.Collections.Dictionary
			{
				["txt"] = new Godot.Collections.Dictionary
				{
					["type"] = "TEXT",
					["data"] = text
				}
			},

			["operations"] = new Godot.Collections.Dictionary
			{
				["clip"] = new Godot.Collections.Dictionary
				{
					["field"] = "clip",

					["inputs"] = new Godot.Collections.Dictionary
					{
						["input"] = "txt"
					},

					["parameters"] = new Godot.Collections.Dictionary
					{
						["limit"] = "1"
					}
				},

				["filelookup"] = new Godot.Collections.Dictionary
				{
					["factory"] = "FieldLookup",

					["inputs"] = new Godot.Collections.Dictionary
					{
						["in"] = "clip"
					},

					["parameters"] = new Godot.Collections.Dictionary
					{
						["field"] = "file",
						["keys"] = "path"
					}
				}
			},

			["output"] = "filelookup"
		};

		return Json.Stringify(payload);
	}

	private void OnRequestCompleted(long result, long responseCode, string[] headers, byte[] body)
	{
		_requestTimeout = null;

		if (_serverUrlStore.Deployed)
			HandleDeployedResponse(responseCode, body);
		else
			HandleLocalResponse(responseCode, body);
	}

	private void HandleDeployedResponse(long responseCode, byte[] body)
	{
		string responseText = Encoding.UTF8.GetString(body);

		Json json = new Json();

		if (json.Parse(responseText) != Error.Ok)
			return;

		var data = json.Data.AsGodotDictionary();

		if (!data.ContainsKey("filename"))
			return;

		string filename = data["filename"].ToString();

		string imageUrl =
			_serverUrlStore.CurrentServerUrl +
			"media/" +
			filename;

		_outputScreen.SetOutputImageFromUrl(imageUrl);

		_visibility.ShowOutput();
	}

	private void HandleLocalResponse(long responseCode, byte[] body)
	{
		string responseText = Encoding.UTF8.GetString(body);

		Json json = new Json();

		if (json.Parse(responseText) != Error.Ok)
			return;

		var data = json.Data.AsGodotDictionary();

		if (!data.ContainsKey("retrievables"))
		{
			GD.PrintErr("No retrievables key in response.");
			return;
		}

		var retrievables = data["retrievables"].AsGodotArray();
		var best = retrievables[0].AsGodotDictionary();
		var descriptors = best["descriptors"].AsGodotDictionary();
		
		string dockerPath = descriptors["file.path"].ToString();
		string filename = Path.GetFileName(dockerPath);
		string localImagePath =Path.Combine(MediaFolderPath, filename);
		
		_outputScreen.SetOutputImageFromLocalPath(localImagePath);
		_visibility.ShowOutput();
	}
}