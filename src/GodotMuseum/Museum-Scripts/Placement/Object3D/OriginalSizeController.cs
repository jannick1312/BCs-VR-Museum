using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Object3D;
using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts.Placement.Object3D;

/// <summary>
/// Moves 3D models into separate rooms for original-size viewing.
/// </summary>
public partial class OriginalSizeController : Node
{
	private static readonly EventLogger Log = new(nameof(OriginalSizeController));
	private Object3DDisplayInstance _activeInstance;
	private CharacterBody3D _body;
	private XRCamera3D _camera;
	private MeshInstance3D _largeMax;
	private Node3D _largeOrigin;
	private Node3D _largeRoom;
	private Transform3D _museumReturnBodyTransform;
	private Node3D _rig;
	private MeshInstance3D _smallMax;
	private MeshInstance3D _smallMin;
	private Node3D _smallOrigin;
	private Node3D _smallRoom;

	[Export] public NodePath PlayerPath;
	[Export] public NodePath RoomsPath;

	public bool IsInOriginalSizeRoom => _activeInstance != null;

	/// <summary>
	/// Finds the player, viewing rooms, and room size markers.
	/// </summary>
	public override void _Ready()
	{
		var player = GetNode<Node>(PlayerPath);
		_rig = (Node3D)player.FindChild("XROrigin3D", true, false);
		_camera = (XRCamera3D)player.FindChild("XRCamera3D", true, false);
		_body = (CharacterBody3D)player.FindChild("PlayerBody", true, false);

		var rooms = GetNode<Node3D>(RoomsPath);
		_smallRoom = rooms.GetNode<Node3D>("ObjectRoomSmall");
		_largeRoom = rooms.GetNode<Node3D>("ObjectRoomLarge");
		_smallOrigin = _smallRoom.GetNode<Node3D>("ObjectOrigin");
		_largeOrigin = _largeRoom.GetNode<Node3D>("ObjectOrigin");
		_smallMin = _smallRoom.GetNode<MeshInstance3D>("Min");
		_smallMax = _smallRoom.GetNode<MeshInstance3D>("Max");
		_largeMax = _largeRoom.GetNode<MeshInstance3D>("Max");

		SetRoomActive(_smallRoom, false);
		SetRoomActive(_largeRoom, false);
	}

	/// <summary>
	/// Closes any active original-size display.
	/// </summary>
	public void Reset()
	{
		if (IsInOriginalSizeRoom)
			ReturnToMuseum();
		else
			RestoreToDisplay();
	}

	/// <summary>
	/// Returns the 3D model and disables the viewing rooms.
	/// </summary>
	private void RestoreToDisplay()
	{
		_activeInstance?.RestoreToDisplay();
		_activeInstance = null;

		if (_smallRoom != null)
			SetRoomActive(_smallRoom, false);
		if (_largeRoom != null)
			SetRoomActive(_largeRoom, false);
	}

	/// <summary>
	/// Returns the 3D model and moves the player to the museum.
	/// </summary>
	public void ReturnToMuseum()
	{
		var returnTransform = _museumReturnBodyTransform;
		RestoreToDisplay();
		_body.Velocity = Vector3.Zero;
		_body.Call("teleport", returnTransform);
		Log.Info("Returned from original-size room to museum.");
	}

	/// <summary>
	/// Places a 3D model in a room where it fits and moves the player there.
	/// </summary>
	/// <param name="instance">The 3D model display to present at original size.</param>
	public void ShowOriginalSize(Object3DDisplayInstance instance)
	{
		if (instance.ObjectNode == null || !IsInstanceValid(instance.ObjectNode))
		{
			Log.Warning("Original-size display cannot use an invalid 3D object node.");
			return;
		}

		var useSmallRoom = FitsInSmallRoom(instance);
		var room = useSmallRoom ? _smallRoom : _largeRoom;
		var origin = useSmallRoom ? _smallOrigin : _largeOrigin;
		var maximum = useSmallRoom ? _smallMax : _largeMax;

		_museumReturnBodyTransform = _body.GlobalTransform;
		SetRoomActive(_smallRoom, useSmallRoom);
		SetRoomActive(_largeRoom, !useSmallRoom);
		MoveObjectToRoom(instance, origin, maximum, useSmallRoom);
		TeleportPlayer(room.GetNode<Marker3D>("EntryPoint"));

		_activeInstance = instance;
		Log.Info($"Showing 3D object in original-size room. Room='{room.Name}', Scale={origin.Scale.X}.");
	}

