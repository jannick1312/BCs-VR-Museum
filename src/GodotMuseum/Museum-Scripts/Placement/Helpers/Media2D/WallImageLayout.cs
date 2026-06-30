using System.Collections.Generic;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D;

public static class WallImageLayout
{
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