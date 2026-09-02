using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Object3D;

/// <summary>
/// Places 3D model displays and scales the models to fit.
/// </summary>
public sealed class Object3DDisplayFitter
{
	private static readonly EventLogger Log = new(nameof(Object3DDisplayFitter));

	private readonly MeshInstance3D _baseMesh;
	private readonly Node3D _template;

	/// <summary>
	/// Creates a fitter from a 3D model display template.
	/// </summary>
	/// <param name="template">The display template containing the base and model slot.</param>
	public Object3DDisplayFitter(Node3D template)
	{
		_template = template;
		_baseMesh = (MeshInstance3D)_template.FindChild("BaseMesh", true, false);
		ConfigureCompatibilityOrbMaterial();
	}

	/// <summary>
	/// Adjusts the glass material for the compatibility renderer.
	/// </summary>
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

	/// <summary>
	/// Fits the 3D model into the display.
	/// </summary>
	/// <param name="item">The generated display template.</param>
	/// <param name="objectNode">The 3D model to show.</param>
	/// <param name="place">The museum placement area.</param>
	/// <param name="objectBounds">The local bounds of the 3D model.</param>
	/// <returns>The scale applied to the 3D model.</returns>
	public float Place(Node3D item, Node3D objectNode, Node3D place, Aabb objectBounds)
	{
		PlaceInstance(item, place);
		AlignDecisionTowardCenter(item, place.GlobalPosition);

		var slot = item.GetNode<MeshInstance3D>("Hinge/ObjectSlot");
		slot.Visible = false;
		PlaceObject(objectNode, slot);

		return ScaleToSlot(objectNode, slot, objectBounds);
	}

	/// <summary>
	/// Fits and lines up a display with its place in the museum.
	/// </summary>
	/// <param name="item">The generated display template.</param>
	/// <param name="place">The museum placement area.</param>
	private void PlaceInstance(Node3D item, Node3D place)
	{
		var scale = InstanceScale(place);
		var basis = place.GlobalTransform.Basis.Orthonormalized() * _template.Transform.Basis.Orthonormalized();
		var origin = place.GlobalTransform.Origin - basis * (BaseCenter() * scale);
		item.GlobalTransform = new Transform3D(basis, origin);
		item.Scale *= scale;
	}

	/// <summary>
	/// Rotates the decision controls toward the museum center.
	/// </summary>
	/// <param name="item">The generated display template.</param>
	/// <param name="pivot">The pivot used to rotate the display.</param>
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

	/// <summary>
	/// Removes the vertical part of a direction.
	/// </summary>
	/// <param name="vector">The direction to flatten.</param>
	/// <returns>The horizontal direction.</returns>
	private static Vector3 Flatten(Vector3 vector)
	{
		vector.Y = 0.0f;
		return vector;
	}

	/// <summary>
	/// Gets the angle between two horizontal directions.
	/// </summary>
	/// <param name="from">The first direction.</param>
	/// <param name="to">The target direction.</param>
	/// <returns>The signed angle in radians.</returns>
	private static float SignedHorizontalAngle(Vector3 from, Vector3 to)
	{
		return Mathf.Atan2(from.Cross(to).Dot(Vector3.Up), from.Dot(to));
	}

	/// <summary>
	/// Adds a 3D model to the display and aligns it with the model slot.
	/// </summary>
	/// <param name="objectNode">The 3D model to attach.</param>
	/// <param name="slot">The mesh that sets the model's position and rotation.</param>
	private static void PlaceObject(Node3D objectNode, MeshInstance3D slot)
	{
		var parent = (Node3D)slot.GetParent();
		parent.AddChild(objectNode);
		objectNode.Transform = new Transform3D(slot.Transform.Basis.Orthonormalized(), slot.Transform.Origin);
	}

	/// <summary>
	/// Scales and centers a 3D model within the display slot.
	/// </summary>
	/// <param name="objectNode">The 3D model to fit.</param>
	/// <param name="slot">The target display slot.</param>
	/// <param name="objectBounds">The local bounds of the 3D model.</param>
	/// <returns>The scale applied to the 3D model.</returns>
	private static float ScaleToSlot(Node3D objectNode, MeshInstance3D slot, Aabb objectBounds)
	{
		var slotBounds = PlacementBounds.ScaledMeshBounds(slot);
		var scale = FitScale(objectBounds.Size, slotBounds.Size);

		objectNode.Scale *= scale;
		objectNode.Position -= objectNode.Transform.Basis * objectBounds.GetCenter();

		return scale;
	}

	/// <summary>
	/// Gets the scale that fits the display base onto the museum placement area.
	/// </summary>
	/// <param name="place">The museum placement area.</param>
	/// <returns>The scale applied to the display template.</returns>
	private float InstanceScale(Node3D place)
	{
		var placeBounds = PlacementBounds.ScaledMeshBounds((MeshInstance3D)place);
		var baseSize = BaseSize();
		var sx = placeBounds.Size.X / baseSize.X;
		var sz = placeBounds.Size.Z / baseSize.Z;

		return Mathf.Min(sx, sz);
	}

	/// <summary>
	/// Gets the display base size inside the template.
	/// </summary>
	/// <returns>The scaled base size.</returns>
	private Vector3 BaseSize()
	{
		var size = _baseMesh.GetAabb().Size;
		var scale = ChainScale(_baseMesh, _template);

		return new Vector3(size.X * scale.X, size.Y * scale.Y, size.Z * scale.Z);
	}

	/// <summary>
	/// Gets the display base center inside the template.
	/// </summary>
	/// <returns>The base center inside the template.</returns>
	private Vector3 BaseCenter()
	{
		return LocalTo(_baseMesh, _template) * _baseMesh.GetAabb().GetCenter();
	}

	/// <summary>
	/// Combines the scale of a node and all parents up to a root node.
	/// </summary>
	/// <param name="node">The child node where the check starts.</param>
	/// <param name="root">The last parent node included in the check.</param>
	/// <returns>The combined absolute scale.</returns>
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

	/// <summary>
	/// Gets a node's position, rotation, and scale inside a parent node.
	/// </summary>
	/// <param name="node">The child node.</param>
	/// <param name="root">The parent node used as the root.</param>
	/// <returns>The node transform inside the root node.</returns>
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

	/// <summary>
	/// Gets one scale value that fits a 3D model inside a target size.
	/// </summary>
	/// <param name="objectSize">The 3D model size.</param>
	/// <param name="targetSize">The available target size.</param>
	/// <returns>The scale that fits the 3D model.</returns>
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

	/// <summary>
	/// Gets one box around a node and all child meshes.
	/// </summary>
	/// <param name="root">The root whose geometry is measured.</param>
	/// <returns>The box around all meshes or a unit box when no meshes are found.</returns>
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

	/// <summary>
	/// Moves all corners of a bounds box with a transform.
	/// </summary>
	/// <param name="transform">The rotation and scale to use.</param>
	/// <param name="bounds">The bounds box to change.</param>
	/// <returns>A box around the corners.</returns>
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



// All calculations in this file were implemented with the assistance of Codex.
