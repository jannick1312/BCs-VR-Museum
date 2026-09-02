using Godot;

namespace BCSVRMuseum.Player;

/// <summary>
/// Changes left-hand motion into virtual joystick movement.
/// </summary>
public partial class HandJoystickMovement : Node
{
	private Vector3 _baseLocal;
	private Basis _basis;
	private XRCamera3D _camera;
	private Node3D _handle;
	private Node3D _handMesh;
	private Node3D _joystickRoot;
	private float _length;
	private Node _movementDirect;
	private Node _movementTurn;
	private Node _playerBody;
	private Vector3 _scale = Vector3.One;
	private Node3D _stick;
	private Node3D _trackedHand;
	private float _visualLength;
	public bool IsLocked { get; private set; }

	/// <summary>
	/// Finds the joystick parts.
	/// </summary>
	public override void _Ready()
	{
		_joystickRoot = GetParent<Node3D>();
		_handle = (Node3D)_joystickRoot.FindChild("Handle", true, false);
		_stick = (Node3D)_joystickRoot.FindChild("Stick", true, false);
		_scale = _joystickRoot.Scale;
		_visualLength = _handle.Position.Length();
		_length = _visualLength * _scale.X;
	}

	/// <summary>
	/// Connects the joystick to player movement and hand tracking.
	/// </summary>
	/// <param name="player">The player node.</param>
	public void Configure(Node3D player)
	{
		_playerBody = player.FindChild("PlayerBody", true, false);
		_movementDirect = player.FindChild("MovementDirect", true, false);
		_movementTurn = player.FindChild("MovementTurn", true, false);
		_trackedHand = (Node3D)player.FindChild("LeftTrackedHand", true, false);
		_handMesh = (Node3D)player.FindChild("LeftHandTrackingMesh", true, false);
		_camera = (XRCamera3D)player.FindChild("XRCamera3D", true, false);
	}

	/// <summary>
	/// Updates the virtual joystick and applies movement while locked.
	/// </summary>
	/// <param name="delta">The physics frame time in seconds.</param>
	public void ProcessMovement(float delta)
	{
		if (!IsLocked)
			Start();

		if (!IsLocked)
			return;

		var (turnAmount, _, z) = UpdateJoystick();
		var forwardAmount = -z;
		var forward = _camera.GlobalTransform.Basis.Z * -1.0f;
		forward.Y = 0.0f;
		forward = forward.Normalized();
		var speed = _movementDirect.Get("max_speed").AsSingle();
		var turnSpeed = _movementTurn.Get("smooth_turn_speed").AsSingle();

		MovePlayer(forward * forwardAmount * speed, turnAmount * turnSpeed * delta);
	}

	/// <summary>
	/// Stops joystick movement and restores the hand visual.
	/// </summary>
	public void ForceStop()
	{
		Stop();
	}

	/// <summary>
	/// Locks the joystick below the hand.
	/// </summary>
	private void Start()
	{
		IsLocked = true;
		_handMesh.Visible = false;

		var handPosition = _trackedHand.Position;
		var forward = _camera.Transform.Basis.Z * -1.0f;
		forward.Y = 0.0f;
		forward = forward.Normalized();

		var right = _camera.Transform.Basis.X;
		right.Y = 0.0f;
		right = right.Normalized();

		_basis = new Basis(right, Vector3.Up, -forward).Orthonormalized();
		_baseLocal = handPosition - Vector3.Up * _length;

		_joystickRoot.Transform = new Transform3D(_basis, _baseLocal);
		_joystickRoot.Scale = _scale;
		_joystickRoot.Visible = true;
		SetVisual(Vector3.Up);
	}

	/// <summary>
	/// Hides the joystick.
	/// </summary>
	private void Stop()
	{
		if (!IsLocked)
			return;

		IsLocked = false;
		_handMesh.Visible = true;
		_joystickRoot.Visible = false;
	}

	/// <summary>
	/// Gets the hand direction inside the joystick.
	/// </summary>
	/// <returns>The local joystick direction with a length of 1.</returns>
	private Vector3 UpdateJoystick()
	{
		var rawDirection = _trackedHand.Position - _baseLocal;
		var direction = rawDirection.Length() > 0.001f ? rawDirection.Normalized() : Vector3.Up;
		if (direction.Dot(Vector3.Up) < 0.1f)
			direction = (direction + Vector3.Up * 0.35f).Normalized();

		var localDirection = (_basis.Inverse() * direction).Normalized();
		SetVisual(localDirection);
		return localDirection;
	}

	/// <summary>
	/// Positions and rotates the joystick handle and stick.
	/// </summary>
	/// <param name="localDirection">The joystick direction with a length of 1.</param>
	private void SetVisual(Vector3 localDirection)
	{
		var handlePosition = localDirection * _visualLength;
		_handle.Position = handlePosition;
		_stick.Position = handlePosition * 0.5f;
		_stick.Scale = new Vector3(1.0f, handlePosition.Length() / _visualLength, 1.0f);
		_stick.LookAt(_joystickRoot.ToGlobal(handlePosition), _joystickRoot.GlobalTransform.Basis.X);
		_stick.RotateObjectLocal(Vector3.Right, Mathf.Pi * 0.5f);
	}

	/// <summary>
	/// Sends translation and rotation commands to the player body.
	/// </summary>
	/// <param name="velocity">The requested movement velocity.</param>
	/// <param name="rotation">The requested yaw rotation.</param>
	private void MovePlayer(Vector3 velocity, float rotation)
	{
		_playerBody.Call("move_player", velocity);
		_playerBody.Call("rotate_player", rotation);
	}
}



// Codex helped implement the direction and rotation calculations in this file.
