using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Object3D;

public static class OriginalSizeFitter
{
	public static bool Fits(Aabb objectBounds, Vector3 maximumSize)
	{
		var size = objectBounds.Size;
		return size.X <= maximumSize.X && size.Y <= maximumSize.Y && size.Z <= maximumSize.Z;
	}

	public static (float Scale, Vector3 Position) Calculate(Aabb objectBounds, Vector3 maximumSize, Vector3? minimumSize = null)
	{
		var maximumScale = CalculateMaximumScale(objectBounds.Size, maximumSize);
		var scale = Mathf.Min(1.0f, maximumScale);

		if (minimumSize != null)
		{
			var minimumScale = LongestAxis(minimumSize.Value) / Mathf.Max(LongestAxis(objectBounds.Size), 0.0001f);

			if (minimumScale <= maximumScale)
				scale = Mathf.Clamp(1.0f, minimumScale, maximumScale);
		}

		var center = objectBounds.GetCenter();
		var position = new Vector3(-center.X * scale, -objectBounds.Position.Y * scale, -center.Z * scale);

		return (scale, position);
	}

	private static float CalculateMaximumScale(Vector3 objectSize, Vector3 maximumSize)
	{
		if (objectSize.X <= 0.0f || objectSize.Y <= 0.0f || objectSize.Z <= 0.0f)
			return 1.0f;

		return Mathf.Min(maximumSize.X / objectSize.X, Mathf.Min(maximumSize.Y / objectSize.Y, maximumSize.Z / objectSize.Z));
	}

	private static float LongestAxis(Vector3 size)
	{
		return Mathf.Max(size.X, Mathf.Max(size.Y, size.Z));
	}
}