	/// <summary>
	/// Checks if a 3D model's original bounds fit inside the small viewing room.
	/// </summary>
	/// <param name="instance">The 3D model display to evaluate.</param>
	/// <returns><see langword="true"/> if the 3D model fits without scaling and <see langword="false"/> otherwise.</returns>
	private bool FitsInSmallRoom(Object3DDisplayInstance instance)
	{
		var maximum = PlacementBounds.ScaledMeshBounds(_smallMax).Size;
		return OriginalSizeFitter.Fits(instance.OriginalBounds, maximum);
	}

	/// <summary>
	/// Moves a 3D model into the selected room and fits it inside.
	/// </summary>
	/// <param name="instance">The 3D model display being moved.</param>
	/// <param name="origin">The room node receiving the 3D model.</param>
	/// <param name="maximum">The marker defining the room's maximum model size.</param>
	/// <param name="applyMinimum">If the small room's minimum display size should be used.</param>
	private void MoveObjectToRoom(Object3DDisplayInstance instance, Node3D origin, MeshInstance3D maximum, bool applyMinimum)
	{
		PrepareObject(instance, origin);
		var bounds = instance.OriginalBounds;
		var maxSize = PlacementBounds.ScaledMeshBounds(maximum).Size;
		var minSize = applyMinimum ? PlacementBounds.ScaledMeshBounds(_smallMin).Size : (Vector3?)null;
		var (scale, position) = OriginalSizeFitter.Calculate(bounds, maxSize, minSize);

		origin.Scale = Vector3.One * scale;
		origin.Position = position;
	}

	/// <summary>
	/// Moves a 3D model to the room and restores its original transform.
	/// </summary>
	/// <param name="instance">The 3D model display being moved.</param>
	/// <param name="origin">The room node receiving the 3D model.</param>
	private static void PrepareObject(Object3DDisplayInstance instance, Node3D origin)
	{
		origin.Transform = Transform3D.Identity;
		instance.ObjectNode.Reparent(origin, false);
		instance.ObjectNode.Transform = instance.OriginalObjectTransform;
	}

	/// <summary>
	/// Moves the player to an entry marker.
	/// </summary>
	/// <param name="marker">The target entry marker.</param>
	private void TeleportPlayer(Marker3D marker)
	{
		_body.Velocity = Vector3.Zero;

		var currentForward = Flatten(-_camera.GlobalTransform.Basis.Z);
		var targetForward = Flatten(-marker.GlobalTransform.Basis.X);
		var angle = currentForward.SignedAngleTo(targetForward, Vector3.Up);
		var rotation = new Basis(Vector3.Up, angle);
		var cameraOffset = _camera.GlobalPosition - _rig.GlobalPosition;
		var targetRig = _rig.GlobalTransform;
		targetRig.Basis = rotation * targetRig.Basis;
		targetRig.Origin = marker.GlobalPosition - rotation * cameraOffset;

		var delta = targetRig * _rig.GlobalTransform.AffineInverse();
		_body.Call("teleport", delta * _body.GlobalTransform);
	}

	/// <summary>
	/// Removes the vertical part of a direction.
	/// </summary>
	/// <param name="vector">The direction to flatten.</param>
	/// <returns>The horizontal direction with a length of 1.</returns>
	private static Vector3 Flatten(Vector3 vector)
	{
		vector.Y = 0.0f;
		return vector.Normalized();
	}

	/// <summary>
	/// Updates a viewing room's visibility, interaction, and size markers.
	/// </summary>
	/// <param name="room">The viewing room to update.</param>
	/// <param name="active">If the room should be active.</param>
	private static void SetRoomActive(Node3D room, bool active)
	{
		NodeTreeActivator.SetActive(room, active);
		room.GetNodeOrNull<MeshInstance3D>("Min")?.Set(Node3D.PropertyName.Visible, false);
		room.GetNodeOrNull<MeshInstance3D>("Max")?.Set(Node3D.PropertyName.Visible, false);
	}
}



// All calculations in this file were implemented with the assistance of Codex.
