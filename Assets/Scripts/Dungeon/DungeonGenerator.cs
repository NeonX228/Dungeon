using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Sirenix.OdinInspector;
using Unity.EditorCoroutines.Editor;
using UnityEngine.AI;
using static Dungeon.Classes;
using static Dungeon.Enums;
using static Dungeon.Tools;

namespace Dungeon
{
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
        /// </remarks>
        [PropertyOrder(-2)]
        [InfoBox(
            "<b><size=14><color=#FF0000>Warning:</color></size></b> <size=13>Using <b>Coroutine</b> mode in the Editor may lead to <b>unexpected behaviour</b>.",
            InfoMessageType.Warning, 
            "@ShowWarning()")]
        [EnumToggleButtons, HideLabel]
        public GenerationMode generationMode = GenerationMode.Instant;

        /// <summary>
        /// Represents the dimensions of the dungeon in grid units.
        /// </summary>
        /// <remarks>
        /// The x and y values correspond to the width and height of the dungeon, respectively.
        /// This value defines the overall area within which rooms, walls, and other structures will be generated.
        /// </remarks>
        [TabGroup("Settings", "General", SdfIconType.GearFill)]
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
        [ProgressBar(0f, 10f, Segmented = true)] 
        [PropertyOrder(-1)]
        [PropertySpace(SpaceAfter = 10)]
        [HideIf("IsInstant")]
        public float coroutineDelay;

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
        /// </remarks>
        [BoxGroup("Settings/Debug/Visibility")]
        [LabelText("Show Edges")]
        [GUIColor("@showEdges ? new Color(0.9f, 0.6f, 1f) : Color.gray")]
        public bool showEdges;
        
        [BoxGroup("Settings/Debug/Visibility")]
        [LabelText("Skip Floor Placement")]
        [GUIColor("@skipFloorPlacement ? new Color(0.7f, 0.8f, 0.3f) : Color.gray")]
        public bool skipFloorPlacement;

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

        private bool IsInstant() => generationMode == GenerationMode.Instant;
        
        private bool ShowWarning() => generationMode == GenerationMode.Coroutine && !Application.isPlaying;

        private float CoroutineDelay()
        {
            return Mathf.Lerp(0.001f, 1, coroutineDelay / 10);
        }

        /// <summary>
        /// Initializes and generates a new dungeon layout.
        /// This method sets up new parameters for the dungeon generation
        /// and initiates the room generation process using the configured settings.
        /// </summary>
        [HorizontalGroup("ActionButtons", Width = 0.5f)]
        [Button("🎯 Generate Dungeon", ButtonSizes.Large), GUIColor(0.3f, 0.7f, 1f)]
        [PropertyOrder(-2)]
        [PropertySpace(SpaceAfter = 10)]
        public void NewGeneration()
        {
            seed = RandomizeSeed();
            navMesh.RemoveData();
            if (!Application.isPlaying)
            {
                EditorCoroutineUtility.StartCoroutineOwnerless(GenerateRoomsCoroutine());
            }
            else
            {
                StartCoroutine(GenerateRoomsCoroutine());
            }
        }

