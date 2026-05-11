using Godot;

public partial class OutputScreenBridge : Node
{
	[Export] public NodePath OutputFramePath;
	[Export] public float TargetImageWidth = 1.5f;
	[Export] public float Border = 0.1f;
	[Export] public float CollisionDepth = 0.03f;

	private Node3D _outputFrame;
	private MeshInstance3D _picture;
	private MeshInstance3D _frame;
	private CollisionShape3D _collision;
	private Node3D _grabLeft;
	private Node3D _grabRight;

	private StandardMaterial3D _pictureMaterial;

	public override void _Ready()
	{
		_outputFrame = GetNode<Node3D>(OutputFramePath);

		_picture = _outputFrame.GetNode<MeshInstance3D>("Picture");
		_frame = _outputFrame.GetNode<MeshInstance3D>("Frame");
		_collision = _outputFrame.GetNode<CollisionShape3D>("Collision");
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

		float imageWidth = TargetImageWidth;
		float imageHeight = imageWidth / aspect;

		float frameWidth = imageWidth + Border * 2.0f;
		float frameHeight = imageHeight + Border * 2.0f;

		_picture.Scale = new Vector3(imageWidth, imageHeight, 1.0f);
		_frame.Scale = new Vector3(frameWidth, frameHeight, 1.0f);

		if (_collision.Shape is BoxShape3D box)
		{
			box.Size = new Vector3(frameWidth, frameHeight, CollisionDepth);
		}
		else
		{
			var newBox = new BoxShape3D();
			newBox.Size = new Vector3(frameWidth, frameHeight, CollisionDepth);
			_collision.Shape = newBox;
		}

		float halfWidth = frameWidth / 2.0f;
		float halfHeight = frameHeight / 2.0f;

		_grabLeft.Position = new Vector3(-halfWidth+0.02f, 0.0f, -0.0925f);
		_grabRight.Position = new Vector3(halfWidth-0.02f, 0.0f, -0.0925f);
	}
}
