using BCSVRMuseum.Museum_Scripts;
using BCSVRMuseum.Player.Hud;
using Godot;
using Logger;

namespace BCSVRMuseum;

public partial class PlatformSwitcher : Node
{
	private readonly EventLogger _logger = new(nameof(PlatformSwitcher));

	private CharacterBody3D _body;
	private XRCamera3D _camera;
	private MuseumEntryState _entryState;
	private bool _inMenu;
	private Transform3D _lastMuseumBody;
	private Transform3D _lastMuseumRig;
	private XRController3D _leftController;
	private Transform3D _lockedMenuRig;
	private Node3D _menu;
	private bool _menuButtonWasPressed;
	private Marker3D _menuSpawn;
	private Node[] _movementNodes;
	private Node3D _museum;
	private Marker3D _museumSpawn;
	private Node _player;
	private Node3D _rig;
	private XRController3D _rightController;
	private bool _switching;
	private WorldEnvironment _worldEnvironment;

	[Export] public Environment MenuEnvironment;
	[Export] public NodePath MenuNodePath;
	[Export] public Environment MuseumEnvironment;
	[Export] public NodePath MuseumNodePath;
	[Export] public NodePath PlayerPath;
	[Export] public NodePath WorldEnvironmentPath;

	public override async void _Ready()
	{
		_player = GetNode(PlayerPath);

		_rig = (Node3D)_player.FindChild("XROrigin3D", true, false);
		_camera = (XRCamera3D)_player.FindChild("XRCamera3D", true, false);

		_leftController = (XRController3D)_player.FindChild("LeftController", true, false);
		_rightController = (XRController3D)_player.FindChild("RightController", true, false);

		_body = (CharacterBody3D)_player.FindChild("PlayerBody", true, false);
		var searchSettingsStore = (SearchSettingsStore)GetTree().Root.FindChild("SearchSettingsStore", true, false);
		_entryState = searchSettingsStore.EntryState;
		_movementNodes = [_player.FindChild("MovementDirect", true, false), _player.FindChild("MovementTurn", true, false)];

		await this.WaitFor(() =>
		{
			foreach (var child in _body.GetChildren())
				if (child is CollisionShape3D collisionShape)
					return collisionShape;

			return null;
		}, "player body collision shape");

		_museum = GetNode<Node3D>(MuseumNodePath);
		_menu = GetNode<Node3D>(MenuNodePath);
		_museumSpawn = (Marker3D)_museum.FindChild("StartSpawnPoint", true, false);
		_menuSpawn = (Marker3D)_menu.FindChild("MenuSpawnPoint", true, false);
		_worldEnvironment = GetNode<WorldEnvironment>(WorldEnvironmentPath);

		MoveCameraTo(_museumSpawn);
		RememberMuseumTransform();

		SetMovementEnabled(false);
		SetEnabled(_body, false);
		SetWorld(false);
		MoveCameraTo(_menuSpawn);

		_body.GlobalTransform = _rig.GlobalTransform;
		_lockedMenuRig = _rig.GlobalTransform;
		_inMenu = true;
		_logger.Info("Platform switcher initialized in menu.");
	}

	public override void _Process(double delta)
	{
		if (_switching)
			return;

		var menuButtonPressed = _leftController.IsButtonPressed("primary_click");

		if (menuButtonPressed && !_menuButtonWasPressed)
			ToggleWorld();

		_menuButtonWasPressed = menuButtonPressed;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!_inMenu)
			return;

		_rig.GlobalTransform = _lockedMenuRig;
	}

	public async void SwitchToMuseum()
	{
		if (!_entryState.CanEnterMuseum)
		{
			_logger.Warning("Switch to museum ignored because the entry requirements are not met.");
			return;
		}

		_body.Velocity = Vector3.Zero;

		_switching = true;
		_inMenu = false;
		SetWorld(true);
		_rig.GlobalTransform = _lastMuseumRig;
		_body.GlobalTransform = _lastMuseumBody;

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

		_body.Velocity = Vector3.Zero;

		RememberMuseumTransform();
		SetMovementEnabled(false);
		SetEnabled(_body, false);
		SetWorld(false);
		MoveCameraTo(_menuSpawn);

		_body.GlobalTransform = _rig.GlobalTransform;
		_lockedMenuRig = _rig.GlobalTransform;
		_inMenu = true;
		_logger.Info("Switched from museum world to menu.");

		_switching = false;
	}

	public void ToggleWorld()
	{
		if (_switching)
			return;

		if (_inMenu)
			SwitchToMuseum();
		else
			SwitchToMenu();
	}

	private void RememberMuseumTransform()
	{
		_lastMuseumRig = _rig.GlobalTransform;
		_lastMuseumBody = _body.GlobalTransform;
	}

	private void SetWorld(bool museumActive)
	{
		_museum.Visible = museumActive;
		_menu.Visible = !museumActive;
		HudController.Instance?.SetMuseumVisible(museumActive);
		_worldEnvironment.Environment = museumActive ? MuseumEnvironment : MenuEnvironment;
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
}
