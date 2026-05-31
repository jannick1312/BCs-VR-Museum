using Godot;
namespace BCSVRMuseum.Museum_Scripts;

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

    public void SetOutputImageFromBytes(byte[] bytes)
    {
        var image = new Image();

        var loadError = image.LoadJpgFromBuffer(bytes);

        if (loadError != Error.Ok)
            loadError = image.LoadPngFromBuffer(bytes);

        if (loadError != Error.Ok)
            loadError = image.LoadWebpFromBuffer(bytes);

        if (loadError != Error.Ok || image.IsEmpty())
        {
            GD.PrintErr("Could not load image from bytes. Error: " + loadError);
            return;
        }

		var texture = ImageTexture.CreateFromImage(image);
		ApplyTexture(texture);
	}

	private void ApplyTexture(ImageTexture texture)
	{
		_pictureMaterial.AlbedoTexture = texture;

		var aspect = (float)texture.GetWidth() / texture.GetHeight();

		var imageWidth = Scale;
		var imageHeight = imageWidth / aspect;

		_picture.Scale = new Vector3(imageWidth, imageHeight, 1.0f);

		_frameMaker.UpdateFrame(_picture, imageWidth, imageHeight);
	}
}