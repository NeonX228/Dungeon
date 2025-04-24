using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

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

internal class Connection
{
    public Connection(Node to, Node door)
    {
        node = to;
        via = door;
    }
    public Node node;
    public Node via;
}
internal class Node
{
    [HideInInspector]
    public bool enabled = true;
    [HideInInspector]
    public Color color;
    [HideInInspector]
    public BoundsInt bounds;
    
    [Button(ButtonSizes.Small, ButtonStyle.Box, Icon = SdfIconType.Search)]
    [GUIColor(0.8f, 0.8f, 1f)]
    [InfoBox("@\"Size: \" + GetSize().ToString()")]
    [PropertyOrder(-1)]
    public void Highlight()
    {
        AlgorithmsUtils.DebugBoundsInt(bounds, Color.magenta, 3f);
    }
    
    public int GetSize()
    {
        return 2 * (bounds.size.x + bounds.size.z);
    }
}

[Serializable]
internal class Room: Node
{
    public Room(RectInt roomRect, SplitMethod lastSplitMethod, Color roomColor)
    {
        bounds = new BoundsInt(new Vector3Int(roomRect.x, 0, roomRect.y), new Vector3Int(roomRect.width, 0, roomRect.height));
        rectBounds = roomRect;
        splitMethod = lastSplitMethod;
        color = roomColor;
    }
    [HideInInspector]
    public RectInt rectBounds;
    [HideInInspector]
    public SplitMethod splitMethod;
    public List<Wall> walls = new();
    
    [EnableIf("enabled")]
    [HorizontalGroup("Toggle")]
    [Button(ButtonSizes.Medium), GUIColor("@enabled ? Color.red : Color.gray")]
    public void Disable()
    {
        enabled = false;
        foreach (var wall in walls)
        {
            if (wall.doorDirection != DoorDirection.None)
            {
                wall.door.enabled = false;
                wall.doorDirection = DoorDirection.None;
            }
            wall.color = Color.red;
        }
    }
    
    [DisableIf("enabled")]
    [HorizontalGroup("Toggle")]
    [Button(ButtonSizes.Medium), GUIColor("@!enabled ? Color.green : Color.gray")]
    public void Enable()
    {
        enabled = true;
        foreach (var wall in walls)
        {
            if (!wall.rooms.All(room => room.enabled)) continue;
            
            if (wall.bounds.size.x > DungeonGenerator.doorWidth + DungeonGenerator.wallWidth * 2)
            {
                wall.color = Color.green;
                wall.doorDirection = DoorDirection.Z;
            }
            else if (wall.bounds.size.z > DungeonGenerator.doorWidth + DungeonGenerator.wallWidth * 2)
            {
                wall.color = Color.green;
                wall.doorDirection = DoorDirection.X;
            }
            else
            {
                wall.color = Color.red;
                wall.doorDirection = DoorDirection.None;
            }
            
            if (wall.doorDirection != DoorDirection.None) wall.door.enabled = true;
        }
    }
}

internal class Door: Node
{
    public Door(BoundsInt doorBounds, Color doorColor)
    {
        bounds = doorBounds;
        color = doorColor;
    }
}

internal class Wall: Node
{
    public Wall(BoundsInt wallBounds, Color wallColor, DoorDirection targetDoorDirection)
    {
        bounds = wallBounds;
        color = wallColor;
        doorDirection = targetDoorDirection;
    }
    [HideInInspector]
    public DoorDirection doorDirection;
    public Door door;
    [HideInInspector]
    public List<Room> rooms = new();
}

public class Graph<TKey, TValue>
{
    private Dictionary<TKey, List<TValue>> adjacencyList = new();

    public void AddNode(TKey node)
    {
        if (adjacencyList.ContainsKey(node)) return;

        adjacencyList.Add(node, new List<TValue>());
    }

    public void AddEdge(TKey fromNode, TValue toNode)
    {
        if (!adjacencyList.ContainsKey(fromNode))
        {
            adjacencyList.Add(fromNode, new List<TValue>());
        }

        adjacencyList[fromNode].Add(toNode);
    }
    