        /// <summary>
        /// Regenerates the dungeon layout by reinitializing the dungeon generation process.
        /// This method resets the generation status and triggers the room generation logic.
        /// Only available when the generation mode is set to "Instant."
        /// </summary>
        [HorizontalGroup("ActionButtons", Width = 0.5f)]
        [Button("♻️ Regenerate Dungeon", ButtonSizes.Large), GUIColor(0.2f, 1f, 0.6f)]
        [PropertyOrder(-2)]
        [PropertySpace(SpaceAfter = 10)]
        public void Regeneration()
        {
            //navMesh.RemoveData();
            if (!Application.isPlaying)
            {
                EditorCoroutineUtility.StartCoroutineOwnerless(GenerateRoomsCoroutine());
            }
            else
            {
                StartCoroutine(GenerateRoomsCoroutine());
            }
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
            rooms.Add(new Room(new RectInt(0, 0, dungeonSize.x, dungeonSize.y), SplitMethod.Horizontaly, Color.green, this));
            walls.Clear();
            doors.Clear();
            graph.DropTable();
            safeSpawnPoints.Clear();
            while (meshes.childCount > 0) {
                if (!Application.isPlaying)
                {
                    DestroyImmediate(meshes.GetChild(0).gameObject);
                }
                else
                {
                    Destroy(meshes.GetChild(0).gameObject);
                }
                yield return null;
            }
            var failStreak = 0;
            for (var i = 0; i < divisions; i++)
            {
                if (endlessDivisions) divisions++;
            
                var tempRoom = rooms[0];
                rooms.RemoveAt(0); // Removing is necessary right here because it works like queue - every iteration it moves either the same room or new rooms to the end of the list.
                if (tempRoom.RectBounds.width > sizeConstrain * 2 && tempRoom.RectBounds.height > sizeConstrain * 2)
                {
                    // Do nothing - Split is possible
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
                    // Split is NOT possible, returning room to the end of the list
                    failStreak++;
                    rooms.Add(tempRoom);
                    if (failStreak >= rooms.Count * 2)
                    {
                        if (Application.isPlaying)
                        {
                            StartCoroutine(AfterGeneration());
                        }
                        else
                        {
                            EditorCoroutineUtility.StartCoroutineOwnerless(AfterGeneration());
                        }
                        yield break;
                    }
                    continue;
                }
                failStreak = 0;
                rooms.AddRange(SplitRoom(tempRoom, tempRoom.SplitMethod, wallWidth, rnd, sizeConstrain, this));
                
                if(!IsInstant()) yield return new WaitForSeconds(CoroutineDelay());
            }
        }

        /// <summary>
        /// Coroutine executed after the generation of dungeon rooms.
        /// </summary>
        private IEnumerator AfterGeneration()
        {
            if (Application.isPlaying)
            {
                yield return BuildingWalls();
                yield return PlaceDoors();
                yield return MakeConnections();
                rooms.Sort((r1, r2) => r1.GetSize().CompareTo(r2.GetSize()));
                yield return CuttingRooms();
                yield return CleanUpDoors();
                showDoors = false;
                showFloor = false;
                showWalls = false;
                showEdges = false;
                showNodes = false;
                showLabels = false;
                yield return PlacingWallsMeshes();
                yield return PlacingFloorMeshes();
            }
            else
            {
                var step = 0;
                var queueCompleted = false;
                while (!queueCompleted)
                {
                    switch (step)
                    {
                        case 0:
                            EditorCoroutineUtility.StartCoroutineOwnerless(BuildingWalls(() => { step++; }));
                            step++;
                            break;
                        case 2:
                            EditorCoroutineUtility.StartCoroutineOwnerless(PlaceDoors(() => { step++; }));
                            step++;
                            break;
                        case 4:
                            EditorCoroutineUtility.StartCoroutineOwnerless(MakeConnections(() => { step++; }));
                            step++;
                            break;
                        case 6:
                            rooms.Sort((r1, r2) => r1.GetSize().CompareTo(r2.GetSize()));
                            step++;
                            break;
                        case 7:
                            EditorCoroutineUtility.StartCoroutineOwnerless(CuttingRooms(() => { step++; }));
                            step++;
                            break;
                        case 9:
                            EditorCoroutineUtility.StartCoroutineOwnerless(CleanUpDoors(() => { step++; }));
                            step++;
                            break;
                        case 11:
                            EditorCoroutineUtility.StartCoroutineOwnerless(PlacingWallsMeshes(() => { step++; }));
                            step++;
                            break;
                        case 13:
                            EditorCoroutineUtility.StartCoroutineOwnerless(PlacingFloorMeshes(() => { queueCompleted = true; }));
                            showDoors = false;
                            showFloor = false;
                            showWalls = false;
                            showEdges = false;
                            showNodes = false;
                            showLabels = false;
                            step++;
                            break;
                        default:
                            yield return null;
                            break;
                    }
                }
            }
            PickSpawnLocation();
            navMesh.BuildNavMesh();
        }

