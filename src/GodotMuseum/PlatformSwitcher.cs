using BCSVRMuseum.Museum_Scripts;
using Godot;
using Logger;

namespace BCSVRMuseum;

public partial class PlatformSwitcher : Node
{
	private readonly EventLogger _logger = new(nameof(PlatformSwitcher));

	[Export] public NodePath PlayerPath;
	[Export] public NodePath MuseumNodePath;
	[Export] public NodePath MenuNodePath;
	[Export] public NodePath WorldEnvironmentPath;
	[Export] public Environment MuseumEnvironment;
	[Export] public Environment MenuEnvironment;

	private Node _player;
	private Node3D _rig;
	private XRCamera3D _camera;
	private XRController3D _leftController;
	private XRController3D _rightController;
	private Node3D _leftHand;
	private Node3D _rightHand;
	private Node3D _museum;
	private Node3D _menu;
	private Marker3D _museumSpawn;
	private Marker3D _menuSpawn;
	private WorldEnvironment _worldEnvironment;
	private CharacterBody3D _body;
	private Node[] _movementNodes;

	private Transform3D _lastMuseumRig;
	private Transform3D _lastMuseumBody;
	private Transform3D _lockedMenuRig;
	private bool _inMenu;
	private bool _menuButtonWasPressed;
	private bool _switching;

	public override async void _Ready()
	{
		_player = GetNodeOrNull(PlayerPath);

		if (_player == null)
		{
			_logger.Error("Player node is missing. Platform switching cannot initialize.");
			return;
		}

		_rig = _player.FindChild("XROrigin3D", true, false) as Node3D;
		_camera = _player.FindChild("XRCamera3D", true, false) as XRCamera3D;

		_leftController = _player.FindChild("LeftController", true, false) as XRController3D;
		_rightController = _player.FindChild("RightController", true, false) as XRController3D;

		_leftHand = _player.FindChild("LeftHand", true, false) as Node3D;
		_rightHand = _player.FindChild("RightHand", true, false) as Node3D;

		_body = _player.FindChild("PlayerBody", true, false) as CharacterBody3D;
		_movementNodes = [_player.FindChild("MovementDirect", true, false), _player.FindChild("MovementTurn", true, false), _player.FindChild("MovementJump", true, false)];

		await this.WaitFor(() =>
		{
			foreach (var child in _body.GetChildren())
			{
				if (child is CollisionShape3D collisionShape)
					return collisionShape;
			}
			return null;
		}, "player body collision shape");	

		_museum = GetNode<Node3D>(MuseumNodePath);
		_menu = GetNode<Node3D>(MenuNodePath);
		_museumSpawn = _museum.FindChild("StartSpawnPoint", true, false) as Marker3D;
		_menuSpawn = _menu.FindChild("MenuSpawnPoint", true, false) as Marker3D;
		_worldEnvironment = GetNode<WorldEnvironment>(WorldEnvironmentPath);

		MoveCameraTo(_museumSpawn);
		RememberMuseumTransform();

		SetMovementEnabled(false);
		SetEnabled(_body, false);
		SetWorld(false);
		MoveCameraTo(_menuSpawn);

		if (_body != null && _rig != null)
			_body.GlobalTransform = _rig.GlobalTransform;

		_lockedMenuRig = _rig.GlobalTransform;
		_inMenu = true;
		_logger.Info("Platform switcher initialized in menu.");
	}

	public override void _Process(double delta)
	{
		if (_switching || _leftController == null)
			return;

		var menuButtonPressed = _leftController.IsButtonPressed("menu_button");

		if (menuButtonPressed && !_menuButtonWasPressed)
			ToggleWorld();

		_menuButtonWasPressed = menuButtonPressed;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_inMenu)
			return;

		_rig?.GlobalTransform = _lockedMenuRig;

		LockHands();
	}

	public async void SwitchToMuseum()
	{
		_switching = true;

		_body?.Velocity = Vector3.Zero;

		_inMenu = false;
		SetWorld(true);
		_rig.GlobalTransform = _lastMuseumRig;
		_body?.GlobalTransform = _lastMuseumBody;

		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		SetEnabled(_body, true);
		SetMovementEnabled(true);
		_logger.Info("Switched from menu to museum world.");

		_switching = false;
	}

	private void SwitchToMenu()
	{
		if (_inMenu || _switching)
			return;

		_switching = true;

		_body?.Velocity = Vector3.Zero;

		RememberMuseumTransform();
		SetMovementEnabled(false);
		SetEnabled(_body, false);
		SetWorld(false);
		MoveCameraTo(_menuSpawn);
		
		if (_body != null && _rig != null)
			_body.GlobalTransform = _rig.GlobalTransform;
		_lockedMenuRig = _rig.GlobalTransform;
		_inMenu = true;
		_logger.Info("Switched from museum world to menu.");

		_switching = false;
	}

	private void ToggleWorld()
	{
		if (_inMenu)
			SwitchToMuseum();
		else
			SwitchToMenu();
	}

	private void RememberMuseumTransform()
	{
		if (_rig != null)
			_lastMuseumRig = _rig.GlobalTransform;
		if (_body != null)
			_lastMuseumBody = _body.GlobalTransform;
	}

	private void SetWorld(bool museumActive)
	{
		_museum?.Visible = museumActive;
		_menu?.Visible = !museumActive;
		_worldEnvironment?.Environment = museumActive ? MuseumEnvironment : MenuEnvironment;
	}

	private void MoveCameraTo(Marker3D marker)
	{
		_rig.GlobalPosition = marker.GlobalPosition - (_camera.GlobalPosition - _rig.GlobalPosition);

		var cameraForward = Flatten(-_camera.GlobalTransform.Basis.Z);
		var targetForward = Flatten(-marker.GlobalTransform.Basis.Z);

		_rig.RotateY(cameraForward.SignedAngleTo(targetForward, Vector3.Up));
		_rig.GlobalPosition = marker.GlobalPosition - (_camera.GlobalPosition - _rig.GlobalPosition);
	}

	private static Vector3 Flatten(Vector3 vector)
	{
		vector.Y = 0;
		return vector.Normalized();
	}

	private void SetMovementEnabled(bool enabled)
	{
		foreach (var node in _movementNodes)
			SetEnabled(node, enabled);
	}

	private static void SetEnabled(Node node, bool enabled)
	{
		node.Set("enabled", enabled);
		node.ProcessMode = enabled ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
	}

	private void LockHands()
	{
		if (_leftHand != null && _leftController != null)
			_leftHand.GlobalTransform = _leftController.GlobalTransform;
		if (_rightHand != null && _rightController != null)
			_rightHand.GlobalTransform = _rightController.GlobalTransform;
	}
}