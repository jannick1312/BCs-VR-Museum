using Godot;
using Server;

public partial class PlatformSwitcher : Node
{
	[Export] public NodePath PlayerPath;
	[Export] public NodePath MuseumNodePath;
	[Export] public NodePath MenuNodePath;
	[Export] public NodePath WorldEnvironmentPath;
	[Export] public Environment MuseumEnvironment;
	[Export] public Environment MenuEnvironment;

	private Node _player;
	private Node3D _playerRig;
	private XRCamera3D _camera;
	private XRController3D _leftController;
	private XRController3D _rightController;
	private Node3D _leftHandVisual;
	private Node3D _rightHandVisual;
	private Node3D _museumNode;
	private Node3D _menuNode;
	private Marker3D _menuSpawnPoint;
	private Marker3D _startSpawnPoint;
	private WorldEnvironment _worldEnvironment;
	private Node _leftMovement;
	private Node _rightTurnMovement;
	private Node _rightJumpMovement;

	private Transform3D _lastMuseumTransform;
	private Transform3D _lockedMenuTransform;
	private bool _isInMenu = false;
	private bool _bothWerePressed = false;

	public override async void _Ready()
	{
		for (int i = 0; i < 8; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		_player = GetNode(PlayerPath);

		_playerRig = _player.FindChild("XROrigin3D", true, false) as Node3D;
		_camera = _player.FindChild("XRCamera3D", true, false) as XRCamera3D;

		_leftController = _player.FindChild("LeftController", true, false) as XRController3D;
		_rightController = _player.FindChild("RightController", true, false) as XRController3D;

		_leftHandVisual = _player.FindChild("LeftHand2", true, false) as Node3D;
		_rightHandVisual = _player.FindChild("RightHand2", true, false) as Node3D;

		_leftMovement = _player.FindChild("MovementDirect", true, false);
		_rightTurnMovement = _player.FindChild("MovementTurn", true, false);
		_rightJumpMovement = _player.FindChild("MovementJump", true, false);

		_museumNode = GetNode<Node3D>(MuseumNodePath);
		_menuNode = GetNode<Node3D>(MenuNodePath);

		_menuSpawnPoint = _menuNode.FindChild("MenuSpawnPoint", true, false) as Marker3D;
		_startSpawnPoint = _museumNode.FindChild("StartSpawnPoint", true, false) as Marker3D;

		_worldEnvironment = GetNode<WorldEnvironment>(WorldEnvironmentPath);

		_worldEnvironment.Environment = MuseumEnvironment;

		SetMuseumActive(true);
		SetMenuActive(false);
		SetMovementEnabled(true);

		var c = new HelloWorld();

		MoveCameraExactlyToStartSpawn();
		_lastMuseumTransform = _playerRig.GlobalTransform;
	}

	public override void _Process(double delta)
	{
		if (_leftController == null || _rightController == null)
			return;
		
		bool bothPressed =
			_leftController.GetFloat("trigger") > 0.75f &&
			_rightController.GetFloat("trigger") > 0.75f;

		if (bothPressed && !_bothWerePressed)
			ToggleWorld();

		_bothWerePressed = bothPressed;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_isInMenu)
			return;

		_playerRig.GlobalTransform = _lockedMenuTransform;

		if (_leftHandVisual != null && _leftController != null)
			_leftHandVisual.GlobalTransform = _leftController.GlobalTransform;

		if (_rightHandVisual != null && _rightController != null)
			_rightHandVisual.GlobalTransform = _rightController.GlobalTransform;
	}

	private void ToggleWorld()
	{
		if (_isInMenu)
		{
			_isInMenu = false;

			SetMenuActive(false);
			SetMuseumActive(true);

			_worldEnvironment.Environment = MuseumEnvironment;

			_playerRig.GlobalTransform = _lastMuseumTransform;

			SetMovementEnabled(true);
		}
		else
		{
			_lastMuseumTransform = _playerRig.GlobalTransform;

			SetMovementEnabled(false);

			SetMuseumActive(false);
			SetMenuActive(true);

			_worldEnvironment.Environment = MenuEnvironment;

			MoveCameraExactlyToMenuSpawn();

			_lockedMenuTransform = _playerRig.GlobalTransform;

			_isInMenu = true;
		}
	}

	private void MoveCameraExactlyToStartSpawn()
	{
		if (_startSpawnPoint == null || _camera == null || _playerRig == null)
			return;

		MoveCameraExactlyToMarker(_startSpawnPoint);
	}

	private void MoveCameraExactlyToMenuSpawn()
	{
		MoveCameraExactlyToMarker(_menuSpawnPoint);
	}

	private void MoveCameraExactlyToMarker(Marker3D marker)
	{
		Vector3 cameraOffset = _camera.GlobalPosition - _playerRig.GlobalPosition;
		_playerRig.GlobalPosition = marker.GlobalPosition - cameraOffset;

		Vector3 cameraForward = -_camera.GlobalTransform.Basis.Z;
		cameraForward.Y = 0;

		cameraForward = cameraForward.Normalized();

		Vector3 targetForward = Vector3.Forward;
		targetForward.Y = 0;
		targetForward = targetForward.Normalized();

		float angle = cameraForward.SignedAngleTo(targetForward, Vector3.Up);

		_playerRig.RotateY(angle);

		cameraOffset = _camera.GlobalPosition - _playerRig.GlobalPosition;
		_playerRig.GlobalPosition = marker.GlobalPosition - cameraOffset;
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
		SetNodeEnabled(_leftMovement, enabled);
		SetNodeEnabled(_rightTurnMovement, enabled);
		SetNodeEnabled(_rightJumpMovement, enabled);
	}

	private void SetNodeEnabled(Node node, bool enabled)
	{
		node.Set("enabled", enabled);

		node.ProcessMode = enabled
			? Node.ProcessModeEnum.Inherit
			: Node.ProcessModeEnum.Disabled;
	}
}