    public Dictionary<TKey, List<TValue>> GetList(){ return adjacencyList; }
    
    public void DropTable() {adjacencyList.Clear();}
}

[ExecuteAlways]
public class DungeonGenerator : MonoBehaviour
{
    [TabGroup("Settings", "General", SdfIconType.GearFill)]
    [BoxGroup("Settings/General/Area Bounds")]
    [LabelText("Starting point")]
    [Tooltip("Starting point of the area (in grid coordinates).")]
    [DisableIf("seed")]
    public Vector2Int startPoint = new(0, 0);
    
    [BoxGroup("Settings/General/Area Bounds")]
    [LabelText("Size")]
    [Tooltip("The size of the dungeon.")]
    public Vector2Int dungeonSize = new(500, 200);

    [TabGroup("Settings", "Division", SdfIconType.LayoutThreeColumns)]
    [BoxGroup("Settings/Division/Config")]
    [MinValue(1), LabelText("Endless Divisions")]
    [Tooltip("How many times the room can be divided.")]
    public bool endlessDivisions = false;
    
    [TabGroup("Settings", "Division", SdfIconType.LayoutThreeColumns)]
    [BoxGroup("Settings/Division/Config")]
    [MinValue(1), LabelText("Division Count")]
    [Tooltip("How many times the room can be divided.")]
    [HideIf("endlessDivisions")]
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
    [ShowInInspector]
    public static int wallWidth = 1;

    [BoxGroup("Settings/Structure/Walls & Doors")]
    [MinValue(0), LabelText("Wall Height")]
    [Tooltip("Height of the generated wall (in Unity units).")]
    [ShowInInspector]
    public static int wallHeight = 5;

    [BoxGroup("Settings/Structure/Walls & Doors")]
    [MinValue(1), LabelText("Door Width")]
    [Tooltip("Width of doors that connect rooms.")]
    [ShowInInspector]
    public static int doorWidth = 3;
    
    [BoxGroup("Settings/Structure/Walls & Doors")]
    [MinValue(0), LabelText("Door Offset")]
    [ShowInInspector]
    public static int doorOffset = 1;
    
    [BoxGroup("Settings/Structure/Rooms")]
    [MinValue(0), MaxValue(100), LabelText("Subtracted Percent")]
    public int subtractedPercent = 10;

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
    
    [BoxGroup("Settings/Debug/Visibility")]
    [LabelText("Show Nodes")]
    [GUIColor("@showNodes ? new Color(0.7f, 0.7f, 1f) : Color.gray")]
    public bool showNodes;

    [BoxGroup("Settings/Debug/Visibility")]
    [LabelText("Show Edges")]
    [GUIColor("@showEdges ? new Color(0.9f, 0.6f, 1f) : Color.gray")]
    public bool showEdges;
    
    [TabGroup("Settings", "Structure", SdfIconType.HouseFill)]
    [BoxGroup("Settings/Structure/Rooms")]
    [LabelText("All Rooms List")]
    [SerializeField]
    [ListDrawerSettings(DraggableItems = false, HideAddButton = true, HideRemoveButton = true, NumberOfItemsPerPage = 8)]
    private List<Room> rooms = new();
    private List<Wall> walls = new();
    private List<Door> doors = new();
    private System.Random rnd;
    private Graph<Node, Connection> graph = new();
    
    private void RandomizeSeed()
    {
        seed = Random.Range(0, int.MaxValue);
    }

    private void Start()
    {
        if (endlessDivisions)
        {
            divisions = 10;
        }
    }

    [HorizontalGroup("ActionButtons", Width = 0.5f)]
    [Button("🎯 Generate Dungeon", ButtonSizes.Large), GUIColor(0.3f, 0.7f, 1f)]
    [PropertyOrder(-1)]
    [PropertySpace(SpaceAfter = 10)]
    private void NewGeneration()
    {
        RandomizeSeed();
        RegenerateRooms();
    }

