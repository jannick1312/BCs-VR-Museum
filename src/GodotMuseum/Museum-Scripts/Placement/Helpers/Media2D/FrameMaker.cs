using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D;

public partial class FrameMaker : Node
{
	private static readonly EventLogger Log = new(nameof(FrameMaker));

	private Node3D _displayInstance;
	private Node3D _frame;
	private Node3D _grabLeft;
	private Node3D _grabRight;
	private MeshInstance3D _top;
	private MeshInstance3D _bottom;
	private MeshInstance3D _left;
	private MeshInstance3D _right;
	private MeshInstance3D _topL;
	private MeshInstance3D _topR;
	private MeshInstance3D _bottomL;
	private MeshInstance3D _bottomR;

	private CollisionShape3D _collision;

	public override void _Ready()
	{
		_displayInstance = GetParent<Node3D>();

		_frame = _displayInstance.GetNode<Node3D>("Frame");

		_collision = _displayInstance.GetNode<CollisionShape3D>("CollisionShape3D");
		_collision.Shape = (Shape3D)_collision.Shape.Duplicate();

		_grabLeft = _displayInstance.GetNode<Node3D>("GrabPointHandLeft");
		_grabRight = _displayInstance.GetNode<Node3D>("GrabPointHandRight");
		_top = _frame.GetNode<MeshInstance3D>("Top");
		_bottom = _frame.GetNode<MeshInstance3D>("Bottom");
		_left = _frame.GetNode<MeshInstance3D>("Left");
		_right = _frame.GetNode<MeshInstance3D>("Right");
		_topL = _frame.GetNode<MeshInstance3D>("TopL");
		_topR = _frame.GetNode<MeshInstance3D>("TopR");
		_bottomL = _frame.GetNode<MeshInstance3D>("BottomL");
		_bottomR = _frame.GetNode<MeshInstance3D>("BottomR");
	}

	public void UpdateFrame(MeshInstance3D picture, float imageWidth, float imageHeight)
	{
		var center = picture.GlobalPosition;
		var basis = picture.GlobalTransform.Basis.Orthonormalized();
		var right = basis.X.Normalized();
		var up = basis.Y.Normalized();
		var forward = basis.Z.Normalized();
		var frameBasis = new Basis(right, forward, -up).Orthonormalized();

		_frame.GlobalTransform = new Transform3D(frameBasis, center);

		var halfW = imageWidth / 2.0f;
		var halfH = imageHeight / 2.0f;

		var tl = new Vector3(-halfW, 0, -halfH);
		var tr = new Vector3(halfW, 0, -halfH);
		var bl = new Vector3(-halfW, 0, halfH);
		var br = new Vector3(halfW, 0, halfH);

		PlaceCornerLocal(_topL, tl, true, false);
		PlaceCornerLocal(_topR, tr, false, false);
		PlaceCornerLocal(_bottomL, bl, true, true);
		PlaceCornerLocal(_bottomR, br, false, true);

		PlaceHLocal(_top, _topL, _topR);
		PlaceHLocal(_bottom, _bottomL, _bottomR);

		PlaceVLocal(_left, _topL, _bottomL);
		PlaceVLocal(_right, _topR, _bottomR);

		UpdateCollision(center, basis, imageWidth, imageHeight);
		UpdateGrabPointsLocal();
	}

	private static void PlaceCornerLocal(MeshInstance3D corner, Vector3 target, bool left, bool bottom)
	{
		corner.Position = target;

		var aabb = corner.GetAabb();

		var innerX = corner.Position.X + (left ? aabb.End.X : aabb.Position.X);
		var innerZ = corner.Position.Z + (bottom ? aabb.Position.Z : aabb.End.Z);

		var correction = target - new Vector3(innerX, target.Y, innerZ);

		corner.Position += correction;
	}

	private static void PlaceHLocal(MeshInstance3D mesh, MeshInstance3D leftCorner, MeshInstance3D rightCorner)
	{
		var l = leftCorner.GetAabb();
		var r = rightCorner.GetAabb();
		var start = leftCorner.Position.X + l.End.X;
		var end = rightCorner.Position.X + r.Position.X;
		var len = end - start;

		mesh.Position = new Vector3((start + end) / 2.0f, 0, leftCorner.Position.Z);

		var baseLength = Mathf.Abs(mesh.GetAabb().Size.X);

		if (!Mathf.IsZeroApprox(baseLength))
			mesh.Scale = new Vector3(len / baseLength, mesh.Scale.Y, mesh.Scale.Z);
		else
			Log.Warning($"Horizontal frame mesh '{mesh.Name}' has zero base length.");
	}

	private static void PlaceVLocal(MeshInstance3D mesh, MeshInstance3D topCorner, MeshInstance3D bottomCorner)
	{
		var t = topCorner.GetAabb();
		var b = bottomCorner.GetAabb();
		var start = topCorner.Position.Z + t.End.Z;
		var end = bottomCorner.Position.Z + b.Position.Z;
		var len = end - start;

		mesh.Position = new Vector3(topCorner.Position.X, 0, (start + end) / 2.0f);

		var baseLength = Mathf.Abs(mesh.GetAabb().Size.Z);

		if (!Mathf.IsZeroApprox(baseLength))
			mesh.Scale = new Vector3(mesh.Scale.X, mesh.Scale.Y, len / baseLength);
		else
			Log.Warning($"Vertical frame mesh '{mesh.Name}' has zero base length.");
	}

	private void UpdateCollision(Vector3 center, Basis basis, float imageWidth, float imageHeight)
	{
		var box = (BoxShape3D)_collision.Shape;

		box.Size = new Vector3(imageWidth + 0.2f, imageHeight + 0.2f, 0.04f);
		_collision.GlobalTransform = new Transform3D(basis, center);
	}

	private void UpdateGrabPointsLocal()
	{
		var l = _left.GetAabb();
		var r = _right.GetAabb();
		var lc = _left.Position + l.GetCenter();
		var rc = _right.Position + r.GetCenter();

		_grabLeft.GlobalPosition = _frame.ToGlobal(lc + new Vector3(0, -0.08f, 0));
		_grabRight.GlobalPosition = _frame.ToGlobal(rc + new Vector3(0, -0.08f, 0));
	}
}