using System.Collections.Generic;
using UnityEngine;

public class AlgorithmsUtils
{
    public static bool Intersects(RectInt a, RectInt b)
    {
        return a.xMin < b.xMax &&
               a.xMax > b.xMin &&
               a.yMin < b.yMax &&
               a.yMax > b.yMin;
    }
    
    public static RectInt Intersect(RectInt a, RectInt b)
    {
        int x = Mathf.Max(a.xMin, b.xMin);
        int y = Mathf.Max(a.yMin, b.yMin);
        int width = Mathf.Min(a.xMax, b.xMax) - x;
        int height = Mathf.Min(a.yMax, b.yMax) - y;

        if (width <= 0 || height <= 0)
        {
            return new RectInt();
        }
        return new RectInt(x, y, width, height);
    }
    
    public static void FillRectangle(char[,] array, RectInt area, char value)
    {
        for (int i = area.y; i < area.y + area.height; i++)
        {
            for (int j = area.x; j < area.x + area.width; j++)
            {
                array[i, j] = value;
            }
        }
    }
    
    public static void FillRectangleOutline(char[,] array, RectInt area, char value) 
    { 
        
        int endX = area.x + area.width - 1;
        int endY = area.y + area.height - 1;

        // Draw top and bottom borders
        for (int x = area.x; x <= endX; x++)
        {
            array[area.y, x] = value;
            array[endY, x] = value;
        }

        // Draw left and right borders
        for (int y = area.y + 1; y < endY; y++)
        {
            array[y, area.x] = value;
            array[y, endX] = value;
        }
    }

    public static void DebugRectInt(RectInt rectInt, Color color, float duration = 0f, bool depthTest = false, float height = 0.01f)
    {
        DebugExtension.DebugBounds(new Bounds(new Vector3(rectInt.center.x, 0, rectInt.center.y), new Vector3(rectInt.width, height, rectInt.height)), color, duration, depthTest);
    }
    
    public static void DebugBoundsInt(BoundsInt boundsInt, Color color, float duration = 0f)
    {
        Vector3 center = boundsInt.center;
        Vector3 size = boundsInt.size;

        Bounds bounds = new Bounds(center, size);

        // Call your DebugExtension method (assuming it's available in your project)
        DebugExtension.DebugBounds(bounds, color, duration, depthTest: false);
    }
}

public static class BoundsIntExt
{
    public static bool Intersects(this BoundsInt a, BoundsInt b)
    {
        return (a.xMin < b.xMax) && (a.xMax > b.xMin) &&
               (a.yMin < b.yMax) && (a.yMax > b.yMin) &&
               (a.zMin < b.zMax) && (a.zMax > b.zMin);
    }
    public static BoundsInt Intersect(BoundsInt a, BoundsInt b)
    {
        int x = Mathf.Max(a.xMin, b.xMin);
        int y = Mathf.Max(a.yMin, b.yMin);
        int z = Mathf.Max(a.zMin, b.zMin);
        int width = Mathf.Min(a.xMax, b.xMax) - x;
        int height = Mathf.Min(a.yMax, b.yMax) - y;
        int depth = Mathf.Min(a.zMax, b.zMax) - z;

        if (width <= 0 || height <= 0)
        {
            return new BoundsInt();
        }
        return new BoundsInt(x, y, z, width, height, depth);
    }
}
