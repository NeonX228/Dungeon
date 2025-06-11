using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Unity.AI.Navigation;
using Random = UnityEngine.Random;

#region InternalClasses&Enums

/// <summary>
/// Specifies the mode of dungeon generation.
/// </summary>
public enum GenerationMode
{
    /// <summary>
    /// Represents the mode of dungeon generation where the entire process is completed immediately
    /// without yielding or waiting. This mode is suitable for scenarios where the generation speed
    /// and immediate availability of the dungeon layout are prioritized over progressive generation.
    /// </summary>
    [LabelText(SdfIconType.LightningChargeFill)] Instant,

    /// <summary>
    /// Represents a generation mode where dungeon generation occurs incrementally over multiple frames.
    /// This mode is designed to divide the dungeon creation process into smaller tasks utilizing coroutines,
    /// which can help prevent the application from becoming unresponsive during complex operations.
    /// </summary>
    [LabelText(SdfIconType.MoonFill)] Coroutine
}

/// <summary>
/// Represents the method of splitting regions in a dungeon generation process.
/// </summary>
internal enum SplitMethod
{
    /// <summary>
    /// Specifies the vertical splitting method for dividing a room in the dungeon generation process.
    /// </summary>
    /// <remarks>
    /// When this method is used, the room is divided along the vertical axis into two smaller rooms.
    /// The resulting subrooms will have equal or configured bounds as determined by the generation logic.
    /// This splitting method is commonly used to alternate with horizontal splitting for recursive room
    /// division in procedural dungeon generation.
    /// </remarks>
    Verticaly,

    /// <summary>
    /// Represents the horizontal split method for partitioning a room or dungeon area.
    /// This method divides a room or area along its horizontal axis, effectively splitting
    /// it into a top and bottom section. Used primarily in procedural dungeon generation
    /// to create structured layouts.
    /// </summary>
    Horizontaly,
}

/// <summary>
/// Specifies the direction of a door in a dungeon generation system.
/// </summary>
internal enum DoorDirection
{
    /// <summary>
    /// Represents a door direction along the X-axis within the dungeon.
    /// </summary>
    X,

    /// <summary>
    /// Represents a door direction that is aligned along the Z-axis.
    /// Used to specify the orientation of a door in relation to the
    /// Z-axis within the dungeon generation system.
    /// </summary>
    Z,

    /// <summary>
    /// Represents the absence of a door direction.
    /// </summary>
    /// <remarks>
    /// The <c>None</c> value is used to indicate that a given wall or boundary does not have a door or is not designated
    /// for door placement. This is typically used as a default or fallback state in the context of the dungeon generation logic.
    /// </remarks>
    None
}

/// <summary>
/// Defines the types of wall prefabs used in the dungeon generation system.
/// </summary>
public enum WallPrefabType
{
    /// <summary>
    /// Represents a wall prefab type designed for a '+' shaped intersection in the dungeon layout.
    /// This type is used when four connecting pathways meet at a single point, forming
    /// a crossroad-like structure.
    /// </summary>
    IntersectionPlus,

    /// <summary>
    /// Represents a T-shaped wall intersection in the dungeon layout.
    /// This type of wall prefab is typically used to create a junction where three wall segments meet,
    /// forming a shape resembling the letter "T".
    /// </summary>
    IntersectionT,

    /// <summary>
    /// Represents a corner wall type prefab used in the dungeon generation process.
    /// This enum member is utilized for defining corner-shaped wall tiles
    /// within the procedural dungeon creation system.
    /// </summary>
    Corner,

    /// <summary>
    /// Represents a wall prefab type used for creating long continuous wall segments in the dungeon generation process.
    /// </summary>
    LongWall,

    /// <summary>
    /// Represents a short wall type used in dungeon generation.
    /// </summary>
    /// <remarks>
    /// This enum member is commonly used to designate prefabs for creating shorter wall sections
    /// within the dungeon layout. It is part of the WallPrefabType enumeration utilized in the dungeon
    /// generator to map specific wall configurations.
    /// </remarks>
    ShortWall,

    /// <summary>
    /// Represents a pillar wall prefab type in the dungeon generation system.
    /// This type is used for placing vertical, standalone structural elements
    /// to enhance the architectural realism and provide support-like visual effects
    /// in generated dungeons.
    /// </summary>
    Pillar
}

/// <summary>
/// Represents a connection between two nodes in a dungeon graph.
/// </summary>
internal class Connection
{
    /// <summary>
    /// Represents a connection between two nodes in the dungeon graph.
    /// </summary>
    public Connection(Node to, Node door)
    {
        Node = to;
        Via = door;
    }

    /// <summary>
    /// Represents a node within the graph structure used in dungeon generation.
    /// </summary>
    /// <remarks>
    /// A node typically has a set of associated properties, such as its enabled state, visual representation, and spatial bounds.
    /// It can represent various components within the dungeon, such as rooms, corridors, or other spatial elements.
    /// </remarks>
    public Node Node;

    /// <summary>
    /// Represents a node that acts as a connecting point or door in a graph structure.
    /// This variable is used within the context of dungeon generation to signify the connecting point
    /// or passage between two nodes (or rooms) in a dungeon layout.
    /// </summary>
    public Node Via;
}

/// <summary>
/// Represents a logical node within the dungeon generation system.
/// This class serves as a base for other specific node types such as
/// Rooms, Walls, and Doors.
/// </summary>
internal class Node
{
    /// <summary>
    /// Indicates whether the node is currently enabled or not.
    /// This property is used to determine if operations or functionalities
    /// specific to the node, such as enabling/disabling associated doors or walls,
    /// should be performed.
    /// </summary>
    [HideInInspector]
    public bool Enabled = true;

    /// <summary>
    /// Represents a color value associated with the node, such as walls, rooms,
    /// or other elements within the dungeon generation system.
    /// </summary>
    /// <remarks>
    /// This variable is used to define the visual representation of the object in the Unity Editor
    /// and for distinguishing between different elements during dungeon generation.
    /// </remarks>
    [HideInInspector]
    public Color Color;

    /// <summary>
    /// Represents the bounding volume for a node in the dungeon generation process.
    /// Used for defining spatial dimensions and size of rooms, walls, or other structures
    /// within the dungeon. Handles 3D coordinates and can be visualized or manipulated
    /// through various helper methods.
    /// </summary>
    [HideInInspector]
    public BoundsInt Bounds;

    /// Highlights the bounds of the current node using a specified color and visualization parameters.
    /// This method uses a debugging utility to visually render the bounds of the node in the scene
    /// with a magenta color. It is primarily intended for debugging and visually inspecting
    /// the area covered by the node.
    /// Note:
    /// - The visibility of the visualized bounds depends on the scene setup and how the debug
    /// visualization is implemented.
    /// - Use this method to ensure the node's bounds are correctly calculated and placed.
    /// Dependencies:
    /// - Calls `AlgorithmsUtils.DebugBoundsInt` to perform the bound visualization.
    /// Parameters:
    /// None.
    [Button(ButtonSizes.Small, ButtonStyle.Box, Icon = SdfIconType.Search)]
    [GUIColor(0.8f, 0.8f, 1f)]
    [InfoBox("@\"Size: \" + GetSize().ToString()")]
    [PropertyOrder(-1)]
    public void Highlight()
    {
        AlgorithmsUtils.DebugBoundsInt(Bounds, Color.magenta, 3f);
    }

    /// <summary>
    /// Calculates the size of the node based on its bounds.
    /// </summary>
    /// <returns>
    /// The calculated size of the node, which is twice the sum of the x and z dimensions of the node's bounds.
    /// </returns>
    public int GetSize()
    {
        return 2 * (Bounds.size.x + Bounds.size.z);
    }
}

/// <summary>
/// Represents a room within a dungeon, extending functionality from the <see cref="Node"/> class.
/// </summary>
internal class Room: Node
{
    /// <summary>
    /// Instance of the <see cref="DungeonGenerator"/> class used to generate and manage dungeon structures.
    /// </summary>
    private DungeonGenerator generator;

    /// <summary>
    /// Represents a room within the dungeon, which is a subtype of a node.
    /// </summary>
    public Room(RectInt roomRect, SplitMethod lastSplitMethod, Color roomColor, DungeonGenerator generator)
    {
        this.generator = generator;
        Bounds = new BoundsInt(new Vector3Int(roomRect.x, 0, roomRect.y), new Vector3Int(roomRect.width, 0, roomRect.height));
        RectBounds = roomRect;
        SplitMethod = lastSplitMethod;
        Color = roomColor;
    }

    /// <summary>
    /// Represents the rectangular bounds of a room in grid coordinates.
    /// </summary>
    [HideInInspector]
    public RectInt RectBounds;

    /// <summary>
    /// Defines the method of splitting used to divide rooms in the dungeon generation process.
    /// </summary>
    [HideInInspector]
    public SplitMethod SplitMethod;

