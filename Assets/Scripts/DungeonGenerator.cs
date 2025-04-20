using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine.Serialization;

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

internal class Door
{
    public Door(BoundsInt doorBounds, Color doorColor)
    {
        bounds = doorBounds;
        color = doorColor;
    }
    public BoundsInt bounds;
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
    [FormerlySerializedAs("bottomLeftCorner")]
    [TabGroup("Settings", "General", SdfIconType.GearFill)]
    [BoxGroup("Settings/General/Area Bounds")]
    [LabelText("Starting point"), GUIColor(0.8f, 0.95f, 1f)]
    [Tooltip("Starting point of the area (in grid coordinates).")]
    [DisableIf("seed")]
    public Vector2Int startPoint = new(0, 0);

    [FormerlySerializedAs("topRightCorner")]
    [BoxGroup("Settings/General/Area Bounds")]
    [LabelText("Size"), GUIColor(0.8f, 0.95f, 1f)]
    [Tooltip("The size of the dungeon.")]
    public Vector2Int dungeonSize = new(500, 200);

    [TabGroup("Settings", "Division", SdfIconType.LayoutThreeColumns)]
    [BoxGroup("Settings/Division/Config")]
    [MinValue(1), LabelText("Division Count")]
    [Tooltip("How many times the room can be divided.")]
    public int divisions = 1;

    [BoxGroup("Settings/Division/Config")]
    [MinValue(1), LabelText("Minimum Room Size")]
    [Tooltip("Rooms smaller than this size will be discarded.")]
    public int sizeConstrain = 30;

    [BoxGroup("Settings/Division/Config")]
    [Range(1f, 5f), LabelText("Acceptable Aspect Ratio")]
    [Tooltip("Max allowed ratio between width and height of a room.")]
    public float acceptableRatio = 1.5f;

    [TabGroup("Settings", "Structure", SdfIconType.HouseFill)]
    [BoxGroup("Settings/Structure/Walls & Doors")]
    [MinValue(0), LabelText("Wall Width")]
    [Tooltip("Width of the generated wall (in grid units).")]
    public int wallWidth = 1;

    [BoxGroup("Settings/Structure/Walls & Doors")]
    [MinValue(0), LabelText("Wall Height")]
    [Tooltip("Height of the generated wall (in Unity units).")]
    public int wallHeight = 5;

    [BoxGroup("Settings/Structure/Walls & Doors")]
    [MinValue(1), LabelText("Door Width")]
    [Tooltip("Width of doors that connect rooms.")]
    public int doorWidth = 3;
    [BoxGroup("Settings/Structure/Walls & Doors")]
    [MinValue(0), LabelText("Door Offset")]
    public int doorOffset = 1;

    [TabGroup("Settings", "Randomization", SdfIconType.Dice6Fill)]
    [BoxGroup("Settings/Randomization/Seed")]
    [InlineButton("RandomizeSeed", SdfIconType.Dice6Fill, "Randomize")]
    [LabelText("Seed Value")]
    [Tooltip("Seed used to initialize the procedural generation.")]
    public int seed = 1;
    
    [TabGroup("Settings", "Debug", SdfIconType.BugFill)]
    [BoxGroup("Settings/Debug/Visibility")]
    [LabelText("Show Labels")]
    [GUIColor("@showLabels ? new Color(1f, 0.8f, 0.4f) : Color.gray")]
    public bool showLabels;

    [BoxGroup("Settings/Debug/Visibility")]
    [LabelText("Show Doors")]
    [GUIColor("@showDoors ? new Color(0.6f, 1f, 0.6f) : Color.gray")]
    public bool showDoors;

    [BoxGroup("Settings/Debug/Visibility")]
    [LabelText("Show Floor")]
    [GUIColor("@showFloor ? new Color(0.5f, 0.9f, 1f) : Color.gray")]
    public bool showFloor;

    [BoxGroup("Settings/Debug/Visibility")]
    [LabelText("Show Walls")]
    [GUIColor("@showWalls ? new Color(1f, 0.5f, 0.5f) : Color.gray")]
    public bool showWalls;

    private List<Room> rooms = new();
    private List<Wall> walls = new();
    private List<Door> doors = new();
    private System.Random rnd;

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
    
    private void RandomizeSeed()
    {
        seed = Random.Range(0, int.MaxValue);
    }

    [HorizontalGroup("ActionButtons", Width = 0.5f)]
    [Button("🎯 Generate Dungeon", ButtonSizes.Large), GUIColor(0.3f, 0.7f, 1f)]
    private void NewGeneration()
    {
        RandomizeSeed();
        RegenerateRooms();
    }

    [HorizontalGroup("ActionButtons", Width = 0.5f)]
    [Button("♻️ Regenerate Dungeon", ButtonSizes.Large), GUIColor(0.2f, 1f, 0.6f)]
    private void RegenerateRooms()
    {
        rooms.Clear();
        rnd = new System.Random(seed);
        rooms.Add(new Room(new RectInt(startPoint.x, startPoint.y, dungeonSize.x, dungeonSize.y), SplitMethod.Horizontaly, Color.green));
        walls.Clear();
        doors.Clear();

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
                    PlaceDoors();
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
        if (showFloor)
        {
            foreach (var room in rooms)
            {
                AlgorithmsUtils.DebugRectInt(room.bounds, room.color);
            }
        }

        if (showWalls)
        {
            foreach (var wall in walls)
            {
                AlgorithmsUtils.DebugBoundsInt(wall.bounds, wall.color);
            }
        }

        if (showDoors)
        {
            foreach (var door in doors)
            {
                AlgorithmsUtils.DebugBoundsInt(door.bounds, door.color);
            }
        }
    }