    [HorizontalGroup("ActionButtons", Width = 0.5f)]
    [Button("♻️ Regenerate Dungeon", ButtonSizes.Large), GUIColor(0.2f, 1f, 0.6f)]
    [PropertyOrder(-1)]
    [PropertySpace(SpaceAfter = 10)]
    private void RegenerateRooms()
    {
        rooms.Clear();
        rnd = new System.Random(seed);
        rooms.Add(new Room(new RectInt(startPoint.x, startPoint.y, dungeonSize.x, dungeonSize.y), SplitMethod.Horizontaly, Color.green));
        walls.Clear();
        doors.Clear();
        graph.DropTable();

        var failStreak = 0;
        // Add new split room to the list
        for (var i = 0; i < divisions; i++)
        {
            if (endlessDivisions) divisions++;
            
            var tempRoom = rooms[0];
            rooms.RemoveAt(0);
            if (tempRoom.rectBounds.width > sizeConstrain * 2 && tempRoom.rectBounds.height > sizeConstrain * 2)
            {
                // Do nothing
            }
            else if (tempRoom.rectBounds.width < sizeConstrain * 2
                && tempRoom.rectBounds.height > sizeConstrain * 2
                && tempRoom.rectBounds.width * acceptableRatio < tempRoom.rectBounds.height)
            {
                tempRoom.splitMethod = SplitMethod.Horizontaly;
            }
            else if (tempRoom.rectBounds.height < sizeConstrain * 2
                     && tempRoom.rectBounds.width > sizeConstrain * 2
                     && tempRoom.rectBounds.height * acceptableRatio < tempRoom.rectBounds.width)
            {
                tempRoom.splitMethod = SplitMethod.Verticaly;
            }
            else
            {
                failStreak++;
                rooms.Add(tempRoom);
                if (failStreak >= rooms.Count)
                {
                    AfterGeneration();
                    return;
                }
                continue;
            }
            failStreak = 0;
            rooms.AddRange(SplitRoom(tempRoom, tempRoom.splitMethod, wallWidth));
        }
    }

    private void AfterGeneration()
    {
        BuildTheWall();
        PlaceDoors();
        MakeConnections();
        rooms.Sort((r1, r2) => r1.GetSize().CompareTo(r2.GetSize()));
        CuttingRooms();
        CleanUpDoors();
    }
    private void Update()
    {
        if (showFloor)
        {
            foreach (var room in rooms)
            {
                if (!room.enabled) continue;
                AlgorithmsUtils.DebugRectInt(room.rectBounds, room.color);
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
                if (!door.enabled) continue;
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
                var newRoom1 = new RectInt(room.rectBounds.x, room.rectBounds.y, rnd.Next(sizeConstrain, room.rectBounds.width - sizeConstrain) + offset,  room.rectBounds.height);
                var newRoom2 = new RectInt(newRoom1.xMax - offset, room.rectBounds.y, room.rectBounds.width - newRoom1.width + offset, room.rectBounds.height);
                newRooms.Add(new Room(newRoom1, SplitMethod.Horizontaly, Color.cyan));
                newRooms.Add(new Room(newRoom2, SplitMethod.Horizontaly, Color.cyan));
                break;
            case SplitMethod.Horizontaly:
                newRoom1 = new RectInt(room.rectBounds.x, room.rectBounds.y, room.rectBounds.width, rnd.Next(sizeConstrain, room.rectBounds.height - sizeConstrain) + offset);
                newRoom2 = new RectInt(room.rectBounds.x, newRoom1.yMax - offset, room.rectBounds.width, room.rectBounds.height - newRoom1.height + offset);
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

                if (!AlgorithmsUtils.Intersects(firstRoom.rectBounds, secondRoom.rectBounds)) continue;
                
                var intersect = AlgorithmsUtils.Intersect(firstRoom.rectBounds, secondRoom.rectBounds);
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
                firstRoom.walls.Add(tempWall);
                secondRoom.walls.Add(tempWall);
                tempWall.rooms.Add(firstRoom);
                tempWall.rooms.Add(secondRoom);
                walls.Add(tempWall);
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
                    wall.door = tempDoor;
                    doors.Add(tempDoor);
                    break;
                case DoorDirection.Z:
                    var xMax = wall.bounds.xMax - wallWidth - doorWidth;
                    var xMin = wall.bounds.xMin + wallWidth;
                    tempBounds = new BoundsInt(new Vector3Int(rnd.Next(xMin, xMax), 0, wall.bounds.zMin - doorOffset), new Vector3Int(doorWidth, wallHeight + doorOffset, wallWidth + doorOffset * 2));
                    tempDoor = new Door(tempBounds, Color.yellow);
                    wall.door = tempDoor;
                    doors.Add(tempDoor);
                    break;
            }
        }
    }

