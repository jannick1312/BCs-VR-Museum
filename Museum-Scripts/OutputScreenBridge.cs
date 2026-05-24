using Godot;

public partial class OutputScreenBridge : Node
{
	[Export] public NodePath OutputFramePath;
	[Export] public float Scale = 1.5f;

	private Node3D _outputFrame;
	private MeshInstance3D _picture;
	private FrameMaker _frameMaker;

	private StandardMaterial3D _pictureMaterial;

	public override void _Ready()
	{
		_outputFrame = GetNode<Node3D>(OutputFramePath);

		_picture = _outputFrame.GetNode<MeshInstance3D>("Picture");
		_frameMaker = _outputFrame.GetNode<FrameMaker>("FrameMaker");

		_pictureMaterial = new StandardMaterial3D();
		_pictureMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled;

		_picture.MaterialOverride = _pictureMaterial;
	}

	public async void SetOutputImageFromUrl(string imageUrl)
	{
		var request = new HttpRequest();
		AddChild(request);

		Error error = request.Request(imageUrl);

		var resultArray = await ToSignal(request, HttpRequest.SignalName.RequestCompleted);

		long responseCode = (long)resultArray[1];
		byte[] body = (byte[])resultArray[3];

		request.QueueFree();

		Image image = new Image();

		Error loadError = image.LoadJpgFromBuffer(body);

		if (loadError != Error.Ok)
			loadError = image.LoadPngFromBuffer(body);

		if (loadError != Error.Ok)
			loadError = image.LoadWebpFromBuffer(body);

		ImageTexture texture = ImageTexture.CreateFromImage(image);
		ApplyTexture(texture);
	}

	public void SetOutputImageFromLocalPath(string imagePath)
	{
		Image image = new Image();
		Error loadError = image.Load(imagePath);

		ImageTexture texture = ImageTexture.CreateFromImage(image);
		ApplyTexture(texture);
	}

	private void ApplyTexture(ImageTexture texture)
	{
		_pictureMaterial.AlbedoTexture = texture;

		float aspect = (float)texture.GetWidth() / texture.GetHeight();

		float imageWidth = Scale;
		float imageHeight = imageWidth / aspect;

		_picture.Scale = new Vector3(imageWidth, imageHeight, 1.0f);

		_frameMaker.UpdateFrame(_picture, imageWidth, imageHeight);
	}
}