    /// <summary>
    /// A collection of <see cref="Wall"/> objects associated with a <see cref="Room"/>.
    /// This collection defines the walls that enclose the room and links to adjacent rooms
    /// through their shared walls.
    /// </summary>
    [HideInInspector]
    public List<Wall> Walls = new();

    /// <summary>
    /// Disables the room and its associated functionality.
    /// This method sets the room's Enabled status to false, disables any associated doors in its walls,
    /// resets the DoorDirection for each wall to None, and changes the color of all its walls to red.
    /// </summary>
    [EnableIf("Enabled")]
    [HorizontalGroup("Toggle")]
    [Button(ButtonSizes.Medium), GUIColor("@enabled ? Color.red : Color.gray")]
    public void Disable()
    {
        Enabled = false;
        foreach (var wall in Walls)
        {
            if (wall.DoorDirection != DoorDirection.None)
            {
                wall.Door.Enabled = false;
                wall.DoorDirection = DoorDirection.None;
            }
            wall.Color = Color.red;
        }
    }

    /// <summary>
    /// Enables the current room and evaluates associated walls for potential door placement.
    /// </summary>
    [DisableIf("Enabled")]
    [HorizontalGroup("Toggle")]
    [Button(ButtonSizes.Medium), GUIColor("@!enabled ? Color.green : Color.gray")]
    public void Enable()
    {
        Enabled = true;
        foreach (var wall in Walls)
        {
            if (!wall.Rooms.All(room => room.Enabled)) continue;
            
            if (wall.Bounds.size.x > generator.doorWidth + generator.wallWidth * 2)
            {
                wall.Color = Color.green;
                wall.DoorDirection = DoorDirection.Z;
            }
            else if (wall.Bounds.size.z > generator.doorWidth + generator.wallWidth * 2)
            {
                wall.Color = Color.green;
                wall.DoorDirection = DoorDirection.X;
            }
            else
            {
                wall.Color = Color.red;
                wall.DoorDirection = DoorDirection.None;
            }
            
            if (wall.DoorDirection != DoorDirection.None) wall.Door.Enabled = true;
        }
    }
}

/// <summary>
/// Represents a door within a dungeon generation system. Extends functionality from the Node class.
/// </summary>
internal class Door: Node
{
    /// <summary>
    /// Represents a door entity in the dungeon generation process, inheriting from the Node class.
    /// </summary>
    public Door(BoundsInt doorBounds, Color doorColor)
    {
        Bounds = doorBounds;
        Color = doorColor;
    }
}

/// <summary>
/// Represents a wall in the dungeon. A wall is defined by its bounds, color, associated rooms,
/// and an optional door and door direction. It serves as a structural component in the dungeon generation process.
/// </summary>
internal class Wall: Node
{
    /// <summary>
    /// Represents a wall in the dungeon, which is defined by its bounds, color, and door direction.
    /// </summary>
    public Wall(BoundsInt wallBounds, Color wallColor, DoorDirection targetDoorDirection)
    {
        Bounds = wallBounds;
        Color = wallColor;
        DoorDirection = targetDoorDirection;
    }

    /// <summary>
    /// An enumeration that specifies the possible directions of a door in a dungeon environment.
    /// </summary>
    /// <remarks>
    /// - X: Represents a door that is aligned along the X-axis.
    /// - Z: Represents a door that is aligned along the Z-axis.
    /// - None: Represents the absence of a door or an undefined door direction.
    /// </remarks>
    [HideInInspector]
    public DoorDirection DoorDirection;

    /// <summary>
    /// Represents a door in a dungeon layout, used to connect different rooms within the dungeon structure.
    /// </summary>
    /// <remarks>
    /// A <see cref="Door"/> is defined by its bounds and can be enabled or disabled depending on the overall dungeon structure.
    /// Doors are placed dynamically based on the configuration of the walls and the required connectivity between rooms.
    /// </remarks>
    /// <example>
    /// A door may connect two rooms if the corresponding wall's door direction allows placement.
    /// </example>
    [HideInInspector]
    public Door Door;

    /// <summary>
    /// Represents a list of rooms connected to a wall in the dungeon generation system.
    /// </summary>
    /// <remarks>
    /// The <c>Rooms</c> variable is a collection used to define the rooms linked to a specific wall.
    /// It allows the dungeon generation process to identify and manage relationships between rooms and their shared boundaries.
    /// </remarks>
    [HideInInspector]
    public List<Room> Rooms = new();
}

/// <summary>
/// Represents a graph data structure where nodes are identified by keys
/// and edges connect nodes to associated values.
/// </summary>
public class Graph<TKey, TValue>
{
    /// <summary>
    /// Represents a dictionary-based adjacency list used to store graph relationships.
    /// Each key represents a node in the graph, and its associated value is a list of connected nodes.
    /// </summary>
    private Dictionary<TKey, List<TValue>> adjacencyList = new();

    /// <summary>
    /// Adds a node to the graph if it does not already exist.
    /// </summary>
    /// <param name="node">The node to be added to the graph.</param>
    public void AddNode(TKey node)
    {
        if (adjacencyList.ContainsKey(node)) return;

        adjacencyList.Add(node, new List<TValue>());
    }

    /// <summary>
    /// Adds an edge between the specified nodes in the graph.
    /// </summary>
    /// <param name="fromNode">The source node from which the edge originates.</param>
    /// <param name="toNode">The destination node to which the edge points.</param>
    public void AddEdge(TKey fromNode, TValue toNode)
    {
        if (!adjacencyList.ContainsKey(fromNode))
        {
            adjacencyList.Add(fromNode, new List<TValue>());
        }

        adjacencyList[fromNode].Add(toNode);
    }

    /// <summary>
    /// Retrieves the adjacency list that represents the graph structure.
    /// </summary>
    /// <returns>
    /// A dictionary where the key is a node of type <typeparamref name="TKey"/> and the value is a list of connected nodes of type <typeparamref name="TValue"/>.
    /// </returns>
    public Dictionary<TKey, List<TValue>> GetList(){ return adjacencyList; }

    /// <summary>
    /// Removes all nodes and edges from the graph by clearing the adjacency list.
    /// </summary>
    public void DropTable() {adjacencyList.Clear();}
}

#endregion

/// <summary>
/// The DungeonGenerator class is responsible for procedurally generating a dungeon layout based
/// on configurable parameters such as size, structure, division, and modeling. It supports both instant
/// and coroutine-based generation modes and offers robust customization options for walls, doors, and rooms.
/// </summary>
[ExecuteAlways]
public class DungeonGenerator : SerializedMonoBehaviour
{
    #region PublicVariables

    /// <summary>
    /// Specifies the mode of dungeon generation.
    /// </summary>
    /// <remarks>
    /// Determines whether the dungeon generation process is performed instantly
    /// or over multiple frames using a coroutine. This can influence user experience
    /// and performance depending on the chosen mode.
    /// </remarks
    [PropertyOrder(-2)]
    [EnumToggleButtons, HideLabel]
    public GenerationMode generationMode = GenerationMode.Instant;

    /// <summary>
    /// Represents the starting point of the area in grid coordinates.
    /// </summary>
    /// <remarks>
    /// This variable defines the origin point from where the dungeon generation begins.
    /// Adjusting this value changes the initial position of the generated dungeon area.
    /// </remarks>
    [TabGroup("Settings", "General", SdfIconType.GearFill)]
    [BoxGroup("Settings/General/Area Bounds")]
    [LabelText("Starting point")]
    [Tooltip("Starting point of the area (in grid coordinates).")]
    [DisableIf("seed")]
    public Vector2Int startPoint = new(0, 0);

    /// <summary>
    /// Represents the dimensions of the dungeon in grid units.
    /// </summary>
    /// <remarks>
    /// The x and y values correspond to the width and height of the dungeon, respectively.
    /// This value defines the overall area within which rooms, walls, and other structures will be generated.
    /// </remarks>
    [BoxGroup("Settings/General/Area Bounds")]
    [LabelText("Size")]
    [Tooltip("The size of the dungeon.")]
    public Vector2Int dungeonSize = new(500, 200);

    /// <summary>
    /// The <see cref="NavMeshSurface"/> component responsible for handling the baking of the navigation mesh
    /// within the generated dungeon environment. This allows AI agents to navigate the generated area.
    /// </summary>
    [BoxGroup("Settings/General/Misc")]
    [LabelText("NavMesh Component")]
    [Required]
    public NavMeshSurface navMesh;

    /// <summary>
    /// The transform parent object which holds all the generated dungeon meshes.
    /// This is used as a container for organizing and managing the mesh hierarchy,
    /// such as walls, floors, and other structural components of the dungeon.
    /// </summary>
    [BoxGroup("Settings/General/Misc")]
    [LabelText("Mesh Parental Object")]
    [Required]
    public Transform meshes;

    /// <summary>
    /// Represents the transform of the player object within the dungeon.
    /// This is used to position or reference the player's location in the generated dungeon.
    /// </summary>
    [BoxGroup("Settings/General/Misc")]
    [LabelText("Player Object")]
    [Required]
    public Transform player;

