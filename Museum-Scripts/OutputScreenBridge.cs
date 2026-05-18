using Godot;

public partial class OutputScreenBridge : Node
{
	[Export] public NodePath OutputFramePath;
	[Export] public float Scale = 1.5f;
	[Export] public bool FrameUsesXZPlane = true;

	private Node3D _outputFrame;
	private MeshInstance3D _picture;
	private Node3D _frame;
	private CollisionShape3D _collision;
	private Node3D _grabLeft;
	private Node3D _grabRight;

	private StandardMaterial3D _pictureMaterial;

	public override void _Ready()
	{
		_outputFrame = GetNode<Node3D>(OutputFramePath);

		_picture = _outputFrame.GetNode<MeshInstance3D>("Picture");
		_frame = _outputFrame.GetNode<Node3D>("Frame");
		_collision = _outputFrame.GetNode<CollisionShape3D>("CollisionShape3D");
		_grabLeft = _outputFrame.GetNode<Node3D>("GrabPointHandLeft");
		_grabRight = _outputFrame.GetNode<Node3D>("GrabPointHandRight");

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

	private void ApplyTexture(ImageTexture texture)
	{
		_pictureMaterial.AlbedoTexture = texture;

		float aspect = (float)texture.GetWidth() / texture.GetHeight();

		float imageWidth = Scale;
		float imageHeight = imageWidth / aspect;

		float frameWidth = imageWidth + 0.2f;
		float frameHeight = imageHeight + 0.2f;

		_picture.Scale = new Vector3(imageWidth, imageHeight, 1.0f);
		_frame.Scale = new Vector3(frameWidth, 10.0f, frameHeight);

		BoxShape3D box = _collision.Shape as BoxShape3D;
		box.Size = new Vector3(frameWidth, frameHeight, 0.03f);

		float halfWidth = frameWidth / 2.0f;

		_grabLeft.Position = new Vector3(-halfWidth+0.02f, 0.0f, -0.0925f);
		_grabRight.Position = new Vector3(halfWidth-0.02f, 0.0f, -0.0925f);
	}
}
