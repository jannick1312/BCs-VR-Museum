using Godot;

public partial class OutputScreenBridge : Node
{
	private TextureRect _resultImage;

	public override async void _Ready()
	{
		for (int i = 0; i < 4; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		var viewport = GetNodeOrNull<Viewport>("../Viewport");

		_resultImage = FindFirstTextureRect(viewport);
	}

	public async void SetOutputImageFromUrl(string imageUrl)
	{

		var request = new HttpRequest();
		AddChild(request);

		Error error = request.Request(imageUrl);

		var resultArray = await ToSignal(request, HttpRequest.SignalName.RequestCompleted);

		long result = (long)resultArray[0];
		long responseCode = (long)resultArray[1];
		byte[] body = (byte[])resultArray[3];

		Image image = new Image();

		Error loadError = image.LoadJpgFromBuffer(body);

		if (loadError != Error.Ok)
			loadError = image.LoadPngFromBuffer(body);

		if (loadError != Error.Ok)
			loadError = image.LoadWebpFromBuffer(body);

		_resultImage.Texture = ImageTexture.CreateFromImage(image);

		request.QueueFree();
	}

	private TextureRect FindFirstTextureRect(Node node)
	{
		if (node is TextureRect textureRect)
			return textureRect;

		foreach (Node child in node.GetChildren())
		{
			var found = FindFirstTextureRect(child);
			if (found != null)
				return found;
		}

		return null;
	}
}