    /// <summary>
    /// Represents the time delay, in seconds, used between coroutine executions
    /// during the procedural generation process. This delay allows for
    /// staggered step-by-step visual creation of dungeon components such
    /// as rooms, walls, doors, and connections, providing better debugging
    /// and visualization value.
    /// </summary>
    [BoxGroup("Settings/General/Misc")]
    [PropertyRange(float.Epsilon, 1f)]
    public float coroutineDelay = 0.1f;

    /// <summary>
    /// A flag that determines whether divisions for generating rooms in the dungeon are infinite.
    /// If set to true, the room subdivision process in the dungeon generation logic will
    /// continue indefinitely as long as other constraints (e.g., size constraints) allow.
    /// If set to false, the number of divisions is limited by the <c>divisions</c> property.
    /// </summary>
    [TabGroup("Settings", "Division", SdfIconType.LayoutThreeColumns)]
    [BoxGroup("Settings/Division/Config")]
    [MinValue(1), LabelText("Endless Divisions")]
    [Tooltip("How many times the room can be divided.")]
    public bool endlessDivisions = false;

    /// <summary>
    /// Represents the number of divisions applied to a dungeon during generation.
    /// Determines how many times the dungeon can be split into rooms, directly influencing
    /// the complexity and layout structure of the dungeon. The value can be adjusted
    /// dynamically during runtime within the defined constraints. This variable is ignored
    /// if <see cref="endlessDivisions"/> is set to true.
    /// </summary>
    [TabGroup("Settings", "Division", SdfIconType.LayoutThreeColumns)]
    [BoxGroup("Settings/Division/Config")]
    [MinValue(1), LabelText("Division Count")]
    [Tooltip("How many times the room can be divided.")]
    [HideIf("endlessDivisions")]
    public int divisions = 1;

    /// <summary>
    /// Defines the minimum allowable size for a room in the dungeon generation process.
    /// Rooms smaller than this size will be discarded during generation.
    /// </summary>
    [BoxGroup("Settings/Division/Config")]
    [MinValue(1), LabelText("Minimum Room Size")]
    [Tooltip("Rooms smaller than this size will be discarded.")]
    public int sizeConstrain = 30;

    /// <summary>
    /// The maximum allowed ratio between the width and height of a room.
    /// Rooms with dimensions exceeding this aspect ratio may be considered invalid
    /// when dividing and generating the dungeon layout.
    /// </summary>
    [BoxGroup("Settings/Division/Config")]
    [Range(1f, 5f), LabelText("Acceptable Aspect Ratio")]
    [Tooltip("Max allowed ratio between width and height of a room.")]
    public float acceptableRatio = 1.5f;

    /// <summary>
    /// Represents the width of the generated wall in the dungeon, measured in grid units.
    /// </summary>
    /// <remarks>
    /// This variable is utilized in the dungeon generation process to determine the thickness of walls,
    /// influencing room boundaries and structural calculations.
    /// </remarks>
    /// <value>
    /// An integer value greater than or equal to 0, where larger values result in thicker walls.
    /// </value>
    [TabGroup("Settings", "Structure", SdfIconType.HouseFill)]
    [BoxGroup("Settings/Structure/Walls & Doors")]
    [MinValue(0), LabelText("Wall Width")]
    [Tooltip("Width of the generated wall (in grid units).")]
    [ShowInInspector]
    public int wallWidth = 1;

    /// <summary>
    /// Specifies the height of the generated walls in the dungeon, measured in Unity units.
    /// </summary>
    /// <remarks>
    /// This value determines the vertical size of walls created during the dungeon generation process.
    /// It is used to set the height of wall boundaries and ensure consistent scaling within the dungeon.
    /// </remarks>
    [BoxGroup("Settings/Structure/Walls & Doors")]
    [MinValue(0), LabelText("Wall Height")]
    [Tooltip("Height of the generated wall (in Unity units).")]
    [ShowInInspector]
    public int wallHeight = 1;

    /// <summary>
    /// Represents the width of doors that connect rooms in the dungeon.
    /// </summary>
    /// <remarks>
    /// This value determines the size of door openings within the generated dungeon.
    /// It is critical for calculating wall configurations and ensuring proper room connectivity.
    /// </remarks>
    [BoxGroup("Settings/Structure/Walls & Doors")]
    [MinValue(1), LabelText("Door Width")]
    [Tooltip("Width of doors that connect rooms.")]
    [ShowInInspector]
    public int doorWidth = 1;

    /// <summary>
    /// Specifies the offset value for the placement of doors relative to the walls.
    /// A non-negative integer value that determines how far doors are positioned from
    /// their default placement, affecting their alignment within the dungeon structure.
    /// </summary>
    [BoxGroup("Settings/Structure/Walls & Doors")]
    [MinValue(0), LabelText("Door Offset")]
    [ShowInInspector]
    public int doorOffset = 0;

    /// <summary>
    /// The percentage of rooms in the dungeon that will be removed during the generation process.
    /// This value is used to calculate the number of rooms to disable while maintaining the overall structure.
    /// </summary>
    [BoxGroup("Settings/Structure/Rooms")]
    [MinValue(0), MaxValue(100), LabelText("Subtracted Percent")]
    public int subtractedPercent = 10;

    /// <summary>
    /// The seed value used to initialize the procedural generation of the dungeon.
    /// A fixed seed ensures consistent and repeatable results for the generated layout,
    /// while varying the seed produces different dungeon structures.
    /// </summary>
    [TabGroup("Settings", "Randomization", SdfIconType.Dice6Fill)]
    [BoxGroup("Settings/Randomization/Seed")]
    [InlineButton("RandomizeSeed", SdfIconType.Dice6Fill, "Randomize")]
    [LabelText("Seed Value")]
    [Tooltip("Seed used to initialize the procedural generation.")]
    public int seed = 1;

    /// <summary>
    /// An array of floor tile prefabs used for generating the floor of the dungeon.
    /// </summary>
    /// <remarks>
    /// Each prefab in the array can be randomly selected for placement during the procedural dungeon generation process.
    /// The selected prefab is instantiated at the appropriate grid coordinates within the dungeon.
    /// </remarks>
    [TabGroup("Settings", "Modeling", SdfIconType.Bricks)]
    [BoxGroup("Settings/Modeling/Prefabs")]
    [LabelText("Floor Tile Prefab")]
    [Required]
    public GameObject[] floorPrefab;

    /// <summary>
    /// A dictionary that maps different types of wall prefabs to collections of GameObjects.
    /// Each WallPrefabType key corresponds to a specific type of wall (e.g., intersection, corner)
    /// and its associated array of GameObject prefabs.
    /// </summary>
    [TabGroup("Settings", "Modeling", SdfIconType.Bricks)]
    [BoxGroup("Settings/Modeling/Prefabs")]
    [LabelText("Walls Tile Prefabs")]
    [DictionaryDrawerSettings()]
    [Required]
    public Dictionary<WallPrefabType, GameObject[]> wallsPrefabs = new();

    /// <summary>
    /// A boolean flag indicating whether or not to display labels on certain elements
    /// during visualization in the editor. When enabled, labels such as object names,
    /// dimensions, or other debug-related information may be shown based on the
    /// current rendering context (e.g., walls, floors, doors, or other dungeon structures).
    /// </summary
    [TabGroup("Settings", "Debug", SdfIconType.BugFill)]
    [BoxGroup("Settings/Debug/Visibility")]
    [LabelText("Show Labels")]
    [GUIColor("@showLabels ? new Color(1f, 0.8f, 0.4f) : Color.gray")]
    public bool showLabels;

    /// <summary>
    /// Indicates whether doors should be visually displayed in the generated dungeon.
    /// </summary>
    /// <remarks>
    /// This boolean controls the visibility of doors in the dungeon generation process.
    /// When set to true, doors are rendered in both the scene view and during gizmos drawing.
    /// When false, doors are hidden from view. The value may be toggled during gameplay
    /// or generation cycles to aid in debugging or visualization.
    /// </remarks>
    [BoxGroup("Settings/Debug/Visibility")]
    [LabelText("Show Doors")]
    [GUIColor("@showDoors ? new Color(0.6f, 1f, 0.6f) : Color.gray")]
    public bool showDoors;

    /// <summary>
    /// Indicates whether the floor of the dungeon should be displayed visually.
    /// This boolean flag can be toggled to show or hide the dungeon floor during the generation process or within the editor.
    /// </summary>
    [BoxGroup("Settings/Debug/Visibility")]
    [LabelText("Show Floor")]
    [GUIColor("@showFloor ? new Color(0.5f, 0.9f, 1f) : Color.gray")]
    public bool showFloor;

    /// <summary>
    /// Determines whether the walls of the dungeon should be displayed in the debug view.
    /// When enabled, walls will be visualized during both the generation and debugging processes.
    /// </summary>
    [BoxGroup("Settings/Debug/Visibility")]
    [LabelText("Show Walls")]
    [GUIColor("@showWalls ? new Color(1f, 0.5f, 0.5f) : Color.gray")]
    public bool showWalls;