        private void AddBoundsWalls()
        {
            var edgeWall1 = new Wall(new BoundsInt(new Vector3Int(0, 0, 0), new Vector3Int(wallWidth, wallHeight, dungeonSize.y)), Color.red, DoorDirection.None);
            var edgeWall2 = new Wall(new BoundsInt(new Vector3Int(0, 0, dungeonSize.y - wallWidth), new Vector3Int(dungeonSize.x, wallHeight, wallWidth)), Color.red, DoorDirection.None);
            var edgeWall3 = new Wall(new BoundsInt(new Vector3Int(dungeonSize.x - wallWidth, 0, 0), new Vector3Int(wallWidth, wallHeight, dungeonSize.y)), Color.red, DoorDirection.None);
            var edgeWall4 = new Wall(new BoundsInt(new Vector3Int(0, 0, 0), new Vector3Int(dungeonSize.x, wallHeight, wallWidth)), Color.red, DoorDirection.None);
            walls.Add(edgeWall1);
            walls.Add(edgeWall2);
            walls.Add(edgeWall3);
            walls.Add(edgeWall4);
        }

        #endregion

        /// <summary>
        /// Coroutine responsible for generating walls in the dungeon.
        /// Iterates through the list of rooms and processes wall creation based on specific conditions.
        /// Can include delays during execution for asynchronous operation.
        /// </summary>
        private IEnumerator BuildingWalls(Action onComplete = null)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    var firstRoom = rooms[i];
                    var secondRoom = rooms[j];

                    if (!AlgorithmsUtils.Intersects(firstRoom.RectBounds, secondRoom.RectBounds)) continue;
                
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
                    
