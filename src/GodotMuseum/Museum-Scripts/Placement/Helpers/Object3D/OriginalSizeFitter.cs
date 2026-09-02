using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Object3D;

/// <summary>
/// Calculates 3D model scale and position for viewing rooms.
/// </summary>
public static class OriginalSizeFitter
{
	/// <summary>
	/// Checks if a 3D model fits inside a maximum size without scaling.
	/// </summary>
	/// <param name="objectBounds">The original bounds of the 3D model.</param>
	/// <param name="maximumSize">The available room size.</param>
	/// <returns><see langword="true"/> if the full 3D model fits and <see langword="false"/> otherwise.</returns>
	public static bool Fits(Aabb objectBounds, Vector3 maximumSize)
	{
		var size = objectBounds.Size;
		return size.X <= maximumSize.X && size.Y <= maximumSize.Y && size.Z <= maximumSize.Z;
	}

	/// <summary>
	/// Gets one scale value and a floor position that fit inside the room.
	/// </summary>
	/// <param name="objectBounds">The original bounds of the 3D model.</param>
	/// <param name="maximumSize">The maximum available room size.</param>
	/// <param name="minimumSize">The optional smallest display size.</param>
	/// <returns>The scale and position to apply to the 3D model's origin.</returns>
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

	/// <summary>
	/// Gets the largest scale value that fits inside a maximum size.
	/// </summary>
	/// <param name="objectSize">The original 3D model size.</param>
	/// <param name="maximumSize">The maximum available size.</param>
	/// <returns>The largest scale that fits.</returns>
	private static float CalculateMaximumScale(Vector3 objectSize, Vector3 maximumSize)
	{
		if (objectSize.X <= 0.0f || objectSize.Y <= 0.0f || objectSize.Z <= 0.0f)
			return 1.0f;

		return Mathf.Min(maximumSize.X / objectSize.X, Mathf.Min(maximumSize.Y / objectSize.Y, maximumSize.Z / objectSize.Z));
	}

	/// <summary>
	/// Gets the length of the longest side of a size.
	/// </summary>
	/// <param name="size">The size to inspect.</param>
	/// <returns>The longest side length.</returns>
	private static float LongestAxis(Vector3 size)
	{
		return Mathf.Max(size.X, Mathf.Max(size.Y, size.Z));
	}
}



// All calculations in this file were implemented with the assistance of Codex.
