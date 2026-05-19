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
		Vector3 c = picture.GlobalPosition;

		Vector3 tl = new Vector3(c.X - imageWidth / 2.0f, c.Y + imageHeight / 2.0f, c.Z);
		Vector3 tr = new Vector3(c.X + imageWidth / 2.0f, c.Y + imageHeight / 2.0f, c.Z);
		Vector3 bl = new Vector3(c.X - imageWidth / 2.0f, c.Y - imageHeight / 2.0f, c.Z);
		Vector3 br = new Vector3(c.X + imageWidth / 2.0f, c.Y - imageHeight / 2.0f, c.Z);

		PlaceCorner(_topL, tl, true, false);
		PlaceCorner(_topR, tr, false, false);
		PlaceCorner(_bottomL, bl, true, true);
		PlaceCorner(_bottomR, br, false, true);

		PlaceH(_top, _topL, _topR);
		PlaceH(_bottom, _bottomL, _bottomR);

		PlaceV(_left, _topL, _bottomL);
		PlaceV(_right, _topR, _bottomR);

		UpdateCollision(c, imageWidth, imageHeight);
		UpdateGrabPoints();
	}

	private void PlaceCorner(MeshInstance3D corner, Vector3 target, bool left, bool bottom)
	{
		corner.GlobalPosition = target;

		Aabb aabb = GlobalAabb(corner);

		float innerX = left ? aabb.End.X : aabb.Position.X;
		float innerY = bottom ? aabb.End.Y : aabb.Position.Y;

		corner.GlobalPosition += target - new Vector3(innerX, innerY, target.Z);
	}

	private void PlaceH(MeshInstance3D mesh, MeshInstance3D leftCorner, MeshInstance3D rightCorner)
	{
		Aabb l = GlobalAabb(leftCorner);
		Aabb r = GlobalAabb(rightCorner);

		float start = l.End.X;
		float end = r.Position.X;
		float len = Mathf.Max(0.001f, end - start);

		mesh.GlobalPosition = new Vector3((start + end) / 2.0f, leftCorner.GlobalPosition.Y, leftCorner.GlobalPosition.Z);

		float baseLength = Mathf.Abs(mesh.GetAabb().Size.X);

		if (!Mathf.IsZeroApprox(baseLength))
			mesh.Scale = new Vector3(len / baseLength, mesh.Scale.Y, mesh.Scale.Z);
	}

	private void PlaceV(MeshInstance3D mesh, MeshInstance3D topCorner, MeshInstance3D bottomCorner)
	{
		Aabb t = GlobalAabb(topCorner);
		Aabb b = GlobalAabb(bottomCorner);

		float start = b.End.Y;
		float end = t.Position.Y;
		float len = Mathf.Max(0.001f, end - start);

		mesh.GlobalPosition = new Vector3(topCorner.GlobalPosition.X, (start + end) / 2.0f, topCorner.GlobalPosition.Z);

		float baseLength = Mathf.Abs(mesh.GetAabb().Size.Z);

		if (!Mathf.IsZeroApprox(baseLength))
			mesh.Scale = new Vector3(mesh.Scale.X, mesh.Scale.Y, len / baseLength);
	}

	private void UpdateCollision(Vector3 center, float imageWidth, float imageHeight)
	{
		if (_collision.Shape is not BoxShape3D box)
			return;

		box.Size = new Vector3(imageWidth + 0.2f, imageHeight + 0.2f, 0.04f);
		_collision.GlobalPosition = center;
	}

	private void UpdateGrabPoints()
	{
		Aabb l = GlobalAabb(_left);
		Aabb r = GlobalAabb(_right);

		Vector3 lc = l.GetCenter();
		Vector3 rc = r.GetCenter();

		_grabLeft.GlobalPosition = new Vector3(l.Position.X, lc.Y, lc.Z + -0.08f);
		_grabRight.GlobalPosition = new Vector3(r.End.X, rc.Y, rc.Z + -0.08f);
	}

	private Aabb GlobalAabb(MeshInstance3D mesh)
	{
		Aabb a = mesh.GetAabb();

		Vector3[] p =
		{
			new Vector3(a.Position.X, a.Position.Y, a.Position.Z),
			new Vector3(a.End.X, a.Position.Y, a.Position.Z),
			new Vector3(a.Position.X, a.End.Y, a.Position.Z),
			new Vector3(a.End.X, a.End.Y, a.Position.Z),
			new Vector3(a.Position.X, a.Position.Y, a.End.Z),
			new Vector3(a.End.X, a.Position.Y, a.End.Z),
			new Vector3(a.Position.X, a.End.Y, a.End.Z),
			new Vector3(a.End.X, a.End.Y, a.End.Z)
		};

		Aabb result = new Aabb(mesh.ToGlobal(p[0]), Vector3.Zero);

		for (int i = 1; i < p.Length; i++)
			result = result.Expand(mesh.ToGlobal(p[i]));

		return result;
	}
}
