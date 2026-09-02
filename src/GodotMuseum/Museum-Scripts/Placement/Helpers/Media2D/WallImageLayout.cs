using System.Collections.Generic;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D;

/// <summary>
/// Splits a wall area into slots for images and videos.
/// </summary>
public static class WallImageLayout
{
	/// <summary>
	/// Creates up to four equal horizontal slots.
	/// </summary>
	/// <param name="count">The number of slots.</param>
	/// <param name="areaWidth">The available area width.</param>
	/// <param name="areaHeight">The available area height.</param>
	/// <returns>The centered slot rectangles.</returns>
	public static List<Rect2> CreateCenteredHorizontalSlots(int count, float areaWidth, float areaHeight)
	{
		var slots = new List<Rect2>();
		var usedCount = Mathf.Clamp(count, 1, 4);
		var slotWidth = areaWidth / usedCount;
		var left = -areaWidth / 2.0f;
		var bottom = -areaHeight / 2.0f;

		for (var i = 0; i < usedCount; i++)
		{
			var slotX = left + i * slotWidth;
			slots.Add(new Rect2(new Vector2(slotX, bottom), new Vector2(slotWidth, areaHeight)));
		}

		return slots;
	}
}



// All calculations in this file were implemented with the assistance of Codex.