    private List<Room> SplitRoom(Room room, SplitMethod splitMethod, int offset)
    {
        var newRooms = new List<Room>();
        switch (splitMethod)
        {
            case SplitMethod.Verticaly:
                var newRoom1 = new RectInt(room.bounds.x, room.bounds.y, rnd.Next(sizeConstrain, room.bounds.width - sizeConstrain) + offset,  room.bounds.height);
                var newRoom2 = new RectInt(newRoom1.xMax - offset, room.bounds.y, room.bounds.width - newRoom1.width + offset, room.bounds.height);
                newRooms.Add(new Room(newRoom1, SplitMethod.Horizontaly, Color.cyan));
                newRooms.Add(new Room(newRoom2, SplitMethod.Horizontaly, Color.cyan));
                break;
            case SplitMethod.Horizontaly:
                newRoom1 = new RectInt(room.bounds.x, room.bounds.y, room.bounds.width, rnd.Next(sizeConstrain, room.bounds.height - sizeConstrain) + offset);
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
                    if (tempBox.size.x > doorWidth + wallWidth * 2)
                    {
                        adaptiveColor = Color.green;
                        doorDirection = DoorDirection.Z;
                    }
                    else if (tempBox.size.z > doorWidth + wallWidth * 2)
                    {
                        adaptiveColor = Color.green;
                        doorDirection = DoorDirection.X;
                    }
                    var tempWall = new Wall(tempBox, adaptiveColor, doorDirection);
                    walls.Add(tempWall);
                }
            }
        }
        var edgeWall1 = new Wall(new BoundsInt(new Vector3Int(startPoint.x, 0, startPoint.y), new Vector3Int(wallWidth, wallHeight, dungeonSize.y)), Color.red, DoorDirection.None);
        var edgeWall2 = new Wall(new BoundsInt(new Vector3Int(startPoint.x, 0, dungeonSize.y - wallWidth), new Vector3Int(dungeonSize.x, wallHeight, wallWidth)), Color.red, DoorDirection.None);
        var edgeWall3 = new Wall(new BoundsInt(new Vector3Int(dungeonSize.x - wallWidth, 0, startPoint.y), new Vector3Int(wallWidth, wallHeight, dungeonSize.y)), Color.red, DoorDirection.None);
        var edgeWall4 = new Wall(new BoundsInt(new Vector3Int(startPoint.x, 0, startPoint.y), new Vector3Int(dungeonSize.x, wallHeight, wallWidth)), Color.red, DoorDirection.None);
        walls.Add(edgeWall1);
        walls.Add(edgeWall2);
        walls.Add(edgeWall3);
        walls.Add(edgeWall4);
    }

    private void PlaceDoors()
    {
        foreach (var wall in walls)
        {
            if (wall.doorDirection is DoorDirection.None) continue;

            switch (wall.doorDirection)
            {
                case DoorDirection.X:
                    var zMax = wall.bounds.zMax - wallWidth - doorWidth;
                    var zMin = wall.bounds.zMin + wallWidth;
                    var tempBounds = new BoundsInt(new Vector3Int(wall.bounds.xMin - doorOffset, 0, rnd.Next(zMin, zMax)), new Vector3Int(wallWidth + doorOffset * 2, wallHeight + doorOffset, doorWidth));
                    var tempDoor = new Door(tempBounds, Color.yellow);
                    doors.Add(tempDoor);
                    break;
                case DoorDirection.Z:
                    var xMax = wall.bounds.xMax - wallWidth - doorWidth;
                    var xMin = wall.bounds.xMin + wallWidth;
                    tempBounds = new BoundsInt(new Vector3Int(rnd.Next(xMin, xMax), 0, wall.bounds.zMin - doorOffset), new Vector3Int(doorWidth, wallHeight + doorOffset, wallWidth + doorOffset * 2));
                    tempDoor = new Door(tempBounds, Color.yellow);
                    doors.Add(tempDoor);
                    break;
            }
        }
    }

    private void OnDrawGizmos()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
        if (showWalls)
        {
            for (int i = 0; i < walls.Count; i++)
            {
                var wall = walls[i];
                var color = wall.color;
                color.a = 0.3f;
                Gizmos.color = color;
                Vector3 center = wall.bounds.center;
                Gizmos.DrawCube(center, wall.bounds.size);
                if (showLabels)
                {
#if UNITY_EDITOR
                    Handles.Label(center, $"*{i}", style);
#endif
                }
            }
        }
        
        style.normal.textColor = Color.gray;
        if (showFloor)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                var color = room.color;
                color.a = 0.3f;
                Gizmos.color = color;
                Vector3 center = new Vector3(room.bounds.center.x, 0, room.bounds.center.y);
                var size = new Vector3(room.bounds.size.x, 0.01f, room.bounds.size.y);
                Gizmos.DrawCube(center, size);
                if (showLabels)
                {
#if UNITY_EDITOR
                    Handles.Label(center, $"#{i}", style);
#endif
                }
            }
        }

        if (showDoors)
        {
            foreach (var door in doors)
            {
                var color = door.color;
                color.a = 0.3f;
                Gizmos.color = color;
                Vector3 center = door.bounds.center;
                Gizmos.DrawCube(center, door.bounds.size);
            }
        }
    }
}
