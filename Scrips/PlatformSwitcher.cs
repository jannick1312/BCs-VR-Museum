using Godot;

public partial class PlatformSwitcher : Node
{
	[Export] public NodePath PlayerRigPath;
	[Export] public NodePath CameraPath;
	[Export] public NodePath MenuSpawnPointPath;

	[Export] public NodePath LeftMovementPath;
	[Export] public NodePath RightTurnMovementPath;
	[Export] public NodePath RightJumpMovementPath;

	private Node3D _playerRig;
	private XRCamera3D _camera;
	private Marker3D _menuSpawnPoint;

	private Node _leftMovement;
	private Node _rightTurnMovement;
	private Node _rightJumpMovement;

	private Transform3D _lastMainTransform;

	private bool _isOnMenuPlatform = false;
	private bool _bothWerePressed = false;

	public override void _Ready()
	{
		_playerRig = GetNode<Node3D>(PlayerRigPath);
		_camera = GetNode<XRCamera3D>(CameraPath);
		_menuSpawnPoint = GetNode<Marker3D>(MenuSpawnPointPath);

		_leftMovement = GetNode<Node>(LeftMovementPath);
		_rightTurnMovement = GetNode<Node>(RightTurnMovementPath);
		_rightJumpMovement = GetNode<Node>(RightJumpMovementPath);

		SetMovementEnabled(true);
	}

	public override void _Process(double delta)
	{
		XRController3D left = GetTree().Root.FindChild("LeftController", true, false) as XRController3D;
		XRController3D right = GetTree().Root.FindChild("RightController", true, false) as XRController3D;

		bool bothPressed =
			left.GetFloat("trigger") > 0.75f &&
			right.GetFloat("trigger") > 0.75f;

		if (bothPressed && !_bothWerePressed)
			TogglePlatform();

		_bothWerePressed = bothPressed;

		if (_isOnMenuPlatform)
			LockCameraToMenuSpawn();
	}

	private void TogglePlatform()
	{
		if (_isOnMenuPlatform)
		{
			_playerRig.GlobalTransform = _lastMainTransform;
			SetMovementEnabled(true);
			_isOnMenuPlatform = false;
		}
		else
		{
			_lastMainTransform = _playerRig.GlobalTransform;

			_playerRig.GlobalTransform = _menuSpawnPoint.GlobalTransform;
			LockCameraToMenuSpawn();

			SetMovementEnabled(false);
			_isOnMenuPlatform = true;
		}
	}

	private void LockCameraToMenuSpawn()
	{
		Vector3 difference = _menuSpawnPoint.GlobalPosition - _camera.GlobalPosition;
		_playerRig.GlobalPosition += difference;
	}

	private void SetMovementEnabled(bool enabled)
	{
		_leftMovement.Set("enabled", enabled);
		_rightTurnMovement.Set("enabled", enabled);
		_rightJumpMovement.Set("enabled", enabled);
	}
}
