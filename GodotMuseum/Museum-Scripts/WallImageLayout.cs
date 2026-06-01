using Godot;
using System.Collections.Generic;

namespace BCSVRMuseum.Museum_Scripts;

public static class WallImageLayout
{
    public static List<Rect2> CreateHorizontalSlots(int count, float wallCenterX, float wallWidth, float wallBottomY, float wallHeight)
    {
        var slots = new List<Rect2>();

        var usedCount = Mathf.Clamp(count, 1, 4);

        var left = wallCenterX - wallWidth / 2.0f;
        var slotWidth = wallWidth / usedCount;

        for (var i = 0; i < usedCount; i++)
        {
            var slotX = left + i * slotWidth;

            slots.Add(
                new Rect2(
                    new Vector2(slotX, wallBottomY),
                    new Vector2(slotWidth, wallHeight)
                )
            );
        }

        return slots;
    }
}