using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Object3D;

public sealed class Object3DDisplayFitter
{
	private static readonly EventLogger Log = new(nameof(Object3DDisplayFitter));
	private readonly MeshInstance3D _baseMesh;
	private readonly Node3D _template;

	public Object3DDisplayFitter(Node3D template)
	{
		_template = template;
		_baseMesh = (MeshInstance3D)_template.FindChild("BaseMesh", true, false);
		ConfigureCompatibilityOrbMaterial();
	}

	private void ConfigureCompatibilityOrbMaterial()
	{
		var renderingMethod = RenderingServer.GetCurrentRenderingMethod();
		if (renderingMethod != "gl_compatibility")
			return;

		var sphere = _template.GetNodeOrNull<MeshInstance3D>("Hinge/Orb/Sphere");
		var glass = sphere.GetActiveMaterial(0) as StandardMaterial3D;

		glass?.RefractionEnabled = false;
		glass?.AlbedoColor = new Color(0.65f, 0.85f, 1.0f, 0.08f);
	}

	public float Place(Node3D item, Node3D objectNode, Node3D place, Aabb objectBounds)
	{
		PlaceInstance(item, place);
		AlignDecisionTowardCenter(item, place.GlobalPosition);

		var slot = item.GetNode<MeshInstance3D>("Hinge/ObjectSlot");
		slot.Visible = false;
		PlaceObject(objectNode, slot);

		return ScaleToSlot(objectNode, slot, objectBounds);
	}

	private void PlaceInstance(Node3D item, Node3D place)
	{
		var scale = InstanceScale(place);
		var basis = place.GlobalTransform.Basis.Orthonormalized() * _template.Transform.Basis.Orthonormalized();
		var origin = place.GlobalTransform.Origin - basis * (BaseCenter() * scale);
		item.GlobalTransform = new Transform3D(basis, origin);
		item.Scale *= scale;
	}

	private static void AlignDecisionTowardCenter(Node3D item, Vector3 pivot)
	{
		var decision = item.GetNodeOrNull<Node3D>("Decision");
		if (decision == null)
			return;

		var currentForward = Flatten(decision.GlobalTransform.Basis.Z);
		var desiredForward = Flatten(Vector3.Zero - decision.GlobalPosition);

		if (currentForward.LengthSquared() <= 0.0001f || desiredForward.LengthSquared() <= 0.0001f)
			return;

		var angle = SignedHorizontalAngle(currentForward.Normalized(), desiredForward.Normalized());
		var rotation = new Basis(Vector3.Up, angle);
		var transform = item.GlobalTransform;

		transform.Basis = rotation * transform.Basis;
		transform.Origin = pivot + rotation * (transform.Origin - pivot);
		item.GlobalTransform = transform;
	}

	private static Vector3 Flatten(Vector3 vector)
	{
		vector.Y = 0.0f;
		return vector;
	}

	private static float SignedHorizontalAngle(Vector3 from, Vector3 to)
	{
		return Mathf.Atan2(from.Cross(to).Dot(Vector3.Up), from.Dot(to));
	}

	private static void PlaceObject(Node3D objectNode, MeshInstance3D slot)
	{
		var parent = (Node3D)slot.GetParent();
		parent.AddChild(objectNode);
		objectNode.Transform = new Transform3D(slot.Transform.Basis.Orthonormalized(), slot.Transform.Origin);
	}

	private static float ScaleToSlot(Node3D objectNode, MeshInstance3D slot, Aabb objectBounds)
	{
		var slotBounds = PlacementBounds.ScaledMeshBounds(slot);
		var scale = FitScale(objectBounds.Size, slotBounds.Size);

		objectNode.Scale *= scale;
		objectNode.Position -= objectNode.Transform.Basis * objectBounds.GetCenter();

		return scale;
	}

	private float InstanceScale(Node3D place)
	{
		var placeBounds = PlacementBounds.ScaledMeshBounds((MeshInstance3D)place);
		var baseSize = BaseSize();
		var sx = placeBounds.Size.X / baseSize.X;
		var sz = placeBounds.Size.Z / baseSize.Z;

		return Mathf.Min(sx, sz);
	}

	private Vector3 BaseSize()
	{
		var size = _baseMesh.GetAabb().Size;
		var scale = ChainScale(_baseMesh, _template);

		return new Vector3(size.X * scale.X, size.Y * scale.Y, size.Z * scale.Z);
	}

	private Vector3 BaseCenter()
	{
		return LocalTo(_baseMesh, _template) * _baseMesh.GetAabb().GetCenter();
	}

	private static Vector3 ChainScale(Node3D node, Node3D root)
	{
		var scale = Vector3.One;
		var current = node;

		while (current != null)
		{
			var local = current.Scale.Abs();
			scale = new Vector3(scale.X * local.X, scale.Y * local.Y, scale.Z * local.Z);

			if (current == root)
				break;

			current = current.GetParent() as Node3D;
		}

		return scale;
	}

	private static Transform3D LocalTo(Node3D node, Node3D root)
	{
		var result = Transform3D.Identity;
		var current = node;

		while (current != null && current != root)
		{
			result = current.Transform * result;
			current = current.GetParent() as Node3D;
		}

		return result;
	}

	private static float FitScale(Vector3 objectSize, Vector3 targetSize)
	{
		if (objectSize.X <= 0 || objectSize.Y <= 0 || objectSize.Z <= 0)
		{
			Log.Warning("Invalid 3D object size. Using scale 1.");
			return 1.0f;
		}

		var sx = targetSize.X / objectSize.X;
		var sy = targetSize.Y / objectSize.Y;
		var sz = targetSize.Z / objectSize.Z;

		return Mathf.Min(sx, Mathf.Min(sy, sz));
	}

	public static Aabb Bounds(Node3D root)
	{
		var hasBounds = root is MeshInstance3D;
		var bounds = root is MeshInstance3D rootMesh ? rootMesh.GetAabb() : new Aabb();

		foreach (var node in root.FindChildren("*", "MeshInstance3D", true, false))
		{
			if (node is not MeshInstance3D mesh)
				continue;

			var local = LocalTo(mesh, root);
			var meshBounds = TransformBounds(local, mesh.GetAabb());

			bounds = hasBounds ? bounds.Merge(meshBounds) : meshBounds;
			hasBounds = true;
		}

		return hasBounds ? bounds : new Aabb(Vector3.Zero, Vector3.One);
	}

	public static Aabb TransformBounds(Transform3D transform, Aabb bounds)
	{
		var min = bounds.Position;
		var max = bounds.End;

		var points = new[]
		{
			new Vector3(min.X, min.Y, min.Z),
			new Vector3(max.X, min.Y, min.Z),
			new Vector3(min.X, max.Y, min.Z),
			new Vector3(max.X, max.Y, min.Z),
			new Vector3(min.X, min.Y, max.Z),
			new Vector3(max.X, min.Y, max.Z),
			new Vector3(min.X, max.Y, max.Z),
			new Vector3(max.X, max.Y, max.Z)
		};

		var result = new Aabb(transform * points[0], Vector3.Zero);

		for (var i = 1; i < points.Length; i++)
			result = result.Expand(transform * points[i]);

		return result;
	}
}