                    if(!IsInstant()) yield return new WaitForSeconds(CoroutineDelay());
                }
            }
            AddBoundsWalls();
            onComplete?.Invoke();
        }

        /// <summary>
        /// Coroutine responsible for placing doors in the dungeon walls.
        /// Iterates through the list of walls in the dungeon and places doors at appropriate locations
        /// by invoking the PlacingDoorsBody method for each wall. Delays are introduced between each
        /// door placement based on the `CoroutineDelay()` value.
        /// </summary>
        private IEnumerator PlaceDoors(Action onComplete = null)
        {
            foreach (var wall in walls)
            {
                if (wall.DoorDirection is DoorDirection.None) continue;

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
                if(!IsInstant()) yield return new WaitForSeconds(CoroutineDelay());
            }
            onComplete?.Invoke();
        }

        /// <summary>
        /// Coroutine responsible for establishing connections between walls by iterating through the list of walls.
        /// Displays edges and nodes during its execution for debugging purposes and applies a delay between processing each wall.
        /// </summary>
        private IEnumerator MakeConnections(Action onComplete = null)
        {
            showEdges = true;
            showNodes = true;
        
            foreach (var wall in walls)
            {
                if (wall.DoorDirection is DoorDirection.None) continue;
            
                graph.AddNode(wall.Rooms[0]);
                graph.AddNode(wall.Rooms[1]);

                var connection1 = new Connection(wall.Rooms[1], wall.Door);
                var connection2 = new Connection(wall.Rooms[0], wall.Door);
                graph.AddEdge(wall.Rooms[0], connection1);
                graph.AddEdge(wall.Rooms[1], connection2);
                if(!IsInstant()) yield return new WaitForSeconds(CoroutineDelay());
            }
            onComplete?.Invoke();
        }

        #region CleaningUp

        /// <summary>
        /// Coroutine responsible for cutting down the number of enabled rooms in the dungeon based on a target percentage.
        /// The process iteratively disables rooms until the target amount is reached, while ensuring the remaining rooms stay connected.
        /// </summary>
        private IEnumerator CuttingRooms(Action onComplete = null)
        {
            var targetAmount = rooms.Count - (int)(((float)rooms.Count / 100) * subtractedPercent);
            while (rooms.Count(a => a.Enabled) > targetAmount)
            {
                if(!IsInstant()) yield return new WaitForSeconds(CoroutineDelay());
                var room = rooms.First(a => a.Enabled);
                room.Disable();
                if (BFS(graph.GetList().Keys.First(a => a.Enabled), graph, rooms)) continue;
                room.Enable();
            
                yield break;
            }
            onComplete?.Invoke();
        }

        ///<summary>
        /// Coroutine that iterates through all doors in the dungeon, temporarily disables them,
        /// and verifies their impact on graph connectivity using a breadth-first search (BFS) algorithm.
        /// If disabling a door breaks the graph's connectivity, the door is re-enabled.
        /// Intended to clean up unnecessary doors in the dungeon structure.
        /// </summary>
        private IEnumerator CleanUpDoors(Action onComplete = null)
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
                if (!BFS(graph.GetList().Keys.First(a => a.Enabled), graph, rooms)) door.Enabled = true;
                if(!IsInstant()) yield return new WaitForSeconds(CoroutineDelay());
            }
            onComplete?.Invoke();
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
            var prefab = PickRandomPrefab(wallsPrefabs[pattern.PrefabType], rnd);
            var tempObject = Instantiate(prefab, spawnPosition, pattern.DefaultRotation, parent);
            tempObject.name = pattern.Name;
        }


        /// <summary>
        /// Coroutine for placing wall meshes in the dungeon.
        /// Creates a parent object for walls, prepares the dungeon matrix for processing,
        /// scans the matrix for defined patterns, and spawns matching wall segments.
        /// </summary>
        private IEnumerator PlacingWallsMeshes(Action onComplete = null)
        {
            var wallsParentalObject = new GameObject("Walls");
            wallsParentalObject.transform.SetParent(meshes.transform);

            DungeonMatrix();
        
            // Process all patterns
            foreach (var wallPattern in wallPatterns)
            {
                yield return ScanMatrix((x, y) => SpawnWall(wallPattern, x, y, wallsParentalObject.transform), wallPattern.Pattern);
            }
            
            onComplete?.Invoke();
            if(!IsInstant()) yield return null;
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
                        if (MatchesPattern(pattern, i, j, matrix))
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

                            if(!IsInstant()) yield return new WaitForSeconds(CoroutineDelay());
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
        private IEnumerator PlacingFloorMeshes(Action onComplete = null)
        {
            // Create a parent object for floor
            var floorParentalObject = new GameObject("Floor");
            floorParentalObject.transform.SetParent(meshes.transform);

            DungeonMatrix(); // Generate the matrix

            // Determine starting point for flood-fill
            var fillStartPoint = graph.GetList().Keys.First(a => a.Enabled).Bounds.center;
            var startPoint = new Vector2Int((int)fillStartPoint.x, (int)fillStartPoint.z);

            yield return StartCoroutine(FloodFill(startPoint, floorParentalObject, () => {onComplete?.Invoke();}));
        }

        /// <summary>
        /// Performs a flood fill algorithm starting from a given point, filling the area within the specified bounds and creating objects along the way.
        /// </summary>
        private IEnumerator FloodFill(Vector2Int startPoint, GameObject parentObject, Action onComplete = null)
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
            
                if(!IsInstant() && !skipFloorPlacement) yield return null;
            }
            onComplete?.Invoke();
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
                PickRandomPrefab(floorPrefab, rnd),
                new Vector3(point.x + 0.5f, 0, point.y + 0.5f),
                Quaternion.identity,
                parentObject.transform
            );
        }
    
        #endregion

        /// <summary>
        /// Selects and assigns a spawn location for the player in the dungeon.
        /// </summary>
        private void PickSpawnLocation()
        {
            var navMeshAgent = player.GetComponent<NavMeshAgent>();
            navMeshAgent.enabled = false;
            player.position = rooms.Where(room => room.Enabled).ToList()[rnd.Next(safeSpawnPoints.Count)].Bounds.center;
            player.position += new Vector3(0, 1, 0);
            navMeshAgent.enabled = true;
        }

        private void OnDrawGizmos()
        {
            DebugVisuals.DrawVisuals(showWalls, showFloor, showLabels, showDoors, showNodes, showEdges, rooms, doors, walls, graph);
        }
    }
}