    /// <summary>
    /// Determines whether the nodes within the dungeon generator are visually displayed
    /// during the debugging or visualization process.
    /// </summary>
    /// <remarks>
    /// When enabled, node positions and connections will be rendered, aiding in testing
    /// and debugging the dungeon generation process. This property influences the appearance
    /// of nodes in the Unity Editor, especially in conjunction with Gizmos rendering within
    /// the dungeon generator implementation.
    /// </remarks>
    [BoxGroup("Settings/Debug/Visibility")]
    [LabelText("Show Nodes")]
    [GUIColor("@showNodes ? new Color(0.7f, 0.7f, 1f) : Color.gray")]
    public bool showNodes;

    /// <summary>
    /// Indicates whether to display edges within the dungeon generation process.
    /// </summary>
    /// <remarks>
    /// This boolean flag determines the visibility of edges, typically used for debugging
    /// or visualization purposes during dungeon creation. Edges are displayed when the value
    /// is set to true and hidden when false. The appearance of the edges may be color-coded
    /// based on the flag's state.
    /// </remarks
    [BoxGroup("Settings/Debug/Visibility")]
    [LabelText("Show Edges")]
    [GUIColor("@showEdges ? new Color(0.9f, 0.6f, 1f) : Color.gray")]
    public bool showEdges;

    #endregion

    #region PrivateVariables

    /// <summary>
    /// Represents a collection of rooms generated during the dungeon creation process.
    /// This list is used to store and manage all rooms that are created as part of the dungeon
    /// generation, including divisions and sub-divisions of the initial dungeon space.
    /// </summary>
    private List<Room> rooms = new();

    /// <summary>
    /// A collection of all the walls present in the dungeon.
    /// Walls represent the boundaries and partitions between rooms in the dungeon layout.
    /// </summary>
    private List<Wall> walls = new();

    /// <summary>
    /// Represents a collection of doors within the dungeon generated by the DungeonGenerator class.
    /// </summary>
    private List<Door> doors = new();

    /// <summary>
    /// Represents a private instance of the System.Random class used for generating random numbers.
    /// This is utilized in various dungeon generation processes such as splitting rooms randomly
    /// and picking random prefabs during procedural generation of the dungeon.
    /// </summary>
    private System.Random rnd;

    /// <summary>
    /// Represents a graph structure used within the dungeon generation process.
    /// </summary>
    private Graph<Node, Connection> graph = new();

    /// <summary>
    /// A list of positions within the dungeon that are considered safe for spawning entities.
    /// </summary>
    private List<Vector3> safeSpawnPoints = new();

    /// <summary>
    /// Represents a two-dimensional matrix used for internal computation and representation of dungeon structures.
    /// </summary>
    private int[,] matrix;

    #endregion
    
    private void Start()
    {
        if (endlessDivisions)
        {
            divisions = 10;
        }
    }

    #region Initializators
    
    bool isInstant() => generationMode == GenerationMode.Instant;

    /// <summary>
    /// Starts a coroutine to generate a new dungeon based on current settings.
    /// This method initializes the process by randomizing the seed, setting the initial status,
    /// and starting the room generation coroutine.
    /// </summary>
    [HorizontalGroup("ActionButtons", Width = 0.5f)]
    [Button("🎯 Generate Dungeon (c)", ButtonSizes.Large), GUIColor(0.3f, 0.7f, 1f)]
    [PropertyOrder(-1)]
    [PropertySpace(SpaceAfter = 10)]
    [HideIf("isInstant")]
    public void NewGenerationCoroutine()
    {
        RandomizeSeed();
        //navMesh.RemoveData();
        StartCoroutine(GenerateRoomsCoroutine());
    }

    /// <summary>
    /// Initializes and generates a new dungeon layout.
    /// This method sets up new parameters for the dungeon generation
    /// and initiates the room generation process using the configured settings.
    /// </summary>
    [HorizontalGroup("ActionButtons", Width = 0.5f)]
    [Button("🎯 Generate Dungeon", ButtonSizes.Large), GUIColor(0.3f, 0.7f, 1f)]
    [PropertyOrder(-1)]
    [PropertySpace(SpaceAfter = 10)]
    [ShowIf("isInstant")]
    public void NewGeneration()
    {
        RandomizeSeed();
        //navMesh.RemoveData();
        GenerateRooms();
    }

    /// <summary>
    /// Initiates a coroutine-based regeneration process for generating dungeon rooms.
    /// This method updates the status of the dungeon generation process and starts the coroutine
    /// responsible for creating rooms asynchronously, allowing for step-by-step execution.
    /// It is particularly useful for scenarios where non-instantaneous generation is required.
    /// </summary>
    [HorizontalGroup("ActionButtons", Width = 0.5f)]
    [Button("♻️ Regenerate Dungeon (c)", ButtonSizes.Large), GUIColor(0.2f, 1f, 0.6f)]
    [PropertyOrder(-1)]
    [PropertySpace(SpaceAfter = 10)]
    [HideIf("isInstant")]
    public void RegenerationCoroutine()
    {
        //navMesh.RemoveData();
        StartCoroutine(GenerateRoomsCoroutine());
    }

    /// <summary>
    /// Regenerates the dungeon layout by reinitializing the dungeon generation process.
    /// This method resets the generation status and triggers the room generation logic.
    /// Only available when the generation mode is set to "Instant."
    /// </summary>
    [HorizontalGroup("ActionButtons", Width = 0.5f)]
    [Button("♻️ Regenerate Dungeon", ButtonSizes.Large), GUIColor(0.2f, 1f, 0.6f)]
    [PropertyOrder(-1)]
    [PropertySpace(SpaceAfter = 10)]
    [ShowIf("isInstant")]
    public void Regeneration()
    {
        //navMesh.RemoveData();
        GenerateRooms();
    }

    #endregion

    #region ToolkitMethods

    /// <summary>
    /// Performs a Breadth-First Search (BFS) from the given starting node and validates the connectivity
    /// of the graph ensuring all enabled rooms are reachable.
    /// </summary>
    private bool BFS(Node startNode)
    {
        if (!startNode.Enabled) return false;
        
        var queue = new Queue<Node>();
        queue.Enqueue(startNode);
        var visited = new HashSet<Node> { startNode };
        while (queue.Count > 0)
        {
            var currentNode = queue.Dequeue();
            foreach (var connection in graph.GetList()[currentNode])
            {
                if (!connection.Node.Enabled || !connection.Via.Enabled) continue;
                if (visited.Add(connection.Node)) queue.Enqueue(connection.Node);
            }
        }
        return visited.Count == rooms.Count(room => room.Enabled);
    }

    ///<summary>
    /// Splits the given room into two new rooms based on the specified splitting method and offset.
    /// </summary>
    private List<Room> SplitRoom(Room room, SplitMethod splitMethod, int offset)
    {
        var newRooms = new List<Room>();
        switch (splitMethod)
        {
            case SplitMethod.Verticaly:
                var newRoom1 = new RectInt(room.RectBounds.x, room.RectBounds.y, rnd.Next(sizeConstrain, room.RectBounds.width - sizeConstrain) + offset,  room.RectBounds.height);
                var newRoom2 = new RectInt(newRoom1.xMax - offset, room.RectBounds.y, room.RectBounds.width - newRoom1.width + offset, room.RectBounds.height);
                newRooms.Add(new Room(newRoom1, SplitMethod.Horizontaly, Color.cyan, this));
                newRooms.Add(new Room(newRoom2, SplitMethod.Horizontaly, Color.cyan, this));
                break;
            case SplitMethod.Horizontaly:
                newRoom1 = new RectInt(room.RectBounds.x, room.RectBounds.y, room.RectBounds.width, rnd.Next(sizeConstrain, room.RectBounds.height - sizeConstrain) + offset);
                newRoom2 = new RectInt(room.RectBounds.x, newRoom1.yMax - offset, room.RectBounds.width, room.RectBounds.height - newRoom1.height + offset);
                newRooms.Add(new Room(newRoom1, SplitMethod.Verticaly, Color.cyan, this));
                newRooms.Add(new Room(newRoom2, SplitMethod.Verticaly,Color.cyan, this));
                break;
        }
        return newRooms;
    }

    /// <summary>
    /// Randomizes the seed value used for procedural generation.
    /// </summary>
    private void RandomizeSeed()
    {
        seed = Random.Range(0, int.MaxValue);
    }

    /// <summary>
    /// Selects and returns a random prefab from the given array of prefabs.
    /// </summary>
    private GameObject PickRandomPrefab(GameObject[] prefabs)
    {
        return rnd.Next(prefabs.Length) == 0 ? prefabs[0] : prefabs[rnd.Next(prefabs.Length)];
    }