    private void MakeConnections()
    {
        foreach (var wall in walls)
        {
            if (wall.doorDirection is DoorDirection.None) continue;
            
            graph.AddNode(wall.rooms[0]);
            graph.AddNode(wall.rooms[1]);

            var connection1 = new Connection(wall.rooms[1], wall.door);
            var connection2 = new Connection(wall.rooms[0], wall.door);
            graph.AddEdge(wall.rooms[0], connection1);
            graph.AddEdge(wall.rooms[1], connection2);
        }
    }

    private void CuttingRooms()
    {
        var targetAmount = rooms.Count - (int)(((float)rooms.Count / 100) * subtractedPercent);
        while (rooms.Count(a => a.enabled) > targetAmount)
        {
            var room = rooms.First(a => a.enabled);
            room.Disable();
            if (BFS(graph.GetList().Keys.First(a => a.enabled))) continue;
            room.Enable();
            return;
        }
    }

    private void CleanUpDoors()
    {
        var queue = new Queue<Door>();
        foreach (var door in doors)
        {
            queue.Enqueue(door);
        }
        while (queue.Count > 0)
        {
            var door = queue.Dequeue();
            door.enabled = false;
            if (!BFS(graph.GetList().Keys.First(a => a.enabled))) door.enabled = true;
        }
    }

    private bool BFS(Node startNode)
    {
        if (!startNode.enabled) return false;
        
        var queue = new Queue<Node>();
        queue.Enqueue(startNode);
        var visited = new HashSet<Node> { startNode };
        while (queue.Count > 0)
        {
            var currentNode = queue.Dequeue();
            foreach (var connection in graph.GetList()[currentNode])
            {
                if (!connection.node.enabled || !connection.via.enabled) continue;
                if (visited.Add(connection.node)) queue.Enqueue(connection.node);
            }
        }
        return visited.Count == rooms.Count(room => room.enabled);
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
                if (!room.enabled) continue;
                var color = room.color;
                color.a = 0.3f;
                Gizmos.color = color;
                Vector3 center = new Vector3(room.rectBounds.center.x, 0, room.rectBounds.center.y);
                var size = new Vector3(room.rectBounds.size.x, 0.01f, room.rectBounds.size.y);
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
                if (!door.enabled) continue;
                var color = door.color;
                color.a = 0.3f;
                Gizmos.color = color;
                Vector3 center = door.bounds.center;
                Gizmos.DrawCube(center, door.bounds.size);
            }
        }
        
        var connectionsList = graph.GetList();
        Gizmos.color = Color.white;
        foreach (var node in connectionsList.Keys)
        {
            if (showNodes)
            {
                if (!node.enabled) continue;
                Gizmos.DrawSphere(node.bounds.center, 1);
            }
            foreach (var connection in connectionsList[node])
            {
                if (showEdges)
                {
                    if (!connection.node.enabled || !connection.via.enabled) continue;
                    Gizmos.DrawLine(node.bounds.center, connection.via.bounds.center);
                    Gizmos.DrawLine(connection.via.bounds.center, connection.node.bounds.center);
                }
            }
        }
    }
}
