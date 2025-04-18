using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;

internal enum SplitMethod
{
    Verticaly,
    Horizontaly,
}

internal class Room
{
    public Room(RectInt roomRect, SplitMethod lastSplitMethod, Color roomColor)
    {
        bounds = roomRect;
        splitMethod = lastSplitMethod;
        color = roomColor;
    }
    public RectInt bounds;
    public SplitMethod splitMethod;
    public Color color;
}

[ExecuteAlways]
public class DungeonGenerator : MonoBehaviour
{
    [Title("General Settings")]
    public Vector2Int bottomLeftCorner = new(0, 0);
    public Vector2Int topRightCorner = new(500, 200);
    [Title("Divisions")]
    public int divisions = 1;
    public int sizeConstrain = 30;
    public float acceptableRatio = 1.5f;
    [Title("Misc")]
    public int wallWidth = 1;
    public int wallHeight = 5;
    
    private List<Room> rooms = new();
    private List<BoundsInt> walls = new();

    private readonly Color[] brightColors = new[]
    {
        Color.red,
        Color.blue,
        Color.green,
        new Color(1f, 0f, 1f), // Pink
        Color.yellow,
        Color.cyan,
        new Color(1f, 0.5f, 0f) // Orange
    };

    [Button("Regenerate Rooms")]
    private void RegenerateRooms()
    {
        rooms.Clear();
        rooms.Add(new Room(new RectInt(bottomLeftCorner.x, bottomLeftCorner.y, topRightCorner.x, topRightCorner.y), SplitMethod.Horizontaly, Color.green));
        walls.Clear();

        var failStreak = 0;
        // Add new split room to the list
        for (var i = 0; i < divisions; i++)
        {
            var tempRoom = rooms[0];
            rooms.RemoveAt(0);
            if (tempRoom.bounds.width > sizeConstrain * 2 && tempRoom.bounds.height > sizeConstrain * 2)
            {
                // Do nothing
            }
            else if (tempRoom.bounds.width < sizeConstrain * 2
                && tempRoom.bounds.height > sizeConstrain * 2
                && tempRoom.bounds.width * acceptableRatio < tempRoom.bounds.height)
            {
                tempRoom.splitMethod = SplitMethod.Horizontaly;
            }
            else if (tempRoom.bounds.height < sizeConstrain * 2
                     && tempRoom.bounds.width > sizeConstrain * 2
                     && tempRoom.bounds.height * acceptableRatio < tempRoom.bounds.width)
            {
                tempRoom.splitMethod = SplitMethod.Verticaly;
            }
            else
            {
                failStreak++;
                rooms.Add(tempRoom);
                if (failStreak >= rooms.Count)
                {
                    Debug.Log($"Regenerating rooms ended on division: {i}");
                    FindIntersects();
                    return;
                }
                continue;
            }
            failStreak = 0;
            rooms.AddRange(SplitRoom(tempRoom, tempRoom.splitMethod, wallWidth));
        }
        Debug.Log("Rooms generated successfully");
    }
    private void Update()
    {
        foreach (var room in rooms)
        {
            AlgorithmsUtils.DebugRectInt(room.bounds, room.color);
        }

        foreach (var wall in walls)
        {
            AlgorithmsUtils.DebugBoundsInt(wall, Color.blue);
        }
    }

    private List<Room> SplitRoom(Room room, SplitMethod splitMethod, int offset)
    {
        var newRooms = new List<Room>();
        switch (splitMethod)
        {
            case SplitMethod.Verticaly:
                var newRoom1 = new RectInt(room.bounds.x, room.bounds.y, Random.Range(sizeConstrain, room.bounds.width - sizeConstrain) + offset,  room.bounds.height);
                var newRoom2 = new RectInt(newRoom1.xMax - offset, room.bounds.y, room.bounds.width - newRoom1.width + offset, room.bounds.height);
                newRooms.Add(new Room(newRoom1, SplitMethod.Horizontaly, new Color(Random.value, Random.value, Random.value)));
                newRooms.Add(new Room(newRoom2, SplitMethod.Horizontaly, new Color(Random.value, Random.value, Random.value)));
                break;
            case SplitMethod.Horizontaly:
                newRoom1 = new RectInt(room.bounds.x, room.bounds.y, room.bounds.width, Random.Range(sizeConstrain, room.bounds.height - sizeConstrain) + offset);
                newRoom2 = new RectInt(room.bounds.x, newRoom1.yMax - offset, room.bounds.width, room.bounds.height - newRoom1.height + offset);
                newRooms.Add(new Room(newRoom1, SplitMethod.Verticaly, brightColors[Random.Range(0, brightColors.Length)]));
                newRooms.Add(new Room(newRoom2, SplitMethod.Verticaly, brightColors[Random.Range(0, brightColors.Length)]));
                break;
        }
        return newRooms;
    }

    private void FindIntersects()
    {
        foreach (var firstRoom in rooms)
        {
            foreach (var secondRoom in rooms)
            {
                if (AlgorithmsUtils.Intersects(firstRoom.bounds, secondRoom.bounds))
                {
                    var intersect = AlgorithmsUtils.Intersect(firstRoom.bounds, secondRoom.bounds);
                    var tempBox = new BoundsInt(new Vector3Int(intersect.x, 0, intersect.y),
                        new Vector3Int(intersect.width, wallHeight, intersect.height));
                    walls.Add(tempBox);
                }
            }
        }
    }
}