    /// <summary>
    /// Determines whether a specified submatrix within the dungeon matrix matches a given pattern.
    /// </summary>
    private bool MatchesPattern<T>(T[,] pattern, int startRow, int startCol)
    {
        int pRows = pattern.GetLength(0);
        int pCols = pattern.GetLength(1);

        for (int i = 0; i < pRows; i++)
        {
            for (int j = 0; j < pCols; j++)
            {
                if (!matrix[startRow + i, startCol + j].Equals(pattern[i, j]))
                    return false;
            }
        }

        return true;
    }

    #endregion

    #region RoomsGeneration

    /// <summary>
    /// Coroutine responsible for generating rooms in the dungeon layout.
    /// This method progressively divides the dungeon space into smaller rooms
    /// and updates the dungeon's visual and logical structure over time.
    /// </summary>
    private IEnumerator GenerateRoomsCoroutine()
    {
        showDoors = true;
        showFloor = true;
        showWalls = true;
        
        rooms.Clear();
        rnd = new System.Random(seed);
        rooms.Add(new Room(new RectInt(startPoint.x, startPoint.y, dungeonSize.x, dungeonSize.y), SplitMethod.Horizontaly, Color.green, this));
        walls.Clear();
        doors.Clear();
        graph.DropTable();
        safeSpawnPoints.Clear();
        while (meshes.childCount > 0) {
                Destroy(meshes.GetChild(0).gameObject);
                yield return null;
        }
        var failStreak = 0;
        for (var i = 0; i < divisions; i++)
        {
            if (endlessDivisions) divisions++;
            
            var tempRoom = rooms[0];
            rooms.RemoveAt(0);
            if (tempRoom.RectBounds.width > sizeConstrain * 2 && tempRoom.RectBounds.height > sizeConstrain * 2)
            {
                // Do nothing
            }
            else if (tempRoom.RectBounds.width < sizeConstrain * 2
                && tempRoom.RectBounds.height > sizeConstrain * 2
                && tempRoom.RectBounds.width * acceptableRatio < tempRoom.RectBounds.height)
            {
                tempRoom.SplitMethod = SplitMethod.Horizontaly;
            }
            else if (tempRoom.RectBounds.height < sizeConstrain * 2
                     && tempRoom.RectBounds.width > sizeConstrain * 2
                     && tempRoom.RectBounds.height * acceptableRatio < tempRoom.RectBounds.width)
            {
                tempRoom.SplitMethod = SplitMethod.Verticaly;
            }
            else
            {
                failStreak++;
                rooms.Add(tempRoom);
                if (failStreak >= rooms.Count * 2)
                {
                    StartCoroutine(AfterGenerationCoroutine());
                    yield break;
                }
                continue;
            }
            failStreak = 0;
            rooms.AddRange(SplitRoom(tempRoom, tempRoom.SplitMethod, wallWidth));
            yield return new WaitForSeconds(coroutineDelay);
        }
    }

    /// <summary>
    /// Generates the rooms for the dungeon based on the specified parameters.
    /// </summary>
    private void GenerateRooms()
    {
        rooms.Clear();
        rnd = new System.Random(seed);
        rooms.Add(new Room(new RectInt(startPoint.x, startPoint.y, dungeonSize.x, dungeonSize.y), SplitMethod.Horizontaly, Color.green, this));
        walls.Clear();
        doors.Clear();
        graph.DropTable();
        safeSpawnPoints.Clear();
        while (meshes.childCount > 0) {
            DestroyImmediate(meshes.GetChild(0).gameObject);
        }
        var failStreak = 0;
        for (var i = 0; i < divisions; i++)
        {
            if (endlessDivisions) divisions++;
            
            var tempRoom = rooms[0];
            rooms.RemoveAt(0);
            if (tempRoom.RectBounds.width > sizeConstrain * 2 && tempRoom.RectBounds.height > sizeConstrain * 2)
            {
                // Do nothing
            }
            else if (tempRoom.RectBounds.width < sizeConstrain * 2
                && tempRoom.RectBounds.height > sizeConstrain * 2
                && tempRoom.RectBounds.width * acceptableRatio < tempRoom.RectBounds.height)
            {
                tempRoom.SplitMethod = SplitMethod.Horizontaly;
            }
            else if (tempRoom.RectBounds.height < sizeConstrain * 2
                     && tempRoom.RectBounds.width > sizeConstrain * 2
                     && tempRoom.RectBounds.height * acceptableRatio < tempRoom.RectBounds.width)
            {
                tempRoom.SplitMethod = SplitMethod.Verticaly;
            }
            else
            {
                failStreak++;
                rooms.Add(tempRoom);
                if (failStreak >= rooms.Count * 2)
                {
                    AfterGeneration();
                    return;
                }
                continue;
            }
            failStreak = 0;
            rooms.AddRange(SplitRoom(tempRoom, tempRoom.SplitMethod, wallWidth));
        }
    }

    /// <summary>
    /// Coroutine executed after the generation of dungeon rooms.
    /// </summary>
    private IEnumerator AfterGenerationCoroutine()
    {
        yield return StartCoroutine(BuildingWallsCoroutine());
        yield return StartCoroutine(PlaceDoorsCoroutine());
        yield return StartCoroutine(MakeConnectionsCoroutine());
        rooms.Sort((r1, r2) => r1.GetSize().CompareTo(r2.GetSize()));
        yield return StartCoroutine(CuttingRoomsCoroutine());
        yield return StartCoroutine(CleanUpDoorsCoroutine());
        showDoors = false;
        showFloor = false;
        showWalls = false;
        showEdges = false;
        showNodes = false;
        yield return StartCoroutine(PlacingWallsMeshesCoroutine());
        yield return StartCoroutine(PlacingFloorMeshesCoroutine());
        PickSpawnLocation();
        navMesh.BuildNavMesh();
    }

    /// <summary>
    /// Performs a series of post-generation operations on the dungeon layout.
    /// </summary>
    private void AfterGeneration()
    {
        BuildingWalls();
        PlaceDoors();
        MakeConnections();
        rooms.Sort((r1, r2) => r1.GetSize().CompareTo(r2.GetSize()));
        CuttingRooms();
        CleanUpDoors();
        PlacingMeshes();
        PlacingFloorMeshes();
        PickSpawnLocation();
        navMesh.BuildNavMesh();
    }

    #endregion

    /// <summary>
    /// Updates the debug visualization for the dungeon generator during runtime.
    /// </summary>
    private void Update()
    {
        if (showFloor)
        {
            foreach (var room in rooms)
            {
                if (!room.Enabled) continue;
                AlgorithmsUtils.DebugRectInt(room.RectBounds, room.Color);
            }
        }

        if (showWalls)
        {
            foreach (var wall in walls)
            {
                AlgorithmsUtils.DebugBoundsInt(wall.Bounds, wall.Color);
            }
        }

        if (showDoors)
        {
            foreach (var door in doors)
            {
                if (!door.Enabled) continue;
                AlgorithmsUtils.DebugBoundsInt(door.Bounds, door.Color);
            }
        }
    }

    #region WeBuildTheWall

