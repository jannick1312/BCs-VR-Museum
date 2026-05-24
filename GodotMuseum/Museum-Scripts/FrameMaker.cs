using Godot;

public partial class FrameMaker : Node
{
	private Node3D _outputFrame;
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
		_outputFrame = GetParent<Node3D>();

		_frame = _outputFrame.GetNode<Node3D>("Frame");

		_collision = _outputFrame.GetNode<CollisionShape3D>("CollisionShape3D");
		_grabLeft = _outputFrame.GetNode<Node3D>("GrabPointHandLeft");
		_grabRight = _outputFrame.GetNode<Node3D>("GrabPointHandRight");

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
		Vector3 center = picture.GlobalPosition;

		Basis basis = picture.GlobalTransform.Basis.Orthonormalized();

		Vector3 right = basis.X.Normalized();
		Vector3 up = basis.Y.Normalized();
		Vector3 forward = basis.Z.Normalized();

		Basis frameBasis = new Basis(
			right,
			forward,
			-up
		).Orthonormalized();

		_frame.GlobalTransform = new Transform3D(frameBasis, center);

		float halfW = imageWidth / 2.0f;
		float halfH = imageHeight / 2.0f;

		Vector3 tl = new Vector3(-halfW, 0, -halfH);
		Vector3 tr = new Vector3( halfW, 0, -halfH);
		Vector3 bl = new Vector3(-halfW, 0,  halfH);
		Vector3 br = new Vector3( halfW, 0,  halfH);

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

	private void PlaceCornerLocal(MeshInstance3D corner, Vector3 target, bool left, bool bottom)
	{
		corner.Position = target;

		Aabb aabb = corner.GetAabb();

		float innerX = corner.Position.X + (left ? aabb.End.X : aabb.Position.X);
		float innerZ = corner.Position.Z + (bottom ? aabb.Position.Z : aabb.End.Z);

		Vector3 correction = target - new Vector3(innerX, target.Y, innerZ);

		corner.Position += correction;
	}

	private void PlaceHLocal(MeshInstance3D mesh, MeshInstance3D leftCorner, MeshInstance3D rightCorner)
	{
		Aabb l = leftCorner.GetAabb();
		Aabb r = rightCorner.GetAabb();

		float start = leftCorner.Position.X + l.End.X;
		float end = rightCorner.Position.X + r.Position.X;
		float len = Mathf.Max(0.001f, end - start);

		mesh.Position = new Vector3(
			(start + end) / 2.0f,
			0,
			leftCorner.Position.Z
		);

		float baseLength = Mathf.Abs(mesh.GetAabb().Size.X);

		if (!Mathf.IsZeroApprox(baseLength))
			mesh.Scale = new Vector3(len / baseLength, mesh.Scale.Y, mesh.Scale.Z);
	}

	private void PlaceVLocal(MeshInstance3D mesh, MeshInstance3D topCorner, MeshInstance3D bottomCorner)
	{
		Aabb t = topCorner.GetAabb();
		Aabb b = bottomCorner.GetAabb();

		float start = topCorner.Position.Z + t.End.Z;
		float end = bottomCorner.Position.Z + b.Position.Z;
		float len = Mathf.Max(0.001f, end - start);

		mesh.Position = new Vector3(
			topCorner.Position.X,
			0,
			(start + end) / 2.0f
		);

		float baseLength = Mathf.Abs(mesh.GetAabb().Size.Z);

		if (!Mathf.IsZeroApprox(baseLength))
			mesh.Scale = new Vector3(mesh.Scale.X, mesh.Scale.Y, len / baseLength);
	}

	private void UpdateCollision(Vector3 center, Basis basis, float imageWidth, float imageHeight)
	{
		if (_collision.Shape is not BoxShape3D box)
			return;

		box.Size = new Vector3(imageWidth + 0.2f, imageHeight + 0.2f, 0.04f);
		_collision.GlobalTransform = new Transform3D(basis, center);
	}

	private void UpdateGrabPointsLocal()
	{
		Aabb l = _left.GetAabb();
		Aabb r = _right.GetAabb();

		Vector3 lc = _left.Position + l.GetCenter();
		Vector3 rc = _right.Position + r.GetCenter();

		_grabLeft.GlobalPosition = _frame.ToGlobal(lc + new Vector3(0, -0.08f, 0));
		_grabRight.GlobalPosition = _frame.ToGlobal(rc + new Vector3(0, -0.08f, 0));
	}
}