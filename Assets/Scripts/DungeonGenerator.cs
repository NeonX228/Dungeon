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

internal enum DoorDirection
{
    X,
    Z,
    None
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

internal class Wall
{
    public Wall(BoundsInt wallBounds, Color wallColor, DoorDirection targetDoorDirection)
    {
        bounds = wallBounds;
        color = wallColor;
        doorDirection = targetDoorDirection;
    }
    public BoundsInt bounds;
    public Color color;
    public DoorDirection doorDirection;
    
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
    public int doorWidth = 3;
    
    private List<Room> rooms = new();
    private List<Wall> walls = new();

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
                    BuildTheWall();
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
            AlgorithmsUtils.DebugBoundsInt(wall.bounds, wall.color);
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
                newRooms.Add(new Room(newRoom1, SplitMethod.Horizontaly, Color.cyan));
                newRooms.Add(new Room(newRoom2, SplitMethod.Horizontaly, Color.cyan));
                break;
            case SplitMethod.Horizontaly:
                newRoom1 = new RectInt(room.bounds.x, room.bounds.y, room.bounds.width, Random.Range(sizeConstrain, room.bounds.height - sizeConstrain) + offset);
                newRoom2 = new RectInt(room.bounds.x, newRoom1.yMax - offset, room.bounds.width, room.bounds.height - newRoom1.height + offset);
                newRooms.Add(new Room(newRoom1, SplitMethod.Verticaly, Color.cyan));
                newRooms.Add(new Room(newRoom2, SplitMethod.Verticaly,Color.cyan));
                break;
        }
        return newRooms;
    }

    private void BuildTheWall()
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                var firstRoom = rooms[i];
                var secondRoom = rooms[j];
                
                if (AlgorithmsUtils.Intersects(firstRoom.bounds, secondRoom.bounds))
                {
                    var intersect = AlgorithmsUtils.Intersect(firstRoom.bounds, secondRoom.bounds);
                    var tempBox = new BoundsInt(new Vector3Int(intersect.x, 0, intersect.y),
                        new Vector3Int(intersect.width, wallHeight, intersect.height));
                    if ((tempBox.size.x != wallWidth) && (tempBox.size.x != wallWidth * 2) &&
                        (tempBox.size.z != wallWidth) && (tempBox.size.z != wallWidth * 2))
                    {
                        continue;
                    }
                    var adaptiveColor = Color.red;
                    var doorDirection = DoorDirection.None;
                    if (tempBox.size.x > doorWidth)
                    {
                        adaptiveColor = Color.green;
                        doorDirection = DoorDirection.X;
                    }
                    else if (tempBox.size.z > doorWidth)
                    {
                        adaptiveColor = Color.green;
                        doorDirection = DoorDirection.Z;
                    }
                    var tempWall = new Wall(tempBox, adaptiveColor, doorDirection);
                    walls.Add(tempWall);
                }
            }
        }
        var edgeWall1 = new Wall(new BoundsInt(new Vector3Int(0, 0, 0), new Vector3Int(wallWidth, wallHeight, topRightCorner.y)), Color.red, DoorDirection.None);
        var edgeWall2 = new Wall(new BoundsInt(new Vector3Int(0, 0, topRightCorner.y - wallWidth), new Vector3Int(topRightCorner.x, wallHeight, wallWidth)), Color.red, DoorDirection.None);
        var edgeWall3 = new Wall(new BoundsInt(new Vector3Int(topRightCorner.x - wallWidth, 0, 0), new Vector3Int(wallWidth, wallHeight, topRightCorner.y)), Color.red, DoorDirection.None);
        var edgeWall4 = new Wall(new BoundsInt(new Vector3Int(0, 0, 0), new Vector3Int(topRightCorner.x, wallHeight, wallWidth)), Color.red, DoorDirection.None);
        walls.Add(edgeWall1);
        walls.Add(edgeWall2);
        walls.Add(edgeWall3);
        walls.Add(edgeWall4);
    }

    private bool CheckEdges(BoundsInt wall)
    {
        return (wall.xMin == 0 && wall.xMax == wallWidth) ||
               (wall.xMin == topRightCorner.x - wallWidth && wall.xMax == topRightCorner.x) ||
               (wall.yMin == 0 && wall.yMax == wallWidth) ||
               (wall.yMin == topRightCorner.y - wallWidth && wall.yMax == topRightCorner.y);
    }

    private void CleanUp()
    {
        var objectsToDelete = new List<int>();
        for (int i = 0; i < walls.Count; i++)
        {
            var wall = walls[i];
            if ((wall.bounds.size.x != wallWidth) && (wall.bounds.size.x != wallWidth * 2) &&
                (wall.bounds.size.z != wallWidth) && (wall.bounds.size.z != wallWidth * 2))
            {
                objectsToDelete.Add(i);
                continue;
            }

            foreach (var pairWall in walls)
            {
                if (pairWall.bounds == wall.bounds)
                {
                    objectsToDelete.Add(i);
                }
            }
        }
        objectsToDelete.Sort((a, b) => b.CompareTo(a));
        foreach (var index in objectsToDelete)
        {
            walls.RemoveAt(index);
        }
    }

    private void OnDrawGizmos()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        
        for (int i = 0; i < walls.Count; i++)
        {
            var wall = walls[i];
            var color = wall.color;
            color.a = 0.3f;
            Gizmos.color = color;
            Vector3 center = wall.bounds.center;
            Gizmos.DrawCube(center, wall.bounds.size);
#if UNITY_EDITOR
            Handles.Label(center, $"*{i}", style);
#endif
        }
        
        style.normal.textColor = Color.gray;
        
        for (int i = 0; i < rooms.Count; i++)
        {
            var room = rooms[i];
            var color = room.color;
            color.a = 0.3f;
            Gizmos.color = color;
            Vector3 center = new Vector3(room.bounds.center.x, 0, room.bounds.center.y);
            var size = new Vector3(room.bounds.size.x, 0.01f, room.bounds.size.y);
            Gizmos.DrawCube(center, size);
#if UNITY_EDITOR
            Handles.Label(center, $"#{i}", style);
#endif
        }
    }
}