    /// <summary>
    /// Coroutine responsible for generating walls in the dungeon.
    /// Iterates through the list of rooms and processes wall creation based on specific conditions.
    /// Can include delays during execution for asynchronous operation.
    /// </summary>
    private IEnumerator BuildingWallsCoroutine()
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                if (BuildingWallsBody(i, j))
                {
                    yield return new WaitForSeconds(coroutineDelay);
                }
            }
        }
        BuildingWallsTail();
    }

    /// <summary>
    /// Responsible for initiating the construction of walls between rooms in the dungeon generation process.
    /// This method iterates through all pairs of rooms and calls the wall construction logic for each pair,
    /// ensuring the proper generation of walls to define the dungeon's structure.
    /// It also performs any final wall-building steps necessary after iterating through all room pairs.
    /// </summary>
    private void BuildingWalls()
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            for (int j = i + 1; j < rooms.Count; j++)
            {
                BuildingWallsBody(i, j);
            }
        }
        BuildingWallsTail();
    }
    
    /// <summary>
    /// unused
    /// </summary>
    private void MakeWallsMatrix()
    {
        matrix = new int[dungeonSize.x, dungeonSize.y];
        
        foreach (var room in rooms)
        {
            for (var x = 0; x < room.RectBounds.width - 1; x++)
            {
                for (var y = 0; y < room.RectBounds.height - 1; y++)
                {
                    matrix[x, y] += 1;
                }
            }
        }
    }

    /// <summary>
    /// Evaluates the process of building walls between two rooms in the dungeon
    /// generator. Checks for intersections between the rectangular bounds of the
    /// rooms and, if valid, constructs a wall object with properties such as bounds,
    /// color, and door direction, and associates it with the respective rooms.
    /// </summary>
    private bool BuildingWallsBody(int i, int j)
    {
        var firstRoom = rooms[i];
        var secondRoom = rooms[j];

        if (!AlgorithmsUtils.Intersects(firstRoom.RectBounds, secondRoom.RectBounds)) return false;
                
        var intersect = AlgorithmsUtils.Intersect(firstRoom.RectBounds, secondRoom.RectBounds);
        var tempBox = new BoundsInt(new Vector3Int(intersect.x, 0, intersect.y),
            new Vector3Int(intersect.width, wallHeight, intersect.height));
        var adaptiveColor = Color.red;
        var doorDirection = DoorDirection.None;
        if (tempBox.size.x > tempBox.size.z)
        {
            if (tempBox.size.x > doorWidth + wallWidth * 2)
            {
                adaptiveColor = Color.green;
                doorDirection = DoorDirection.Z;
            }
        }
        else if (tempBox.size.z > tempBox.size.x)
        {
            if (tempBox.size.z > doorWidth + wallWidth * 2)
            {
                adaptiveColor = Color.green;
                doorDirection = DoorDirection.X;
            }
        }
        var tempWall = new Wall(tempBox, adaptiveColor, doorDirection);
        firstRoom.Walls.Add(tempWall);
        secondRoom.Walls.Add(tempWall);
        tempWall.Rooms.Add(firstRoom);
        tempWall.Rooms.Add(secondRoom);
        walls.Add(tempWall);
        return true;
    }

    /// <summary>
    /// Completes the wall-building process by creating the edge walls of the dungeon.
    /// </summary>
    private void BuildingWallsTail()
    {
        var edgeWall1 = new Wall(new BoundsInt(new Vector3Int(startPoint.x, 0, startPoint.y), new Vector3Int(wallWidth, wallHeight, dungeonSize.y)), Color.red, DoorDirection.None);
        var edgeWall2 = new Wall(new BoundsInt(new Vector3Int(startPoint.x, 0, dungeonSize.y - wallWidth), new Vector3Int(dungeonSize.x, wallHeight, wallWidth)), Color.red, DoorDirection.None);
        var edgeWall3 = new Wall(new BoundsInt(new Vector3Int(dungeonSize.x - wallWidth, 0, startPoint.y), new Vector3Int(wallWidth, wallHeight, dungeonSize.y)), Color.red, DoorDirection.None);
        var edgeWall4 = new Wall(new BoundsInt(new Vector3Int(startPoint.x, 0, startPoint.y), new Vector3Int(dungeonSize.x, wallHeight, wallWidth)), Color.red, DoorDirection.None);
        walls.Add(edgeWall1);
        walls.Add(edgeWall2);
        walls.Add(edgeWall3);
        walls.Add(edgeWall4);
    }

    #endregion
    
    #region Doors

    /// <summary>
    /// Coroutine responsible for placing doors in the dungeon walls.
    /// Iterates through the list of walls in the dungeon and places doors at appropriate locations
    /// by invoking the PlacingDoorsBody method for each wall. Delays are introduced between each
    /// door placement based on the `coroutineDelay` value.
    /// </summary>
    private IEnumerator PlaceDoorsCoroutine()
    {
        foreach (var wall in walls)
        {
            PlacingDoorsBody(wall);
            yield return new WaitForSeconds(coroutineDelay);
        }
    }

    /// <summary>
    /// Iterates through the list of generated walls in the dungeon and places doors at appropriate locations.
    /// </summary>
    private void PlaceDoors()
    {
        foreach (var wall in walls)
        {
            PlacingDoorsBody(wall);
        }
    }

    /// <summary>
    /// Places the doors on a given wall based on its direction, and updates the door collection in the dungeon generator.
    /// </summary>
    private void PlacingDoorsBody(Wall wall)
    {
        if (wall.DoorDirection is DoorDirection.None) return;

        switch (wall.DoorDirection)
        {
            case DoorDirection.X:
                var zMax = wall.Bounds.zMax - wallWidth - doorWidth;
                var zMin = wall.Bounds.zMin + wallWidth;
                var tempBounds = new BoundsInt(new Vector3Int(wall.Bounds.xMin - doorOffset, 0, rnd.Next(zMin, zMax)), new Vector3Int(wallWidth + doorOffset * 2, wallHeight + doorOffset, doorWidth));
                var tempDoor = new Door(tempBounds, Color.yellow);
                wall.Door = tempDoor;
                doors.Add(tempDoor);
                break;
            case DoorDirection.Z:
                var xMax = wall.Bounds.xMax - doorWidth - wallWidth;
                var xMin = wall.Bounds.xMin + wallWidth;
                tempBounds = new BoundsInt(new Vector3Int(rnd.Next(xMin, xMax), 0, wall.Bounds.zMin - doorOffset), new Vector3Int(doorWidth, wallHeight + doorOffset, wallWidth + doorOffset * 2));
                tempDoor = new Door(tempBounds, Color.yellow);
                wall.Door = tempDoor;
                doors.Add(tempDoor);
                break;
        }
    }

    #endregion

    #region Graphing

    /// <summary>
    /// Coroutine responsible for establishing connections between walls by iterating through the list of walls.
    /// Displays edges and nodes during its execution for debugging purposes and applies a delay between processing each wall.
    /// </summary>
    private IEnumerator MakeConnectionsCoroutine()
    {
        showEdges = true;
        showNodes = true;
        
        foreach (var wall in walls)
        {
            MakeConnectionsBody(wall);
            yield return new WaitForSeconds(coroutineDelay);
        }
    }

    /// <summary>
    /// Handles the creation of connections between rooms in the dungeon by iterating through all walls and processing them accordingly.
    /// </summary>
    private void MakeConnections()
    {
        foreach (var wall in walls)
        {
            MakeConnectionsBody(wall);
        }
    }

    /// <summary>
    /// Creates graph connections between rooms based on the door present in the specified wall.
    /// </summary>
    private void MakeConnectionsBody(Wall wall)
    {
        if (wall.DoorDirection is DoorDirection.None) return;
            
        graph.AddNode(wall.Rooms[0]);
        graph.AddNode(wall.Rooms[1]);

        var connection1 = new Connection(wall.Rooms[1], wall.Door);
        var connection2 = new Connection(wall.Rooms[0], wall.Door);
        graph.AddEdge(wall.Rooms[0], connection1);
        graph.AddEdge(wall.Rooms[1], connection2);
    }

    #endregion

    #region CleaningUp

    /// <summary>
    /// Coroutine responsible for cutting down the number of enabled rooms in the dungeon based on a target percentage.
    /// The process iteratively disables rooms until the target amount is reached, while ensuring the remaining rooms stay connected.
    /// </summary>
    private IEnumerator CuttingRoomsCoroutine()
    {
        var targetAmount = rooms.Count - (int)(((float)rooms.Count / 100) * subtractedPercent);
        while (rooms.Count(a => a.Enabled) > targetAmount)
        {
            yield return new WaitForSeconds(coroutineDelay);
            var room = rooms.First(a => a.Enabled);
            room.Disable();
            if (BFS(graph.GetList().Keys.First(a => a.Enabled))) continue;
            room.Enable();
            
            yield break;
        }
    }

    /// <summary>
    /// Adjusts the number of active rooms in the dungeon by disabling extra rooms based on a specified
    /// percentage (`subtractedPercent`) of total rooms. Ensures the remaining rooms are connected.
    /// </summary>
    private void CuttingRooms()
    {
        var targetAmount = rooms.Count - (int)(((float)rooms.Count / 100) * subtractedPercent);
        while (rooms.Count(a => a.Enabled) > targetAmount)
        {
            var room = rooms.First(a => a.Enabled);
            room.Disable();
            if (BFS(graph.GetList().Keys.First(a => a.Enabled))) continue;
            room.Enable();
            
            return;
        }
    }

    ///<summary>
    /// Coroutine that iterates through all doors in the dungeon, temporarily disables them,
    /// and verifies their impact on graph connectivity using a breadth-first search (BFS) algorithm.
    /// If disabling a door breaks the graph's connectivity, the door is re-enabled.
    /// Intended to clean up unnecessary doors in the dungeon structure.
    /// </summary>
    private IEnumerator CleanUpDoorsCoroutine()
    {
        var queue = new Queue<Door>();
        foreach (var door in doors)
        {
            queue.Enqueue(door);
        }
        while (queue.Count > 0)
        {
            var door = queue.Dequeue();
            door.Enabled = false;
            if (!BFS(graph.GetList().Keys.First(a => a.Enabled))) door.Enabled = true;
            yield return new WaitForSeconds(coroutineDelay);
        }
    }

    /// <summary>
    /// Cleans up unnecessary or invalid doors in the dungeon generation process.
    /// </summary>
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
            door.Enabled = false;
            if (!BFS(graph.GetList().Keys.First(a => a.Enabled))) door.Enabled = true;
        }
    }

    #endregion

    #region Meshing

    /// <summary>
    /// Generates a two-dimensional matrix representing the dungeon layout by processing the dungeon's walls, their bounds,
    /// and door states. Each cell in the matrix represents a specific state of the dungeon grid, where a value indicates
    /// the presence or absence of walls or other features.
    /// </summary>
    private void DungeonMatrix()
    {
        matrix = new int[dungeonSize.x, dungeonSize.y];
        foreach (var wall in walls)
        {
            var doorEnabled = false;
            if (wall.DoorDirection is not DoorDirection.None)
            {
                if (wall.Door.Enabled)
                {
                    doorEnabled = true;
                }
            }
            
            for (int z = wall.Bounds.zMin; z < wall.Bounds.zMax; z++)
            {
                for (int x = wall.Bounds.xMin; x < wall.Bounds.xMax; x++)
                {
                    var pos = new Vector3Int(x, 0, z);
                    if (matrix[x,z] == 1) continue;
                    if (doorEnabled)
                    {
                        if (wall.Door.Bounds.Contains(pos)) continue;
                    }
                    matrix[x,z] = 1;
                }
            }
        }
    }

    /// <summary>
    /// Represents a wall pattern structure used in the dungeon generation process.
    /// </summary>
    private class WallPattern
    {
        /// <summary>
        /// Gets the name of the wall pattern.
        /// </summary>
        /// <remarks>
        /// This property holds the unique identifier name for a specific wall pattern. It is used to name the
        /// instantiated wall objects during the dungeon generation process.
        /// </remarks>
        public string Name { get; }

        /// <summary>
        /// Represents a two-dimensional pattern grid where specific wall configurations
        /// can be defined for dungeon generation. This property defines the matrix of
        /// integers used to match and identify specific wall arrangements within the
        /// dungeon grid.
        /// </summary>
        public int[,] Pattern { get; }

        /// <summary>
        /// Specifies the offset position for spawning the wall pattern in the dungeon generation process.
        /// This property represents a relative displacement in world space to adjust the starting spawn position
        /// of a wall prefab when instantiated, ensuring proper alignment and placement within the dungeon structure.
        /// </summary>
        public Vector3 SpawnOffset { get; }

        /// <summary>
        /// Represents the default rotation of the wall pattern during instantiation.
        /// This property specifies the orientation in which a wall prefab should be spawned,
        /// ensuring that it matches the desired alignment in the dungeon layout.
        /// </summary>
        public Quaternion DefaultRotation { get; }

        /// <summary>
        /// Specifies the type of wall prefab to be used for generating dungeon structures.
        /// </summary>
        public WallPrefabType PrefabType { get; }

        /// <summary>
        /// Represents a pattern for wall placement within the dungeon generation system.
        /// </summary>
        public WallPattern(string name, int[,] pattern, Vector3 spawnOffset, Quaternion defaultRotation, WallPrefabType prefabType)
        {
            Name = name;
            Pattern = pattern;
            SpawnOffset = spawnOffset;
            DefaultRotation = defaultRotation;
            PrefabType = prefabType;
        }
    }

    /// <summary>
    /// A collection of predefined wall patterns used for procedural dungeon generation.
    /// Each wall pattern defines its shape, position, rotation, and associated prefab type.
    /// </summary>
    private List<WallPattern> wallPatterns = new()
    {
        new WallPattern(
            "IntersectionPlus",
            new[,] { { 0, 1, 0 }, { 1, 1, 1 }, { 0, 1, 0 } },
            new Vector3(1.5f, 0, 1.5f),
            Quaternion.identity,
            WallPrefabType.IntersectionPlus
        ),
        new WallPattern(
            "IntersectionTDown",
            new[,] { { 0, 1 }, { 1, 1 }, { 0, 1 } },
            new Vector3(1.5f, 0, 1.5f),
            Quaternion.Euler(0, 180, 0),
            WallPrefabType.IntersectionT
        ),
        new WallPattern(
            "IntersectionTUp",
            new[,] { { 1, 0 }, { 1, 1 }, { 1, 0 } },
            new Vector3(1.5f, 0, 0.5f),
            Quaternion.identity,
            WallPrefabType.IntersectionT
        ),
        new WallPattern(
            "IntersectionTRight",
            new[,] { { 1, 1, 1 }, { 0, 1, 0 } },
            new Vector3(0.5f, 0, 1.5f),
            Quaternion.Euler(0, 90, 0),
            WallPrefabType.IntersectionT
        ),
        new WallPattern(
            "IntersectionTLeft",
            new[,] { { 0, 1, 0 }, { 1, 1, 1 } },
            new Vector3(1.5f, 0, 1.5f),
            Quaternion.Euler(0, -90, 0),
            WallPrefabType.IntersectionT
        ),
        new WallPattern(
            "LBCorner",
            new[,] { { 1, 1 }, { 1, 0 } },
            new Vector3(0.5f, 0, 0.5f),
            Quaternion.identity,
            WallPrefabType.Corner
        ),
        new WallPattern(
            "RBCorner",
            new[,] { { 1, 0 }, { 1, 1 } },
            new Vector3(1.5f, 0, 0.5f),
            Quaternion.Euler(0, -90, 0),
            WallPrefabType.Corner
        ),
        new WallPattern(
            "RTCorner",
            new[,] { { 0, 1 }, { 1, 1 } },
            new Vector3(1.5f, 0, 1.5f),
            Quaternion.Euler(0, 180, 0),
            WallPrefabType.Corner
        ),
        new WallPattern(
            "LTCorner",
            new[,] { { 1, 1 }, { 0, 1 } },
            new Vector3(0.5f, 0, 1.5f),
            Quaternion.Euler(0, 90, 0),
            WallPrefabType.Corner
        ),
        new WallPattern(
            "VWallLong",
            new[,] { { 1, 1, 1, 1 } },
            new Vector3(0.5f, 0, 2.0f),
            Quaternion.Euler(0, 90, 0),
            WallPrefabType.LongWall
        ),
        new WallPattern(
            "HWallLong",
            new[,] { { 1 }, { 1 }, { 1 }, { 1 } },
            new Vector3(2.0f, 0, 0.5f),
            Quaternion.identity,
            WallPrefabType.LongWall
        ),
        new WallPattern(
            "VWallShort",
            new[,] { { 1, 1 } },
            new Vector3(0.5f, 0, 0.0f),
            Quaternion.Euler(0, 90, 0),
            WallPrefabType.ShortWall
        ),
        new WallPattern(
            "HWallShort",
            new[,] { { 1 }, { 1 } },
            new Vector3(2.0f, 0, 0.5f),
            Quaternion.identity,
            WallPrefabType.ShortWall
        ),
        new WallPattern(
            "PillarUp",
            new[,] { { 1, 0 } },
            new Vector3(0.5f, 0, 0.5f),
            Quaternion.identity,
            WallPrefabType.Pillar
        ),
        new WallPattern(
            "PillarDown",
            new[,] { { 0, 1 } },
            new Vector3(0.5f, 0, 1.5f),
            Quaternion.Euler(0, 180, 0),
            WallPrefabType.Pillar
        ),
        new WallPattern(
            "PillarRight",
            new[,] { { 1 }, { 0 } },
            new Vector3(0.5f, 0, 0.5f),
            Quaternion.Euler(0, 90, 0),
            WallPrefabType.Pillar
        ),
        new WallPattern(
            "PillarLeft",
            new[,] { { 0 }, { 1 } },
            new Vector3(0.5f, 0, 0.5f),
            Quaternion.Euler(0, -90, 0),
            WallPrefabType.Pillar
        )
    };

    ///<summary>
    /// Spawns a wall at a specified grid position with the given pattern and attaches it to a parent transform.
    /// </summary>
    private void SpawnWall(WallPattern pattern, int x, int y, Transform parent)
    {
        var spawnPosition = new Vector3(x, 0, y) + pattern.SpawnOffset;
        var prefab = PickRandomPrefab(wallsPrefabs[pattern.PrefabType]);
        var tempObject = Instantiate(prefab, spawnPosition, pattern.DefaultRotation, parent);
        tempObject.name = pattern.Name;
    }


    /// <summary>
    /// Coroutine for placing wall meshes in the dungeon.
    /// Creates a parent object for walls, prepares the dungeon matrix for processing,
    /// scans the matrix for defined patterns, and spawns matching wall segments.
    /// </summary>
    private IEnumerator PlacingWallsMeshesCoroutine()
    {
        var wallsParentalObject = new GameObject("Walls");
        wallsParentalObject.transform.SetParent(meshes.transform);

        DungeonMatrix();
        

        
        // Process all patterns
        foreach (var wallPattern in wallPatterns)
        {
            yield return ScanMatrix((x, y) => SpawnWall(wallPattern, x, y, wallsParentalObject.transform), wallPattern.Pattern);
        }

        
        yield return null;
        yield break;

        // Local spawning methods

        // ScanMatrix coroutine for managing pattern scanning in the dungeon grid
        IEnumerator ScanMatrix<T>(Action<int, int> onMatchingPattern, T[,] pattern)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            int pRows = pattern.GetLength(0);
            int pCols = pattern.GetLength(1);

            for (int i = 0; i <= rows - pRows; i++)
            {
                for (int j = 0; j <= cols - pCols; j++)
                {
                    if (MatchesPattern(pattern, i, j))
                    {
                        onMatchingPattern(i, j);

                        // Clear cells in the matched pattern
                        for (int pi = 0; pi < pRows; pi++)
                        {
                            for (int pj = 0; pj < pCols; pj++)
                            {
                                matrix[i + pi, j + pj] = 0;
                            }
                        }

                        yield return new WaitForSeconds(coroutineDelay);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Coroutine responsible for placing floor meshes in the dungeon.
    /// This method creates a parent object for the floor meshes, determines the starting point,
    /// and uses a flood-fill algorithm to generate the floor layout.
    /// </summary>
    private IEnumerator PlacingFloorMeshesCoroutine()
    {
        // Create a parent object for floor
        var floorParentalObject = new GameObject("Floor");
        floorParentalObject.transform.SetParent(meshes.transform);

        DungeonMatrix(); // Generate the matrix

        // Determine starting point for flood-fill
        var fillStartPoint = graph.GetList().Keys.First(a => a.Enabled).Bounds.center;
        var startPoint = new Vector2Int((int)fillStartPoint.x, (int)fillStartPoint.z);

        yield return StartCoroutine(FloodFillCoroutine(startPoint, floorParentalObject));
    }

    /// <summary>
    /// Performs a flood fill algorithm starting from a given point, filling the area within the specified bounds and creating objects along the way.
    /// </summary>
    private IEnumerator FloodFillCoroutine(Vector2Int startPoint, GameObject parentObject)
    {
        // 8-Directional movement
        var directions = new List<Vector2Int>
        {
            new(0, 1), // Up
            new(0, -1), // Down
            new(-1, 0), // Left
            new(1, 0), // Right
            new(1, 1), // Up-Right
            new(1, -1), // Down-Right
            new(-1, 1), // Up-Left
            new(-1, -1) // Down-Left
        };

        var rows = matrix.GetLength(0);
        var cols = matrix.GetLength(1);

        // Stack for iterative flood fill
        var stack = new Stack<Vector2Int>();
        stack.Push(startPoint);

        while (stack.Count > 0)
        {
            var point = stack.Pop();

            if (point.x < 0 || point.x >= rows || point.y < 0 || point.y >= cols) continue; // Boundary check
            if (matrix[point.x, point.y] == 1) continue; // Already filled

            // Fill the current point
            Fill(point, parentObject);

            // Add neighboring points to the stack
            foreach (var direction in directions)
            {
                var newPoint = point + direction;
                stack.Push(newPoint);
            }
            
            yield return null;
        }
    }

    /// <summary>
    /// Fills a specific cell in the dungeon grid and instantiates a floor tile at that position.
    /// </summary>
    private void Fill(Vector2Int point, GameObject parentObject)
    {
        // Mark the cell as filled
        matrix[point.x, point.y] = 1;

        // Spawn floor prefab
        Instantiate(
            PickRandomPrefab(floorPrefab),
            new Vector3(point.x + 0.5f, 0, point.y + 0.5f),
            Quaternion.identity,
            parentObject.transform
        );
    }

    /// <summary>
    /// Handles the placement of wall meshes in the dungeon generation process.
    /// This method organizes walls into a parent object and processes the dungeon grid
    /// to identify patterns where wall meshes should be instantiated.
    /// </summary>
    private void PlacingMeshes()
    {
        var wallsParentalObject = new GameObject("Walls");
        wallsParentalObject.transform.SetParent(meshes.transform);

        DungeonMatrix();
        
        // Process all patterns
        foreach (var wallPattern in wallPatterns)
        {
            ScanMatrix((x, y) => SpawnWall(wallPattern, x, y, wallsParentalObject.transform), wallPattern.Pattern);
        }

        return;

        // ScanMatrix coroutine for managing pattern scanning in the dungeon grid
        void ScanMatrix<T>(Action<int, int> onMatchingPattern, T[,] pattern)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            int pRows = pattern.GetLength(0);
            int pCols = pattern.GetLength(1);

            for (int i = 0; i <= rows - pRows; i++)
            {
                for (int j = 0; j <= cols - pCols; j++)
                {
                    if (MatchesPattern(pattern, i, j))
                    {
                        onMatchingPattern(i, j);

                        // Clear cells in the matched pattern
                        for (int pi = 0; pi < pRows; pi++)
                        {
                            for (int pj = 0; pj < pCols; pj++)
                            {
                                matrix[i + pi, j + pj] = 0;
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Places floor meshes in the generated dungeon by calculating the appropriate floor areas
    /// and performing a flood-fill operation to fill those areas. The floor meshes are organized
    /// under a parent GameObject for better scene hierarchy management.
    /// </summary>
    private void PlacingFloorMeshes()
    {
        // Create a parent object for floor
        var floorParentalObject = new GameObject("Floor");
        floorParentalObject.transform.SetParent(meshes.transform);

        DungeonMatrix(); // Generate the matrix

        // Determine starting point for flood-fill
        var fillStartPoint = graph.GetList().Keys.First(a => a.Enabled).Bounds.center;
        var startPoint = new Vector2Int((int)fillStartPoint.x, (int)fillStartPoint.z);

        FloodFill(startPoint, floorParentalObject);
    }

    /// <summary>
    /// Recursively fills a specified starting point and its connected neighbors within the dungeon matrix.
    /// </summary>
    /// <param name="startPoint">
    /// The starting point for the flood-fill algorithm within the grid coordinates. It indicates where the filling begins.
    /// </param>
    /// <param name="parentObject">
    /// The parent GameObject to which the procedural floor meshes or elements created during the fill operation will be attached.
    /// </param>
    private void FloodFill(Vector2Int startPoint, GameObject parentObject)
    {
        // 4-Directional movement (no diagonals)
        var directions = new List<Vector2Int>
        {
            new(0, 1), // Up
            new(0, -1), // Down
            new(-1, 0), // Left
            new(1, 0), // Right
            new(1, 1), // Up-Right
            new(1, -1), // Down-Right
            new(-1, 1), // Up-Left
            new(-1, -1) // Down-Left
        };

        var rows = matrix.GetLength(0);
        var cols = matrix.GetLength(1);

        // Stack for iterative flood fill
        var stack = new Stack<Vector2Int>();
        stack.Push(startPoint);

        while (stack.Count > 0)
        {
            var point = stack.Pop();

            if (point.x < 0 || point.x >= rows || point.y < 0 || point.y >= cols) continue; // Boundary check
            if (matrix[point.x, point.y] == 1) continue; // Already filled

            // Fill the current point
            Fill(point, parentObject);

            // Add neighboring points to the stack
            foreach (var direction in directions)
            {
                var newPoint = point + direction;
                stack.Push(newPoint);
            }
        }
    }
    
    #endregion

    /// <summary>
    /// Selects and assigns a spawn location for the player in the dungeon.
    /// </summary>
    private void PickSpawnLocation()
    {
        player.position = rooms.Where(room => room.Enabled).ToList()[rnd.Next(safeSpawnPoints.Count)].Bounds.center;
        player.position += new Vector3(0, 1, 0);
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
                var color = wall.Color;
                color.a = 0.3f;
                Gizmos.color = color;
                Vector3 center = wall.Bounds.center;
                Gizmos.DrawCube(center, wall.Bounds.size);
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
                if (!room.Enabled) continue;
                var color = room.Color;
                color.a = 0.3f;
                Gizmos.color = color;
                Vector3 center = new Vector3(room.RectBounds.center.x, 0, room.RectBounds.center.y);
                var size = new Vector3(room.RectBounds.size.x, 0.01f, room.RectBounds.size.y);
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
                if (!door.Enabled) continue;
                var color = door.Color;
                color.a = 0.3f;
                Gizmos.color = color;
                Vector3 center = door.Bounds.center;
                Gizmos.DrawCube(center, door.Bounds.size);
            }
        }
        
        var connectionsList = graph.GetList();
        Gizmos.color = Color.white;
        foreach (var node in connectionsList.Keys)
        {
            if (showNodes)
            {
                if (!node.Enabled) continue;
                Gizmos.DrawSphere(node.Bounds.center, 1);
            }
            foreach (var connection in connectionsList[node])
            {
                if (showEdges)
                {
                    if (!connection.Node.Enabled || !connection.Via.Enabled) continue;
                    Gizmos.DrawLine(node.Bounds.center, connection.Via.Bounds.center);
                    Gizmos.DrawLine(connection.Via.Bounds.center, connection.Node.Bounds.center);
                }
            }
        }

        if (showLabels)
        {
            if (matrix == null) return;

            for (int x = 0; x < matrix.GetLength(0); x++)
            {
                for (int y = 0; y < matrix.GetLength(1); y++)
                {
                    Vector3 pos = new Vector3(x + 0.5f, 0, y + 0.5f);
                    Gizmos.color = matrix[x, y] == 0 ? Color.gray : Color.green;
                    Gizmos.DrawWireCube(pos, Vector3.one * 0.9f);

#if UNITY_EDITOR
                    Handles.Label(pos + Vector3.up * 0.5f, matrix[x, y].ToString());
#endif
                }
            }
        }
    }
}
