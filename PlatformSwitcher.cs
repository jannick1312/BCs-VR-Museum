using Godot;

public partial class PlatformSwitcher : Node
{
	[Export] public NodePath PlayerRigPath;
	[Export] public NodePath CameraPath;

	[Export] public NodePath MuseumNodePath;
	[Export] public NodePath MenuNodePath;
	[Export] public NodePath MenuSpawnPointPath;

	[Export] public NodePath WorldEnvironmentPath;
	[Export] public Environment MuseumEnvironment;
	[Export] public Environment MenuEnvironment;

	[Export] public NodePath LeftMovementPath;
	[Export] public NodePath RightTurnMovementPath;
	[Export] public NodePath RightJumpMovementPath;

	[Export] public NodePath LeftHandVisualPath;
	[Export] public NodePath RightHandVisualPath;

	private Node3D _playerRig;
	private XRCamera3D _camera;

	private Node3D _museumNode;
	private Node3D _menuNode;
	private Marker3D _menuSpawnPoint;

	private WorldEnvironment _worldEnvironment;

	private Node _leftMovement;
	private Node _rightTurnMovement;
	private Node _rightJumpMovement;

	private Node3D _leftHandVisual;
	private Node3D _rightHandVisual;

	private Transform3D _lastMuseumTransform;

	private bool _isInMenu = false;
	private bool _bothWerePressed = false;

	public override void _Ready()
	{
		_playerRig = GetNode<Node3D>(PlayerRigPath);
		_camera = GetNode<XRCamera3D>(CameraPath);

		_museumNode = GetNode<Node3D>(MuseumNodePath);
		_menuNode = GetNode<Node3D>(MenuNodePath);
		_menuSpawnPoint = GetNode<Marker3D>(MenuSpawnPointPath);

		_worldEnvironment = GetNode<WorldEnvironment>(WorldEnvironmentPath);

		_leftMovement = GetNode<Node>(LeftMovementPath);
		_rightTurnMovement = GetNode<Node>(RightTurnMovementPath);
		_rightJumpMovement = GetNode<Node>(RightJumpMovementPath);

		_leftHandVisual = GetNode<Node3D>(LeftHandVisualPath);
		_rightHandVisual = GetNode<Node3D>(RightHandVisualPath);

		_worldEnvironment.Environment = MuseumEnvironment;

		SetMuseumActive(true);
		SetMenuActive(false);
		SetMovementEnabled(true);
		SetHandVisualsVisible(true);
	}

	public override void _Process(double delta)
	{
		XRController3D left = GetTree().Root.FindChild("LeftController", true, false) as XRController3D;
		XRController3D right = GetTree().Root.FindChild("RightController", true, false) as XRController3D;

		if (left == null || right == null)
			return;

		bool bothPressed =
			left.GetFloat("trigger") > 0.75f &&
			right.GetFloat("trigger") > 0.75f;

		if (bothPressed && !_bothWerePressed)
			ToggleWorld();

		_bothWerePressed = bothPressed;

		if (_isInMenu)
			LockCameraToMenuSpawn();
	}

	private void ToggleWorld()
	{
		if (_isInMenu)
		{
			SetMenuActive(false);
			SetMuseumActive(true);

			_worldEnvironment.Environment = MuseumEnvironment;

			_playerRig.GlobalTransform = _lastMuseumTransform;

			SetMovementEnabled(true);
			SetHandVisualsVisible(true);

			_isInMenu = false;
		}
		else
		{
			_lastMuseumTransform = _playerRig.GlobalTransform;

			SetMuseumActive(false);
			SetMenuActive(true);

			_worldEnvironment.Environment = MenuEnvironment;

			_playerRig.GlobalTransform = _menuSpawnPoint.GlobalTransform;
			LockCameraToMenuSpawn();

			SetMovementEnabled(false);
			SetHandVisualsVisible(false);

			_isInMenu = true;
		}
	}

	private void LockCameraToMenuSpawn()
	{
		Vector3 difference = _menuSpawnPoint.GlobalPosition - _camera.GlobalPosition;
		_playerRig.GlobalPosition += difference;

		Vector3 rotation = _playerRig.GlobalRotation;
		rotation.Y = _menuSpawnPoint.GlobalRotation.Y;
		_playerRig.GlobalRotation = rotation;
	}

	private void SetMuseumActive(bool active)
	{
		_museumNode.Visible = active;
	}

	private void SetMenuActive(bool active)
	{
		_menuNode.Visible = active;
	}

	private void SetMovementEnabled(bool enabled)
	{
		_leftMovement.Set("enabled", enabled);
		_rightTurnMovement.Set("enabled", enabled);
		_rightJumpMovement.Set("enabled", enabled);
	}

	private void SetHandVisualsVisible(bool visible)
	{
		_leftHandVisual.Visible = visible;
		_rightHandVisual.Visible = visible;
	}
}
