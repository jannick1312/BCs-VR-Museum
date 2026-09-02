using BCSVRMuseum.Museum_Scripts;
using BCSVRMuseum.Player.Hud;
using Godot;
using Logger;

namespace BCSVRMuseum;

/// <summary>
/// Switches the player between the menu and museum worlds.
/// </summary>
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

	/// <summary>
	/// Finds the needed scene nodes and places the player in the menu.
	/// </summary>
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

	/// <summary>
	/// Checks the menu button and switches between menu and museum.
	/// </summary>
	/// <param name="delta">The frame time in seconds.</param>
	public override void _Process(double delta)
	{
		if (_switching)
			return;

		var menuButtonPressed = _leftController.IsButtonPressed("primary_click");

		if (menuButtonPressed && !_menuButtonWasPressed)
			ToggleWorld();

		_menuButtonWasPressed = menuButtonPressed;
	}

	/// <summary>
	/// Keeps the player rig fixed while the menu is active.
	/// </summary>
	/// <param name="delta">The physics frame time in seconds.</param>
	public override void _PhysicsProcess(double delta)
	{
		if (!_inMenu)
			return;

		_rig.GlobalTransform = _lockedMenuRig;
	}

	/// <summary>
	/// Moves the player into the museum when entry requirements are met.
	/// </summary>
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

	/// <summary>
	/// Saves the museum position and moves the player into the menu.
	/// </summary>
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

	/// <summary>
	/// Switches between the menu and museum worlds.
	/// </summary>
	public void ToggleWorld()
	{
		if (_switching)
			return;

		if (_inMenu)
			SwitchToMuseum();
		else
			SwitchToMenu();
	}

	/// <summary>
	/// Saves the player position and rotation for the return to the museum.
	/// </summary>
	private void RememberMuseumTransform()
	{
		_lastMuseumRig = _rig.GlobalTransform;
		_lastMuseumBody = _body.GlobalTransform;
	}

	/// <summary>
	/// Shows the selected world and uses its lighting settings.
	/// </summary>
	/// <param name="museumActive">If the museum world should be active.</param>
	private void SetWorld(bool museumActive)
	{
		_museum.Visible = museumActive;
		_menu.Visible = !museumActive;
		HudController.Instance?.SetMuseumVisible(museumActive);
		_worldEnvironment.Environment = museumActive ? MuseumEnvironment : MenuEnvironment;
	}

	/// <summary>
	/// Moves and turns the camera rig to match a spawn marker.
	/// </summary>
	/// <param name="marker">The target camera marker.</param>
	private void MoveCameraTo(Marker3D marker)
	{
		_rig.GlobalPosition = marker.GlobalPosition - (_camera.GlobalPosition - _rig.GlobalPosition);

		var cameraForward = Flatten(-_camera.GlobalTransform.Basis.Z);
		var targetForward = Flatten(-marker.GlobalTransform.Basis.Z);

		_rig.RotateY(cameraForward.SignedAngleTo(targetForward, Vector3.Up));
		_rig.GlobalPosition = marker.GlobalPosition - (_camera.GlobalPosition - _rig.GlobalPosition);
	}

	/// <summary>
	/// Removes the vertical part of a direction and sets its length to 1.
	/// </summary>
	/// <param name="vector">The direction to flatten.</param>
	/// <returns>The horizontal direction with a length of 1.</returns>
	private static Vector3 Flatten(Vector3 vector)
	{
		vector.Y = 0;
		return vector.Normalized();
	}

	/// <summary>
	/// Turns all player movement parts on or off.
	/// </summary>
	/// <param name="enabled">If movement should be enabled.</param>
	private void SetMovementEnabled(bool enabled)
	{
		foreach (var node in _movementNodes)
			SetEnabled(node, enabled);
	}

	/// <summary>
	/// Turns a node on or off.
	/// </summary>
	/// <param name="node">The node to update.</param>
	/// <param name="enabled">If the node should be enabled.</param>
	private static void SetEnabled(Node node, bool enabled)
	{
		node.Set("enabled", enabled);
		node.ProcessMode = enabled ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
	}
}
