using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ChunksSys;
using Navigation;
using PlayersSystems;
using Sirenix.OdinInspector;
using UnityEngine.AI;
using Utils;
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
    public class DungeonGenerator : SerializedMonoBehaviour
    {
        #region Singleton
        public static DungeonGenerator Instance {get; private set;}

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        #endregion
        
        #region PublicVariables

        /// <summary>
        /// Specifies the mode of dungeon generation.
        /// </summary>
        /// <remarks>
        /// Determines whether the dungeon generation process is performed instantly
        /// or over multiple frames using a coroutine. This can influence user experience
        /// and performance depending on the chosen mode.
        /// </remarks>
        [PropertyOrder(-2), EnumToggleButtons, HideLabel, SerializeField]
        private GenerationMode generationMode = GenerationMode.Instant;

        /// <summary>
        /// Represents the dimensions of the dungeon in grid units.
        /// </summary>
        /// <remarks>
        /// The x and y values correspond to the width and height of the dungeon, respectively.
        /// This value defines the overall area within which rooms, walls, and other structures will be generated.
        /// </remarks>
        [TabGroup("Settings", "General", SdfIconType.GearFill)]
        [BoxGroup("Settings/General/Area Bounds")]
        [LabelText("Size"), Tooltip("The size of the dungeon.")]
        public Vector2Int dungeonSize = new(500, 200);

        /// <summary>
        /// The transform parent object which holds all the generated dungeon meshes.
        /// This is used as a container for organizing and managing the mesh hierarchy,
        /// such as walls, floors, and other structural components of the dungeon.
        /// </summary>
        [BoxGroup("Settings/General/Misc")]
        [LabelText("Mesh Parental Object"), Required, SerializeField]
        private Transform meshes;

        /// <summary>
        /// Represents the time delay, in seconds, used between coroutine executions
        /// during the procedural generation process. This delay allows for
        /// staggered step-by-step visual creation of dungeon components such
        /// as rooms, walls, doors, and connections, providing better debugging
        /// and visualization value.
        /// </summary>
        [ProgressBar(0f, 10f, Segmented = true)] 
        [PropertyOrder(-1), PropertySpace(SpaceAfter = 10), HideIf("IsInstant")]
        public float coroutineDelay;
        
        /// <summary>
        /// Defines the minimum allowable size for a room in the dungeon generation process.
        /// Rooms smaller than this size will be discarded during generation.
        /// </summary>
        [TabGroup("Settings", "Division", SdfIconType.GearFill)]
        [BoxGroup("Settings/Division/Config")]
        [MinValue(1), LabelText("Minimum Room Size"), Tooltip("Rooms smaller than this size will be discarded.")]
        public int sizeConstrain = 30;

        /// <summary>
        /// The maximum allowed ratio between the width and height of a room.
        /// Rooms with dimensions exceeding this aspect ratio may be considered invalid
        /// when dividing and generating the dungeon layout.
        /// </summary>
        [BoxGroup("Settings/Division/Config")]
        [Range(1f, 5f), LabelText("Acceptable Aspect Ratio"), Tooltip("Max allowed ratio between width and height of a room.")]
        public float acceptableRatio = 1.5f;

        /// <summary>
        /// Represents the width of the generated wall in the dungeon, measured in grid units.
        /// </summary>
        /// <remarks>
        /// This variable is used in the dungeon generation process to determine the thickness of walls,
        /// influencing room boundaries and structural calculations.
        /// </remarks>
        /// <value>
        /// An integer value greater than or equal to 0, where larger values result in thicker walls.
        /// </value>
        [TabGroup("Settings", "Structure", SdfIconType.HouseFill)]
        [BoxGroup("Settings/Structure/Walls & Doors")]
        [MinValue(0), LabelText("Wall Width"), Tooltip("Width of the generated wall (in grid units).")]
        public int wallWidth = 1;

        /// <summary>
        /// Specifies the height of the generated walls in the dungeon, measured in Unity units.
        /// </summary>
        /// <remarks>
        /// This value determines the vertical size of walls created during the dungeon generation process.
        /// It is used to set the height of wall boundaries and ensure consistent scaling within the dungeon.
        /// </remarks>
        [BoxGroup("Settings/Structure/Walls & Doors")]
        [MinValue(0), LabelText("Wall Height"), Tooltip("Height of the generated wall (in Unity units).")]
        public int wallHeight = 1;

        /// <summary>
        /// Represents the width of doors that connect rooms in the dungeon.
        /// </summary>
        /// <remarks>
        /// This value determines the size of door openings within the generated dungeon.
        /// It is critical for calculating wall configurations and ensuring proper room connectivity.
        /// </remarks>
        [BoxGroup("Settings/Structure/Walls & Doors")]
        [MinValue(1), LabelText("Door Width"), Tooltip("Width of doors that connect rooms.")]
        public int doorWidth = 1;

        /// <summary>
        /// Specifies the offset value for the placement of doors relative to the walls.
        /// A non-negative integer value that determines how far doors are positioned from
        /// their default placement, affecting their alignment within the dungeon structure.
        /// </summary>
        [BoxGroup("Settings/Structure/Walls & Doors")]
        [MinValue(0), LabelText("Door Offset")]
        public int doorOffset;
        
        [BoxGroup("Settings/Structure/Rooms")]
        [LabelText("Subtract Rooms")]
        public bool subtractRooms = true;

        /// <summary>
        /// The percentage of rooms in the dungeon that will be removed during the generation process.
        /// This value is used to calculate the number of rooms to disable while maintaining the overall structure.
        /// </summary>
        [BoxGroup("Settings/Structure/Rooms")]
        [MinValue(1), MaxValue(100), LabelText("Subtracted Percent"), Range(1, 100), ShowIf("subtractRooms")]
        public int subtractedPercent = 10;
        
        [BoxGroup("Settings/Structure/Doors")]
        [LabelText("Remove Cycles")]
        public bool subtractDoors = true;

        /// <summary>
        /// The percentage of rooms in the dungeon that will be removed during the generation process.
        /// This value is used to calculate the number of rooms to disable while maintaining the overall structure.
        /// </summary>
        [BoxGroup("Settings/Structure/Doors")]
        [MinValue(0), MaxValue(99), LabelText("Left Cycle Chance"), Range(0, 99), ShowIf("subtractDoors")]
        public int leftCycleProb = 10;

        /// <summary>
        /// The seed value used to initialize the procedural generation of the dungeon.
        /// A fixed seed ensures consistent and repeatable results for the generated layout,
        /// while varying the seed produces different dungeon structures.
        /// </summary>
        [TabGroup("Settings", "Randomization", SdfIconType.Dice6Fill)]
        [BoxGroup("Settings/Randomization/Seed")]
        [LabelText("Seed Value"), Tooltip("Seed used to initialize the procedural generation.")]
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
        [LabelText("Floor Tile Prefab"), Required, SerializeField]
        private GameObject[] floorPrefab;

        /// <summary>
        /// A dictionary that maps different types of wall prefabs to collections of GameObjects.
        /// Each WallPrefabType key corresponds to a specific type of wall (e.g., intersection, corner)
        /// and its associated array of GameObject prefabs.
        /// </summary>
        [BoxGroup("Settings/Modeling/Prefabs")]
        [LabelText("Walls Tile Prefabs"), Required]
        public Dictionary<WallPrefabType, GameObject[]> wallsPrefabs = new();
        
        [BoxGroup("Settings/Modeling/Chunking")]
        [LabelText("Chunks Loader"), Required, SerializeField]
        private ChunksLoader chunksLoader;

        /// <summary>
        /// A boolean flag indicating whether to display labels on certain elements
        /// during visualization in the editor. When enabled, labels such as object names,
        /// dimensions, or other debug-related information may be shown based on the
        /// current rendering context (e.g., walls, floors, doors, or other dungeon structures).
        /// </summary>
        [TabGroup("Settings", "Debug", SdfIconType.BugFill)]
        [BoxGroup("Settings/Debug/Visibility")]
        [LabelText("Show"), EnumToggleButtons]
        public VisualFlags visualFlags;

        [BoxGroup("Settings/Debug/Skipping")]
        [LabelText("Skip Floor Placement"), GUIColor("@skipFloorPlacement ? new Color(0.85f, 0.9f, 0.6f) : Color.gray")]
        public bool skipFloorPlacement;

        [BoxGroup("Settings/Debug/Skipping")]
        [LabelText("Skip NavMap Baking"), GUIColor("@skipNavMapBaking ? new Color(0.6f, 0.8f, 0.7f) : Color.gray")]
        public bool skipNavMapBaking;
        
        public AlgorithmStatus Status { get; private set; } = new AlgorithmStatus();

        #endregion

        #region PrivateVariables

        /// <summary>
        /// Represents a collection of rooms generated during the dungeon creation process.
        /// This list is used to store and manage all rooms that are created as part of the dungeon
        /// generation, including divisions and subdivisions of the initial dungeon space.
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
        /// This is used in various dungeon generation processes such as splitting rooms randomly
        /// and picking random prefabs during procedural generation of the dungeon.
        /// </summary>
        private System.Random rnd;

        /// <summary>
        /// Represents a graph structure used within the dungeon generation process.
        /// </summary>
        private Graph<Node, Connection> graph = new();

        /// <summary>
        /// Represents a two-dimensional matrix used for internal computation and representation of dungeon structures.
        /// </summary>
        private int[,] matrix;
        
        private DungeonDebugData debugData = new(null, null, null, null, null);

        private NavigationManager navManager;

        #endregion

        private void Start()
        {
            navManager = NavigationManager.Instance;
        }

        #region Initializators

        private bool IsInstant() => generationMode == GenerationMode.Instant;

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
        [PropertyOrder(-2), PropertySpace(SpaceAfter = 10), ShowIf("@UnityEngine.Application.isPlaying")]
        public void NewGeneration()
        {
            seed = RandomizeSeed();
            StartCoroutine(GenerateRoomsCoroutine());
        }

        /// <summary>
        /// Regenerates the dungeon layout by reinitializing the dungeon generation process.
        /// This method resets the generation status and triggers the room generation logic.
        /// Only available when the generation mode is set to "Instant."
        /// </summary>
        [HorizontalGroup("ActionButtons", Width = 0.5f)]
        [Button("♻️ Regenerate Dungeon", ButtonSizes.Large), GUIColor(0.2f, 1f, 0.6f)]
        [PropertyOrder(-2), PropertySpace(SpaceAfter = 10), ShowIf("@UnityEngine.Application.isPlaying")]
        public void Regeneration()
        {
            StartCoroutine(GenerateRoomsCoroutine());
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
            rooms.Clear();
            walls.Clear();
            doors.Clear();
            graph.DropTable();
            while (meshes.childCount > 0) {
                Destroy(meshes.GetChild(0).gameObject);
                yield return null;
            }
            rnd = new System.Random(seed);
            var genQueue = new PriorityQueue<Room, int>(
                Comparer<int>.Create((a, b) => b.CompareTo(a))); 
            genQueue.Enqueue(
                new Room(
                    new RectInt(
                        0, 
                        0, 
                        dungeonSize.x, 
                        dungeonSize.y), 
                    SplitMethod.Horizontaly, 
                    Color.green, 
                    this),
                dungeonSize.x * dungeonSize.y
                );
            
            while (genQueue.Count > 0)
            {
                var tempRoom = genQueue.Dequeue();
                
                var split = AnalyzeSplit(tempRoom, tempRoom.SplitMethod, sizeConstrain, acceptableRatio);
                if (split is null)
                {
                    rooms.Add(tempRoom);
                    continue;
                }

                tempRoom.SplitMethod = split.Value;
                var newRooms = SplitRoom(tempRoom, tempRoom.SplitMethod, wallWidth, rnd, sizeConstrain, this);
                foreach (var room in newRooms)
                {
                    genQueue.Enqueue(room, room.RectBounds.width * room.RectBounds.height);
                }
                
                if(!IsInstant()) yield return new WaitForSeconds(CoroutineDelay());
            }
            
            StartCoroutine(AfterGeneration());
        }

        /// <summary>
        /// Coroutine executed after the generation of dungeon rooms.
        /// </summary>
        private IEnumerator AfterGeneration()
        {
            yield return BuildingWalls();
            yield return PlaceDoors();
            yield return MakeConnections();
            rooms.Sort((r1, r2) => r1.GetSize().CompareTo(r2.GetSize()));
            if (subtractRooms) yield return CuttingRooms();
            if (subtractDoors) yield return CleanUpDoors();
            visualFlags = VisualFlags.None;
            yield return PlacingWallsMeshes();
            yield return PlacingFloorMeshes();
            yield return MakeNavMap();
            
            //PickSpawnLocation();
            EventManager.TriggerGenerationComplete();
        }
        #endregion

        /// <summary>
        /// Coroutine responsible for generating walls in the dungeon.
        /// Iterates through the list of rooms and processes wall creation based on specific conditions.
        /// It Can include delays during execution for asynchronous operation.
        /// </summary>
        private IEnumerator BuildingWalls()
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = i + 1; j < rooms.Count; j++)
                {
                    var firstRoom = rooms[i];
                    var secondRoom = rooms[j];

                    if (!AlgorithmsUtils.Intersects(firstRoom.RectBounds, secondRoom.RectBounds, out var intersection)) continue;
                    
                    var tempBox = new BoundsInt(new Vector3Int(intersection.x, 0, intersection.y),
                        new Vector3Int(intersection.width, wallHeight, intersection.height));
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

        /// <summary>
        /// Coroutine responsible for placing doors in the dungeon walls.
        /// Iterates through the list of walls in the dungeon and places doors at appropriate locations
        /// by invoking the PlacingDoorsBody method for each wall. Delays are introduced between each
        /// door placement based on the `CoroutineDelay()` value.
        /// </summary>
        private IEnumerator PlaceDoors()
        {
            foreach (var wall in walls)
            {
                if (wall.DoorDirection is DoorDirection.None) continue;

                var tempBounds = new BoundsInt();
                switch (wall.DoorDirection)
                {
                    case DoorDirection.X:
                        var zMax = wall.Bounds.zMax - wallWidth - doorWidth;
                        var zMin = wall.Bounds.zMin + wallWidth;
                        tempBounds = new BoundsInt(new Vector3Int(wall.Bounds.xMin - doorOffset, 0, rnd.Next(zMin, zMax)), new Vector3Int(wallWidth + doorOffset * 2, wallHeight + doorOffset, doorWidth));
                        break;
                    case DoorDirection.Z:
                        var xMax = wall.Bounds.xMax - doorWidth - wallWidth;
                        var xMin = wall.Bounds.xMin + wallWidth;
                        tempBounds = new BoundsInt(new Vector3Int(rnd.Next(xMin, xMax), 0, wall.Bounds.zMin - doorOffset), new Vector3Int(doorWidth, wallHeight + doorOffset, wallWidth + doorOffset * 2));
                        break;
                }
                
                var tempDoor = new Door(tempBounds, Color.yellow);
                wall.Door = tempDoor;
                doors.Add(tempDoor);
                
                if(!IsInstant()) yield return new WaitForSeconds(CoroutineDelay());
            }
        }

        /// <summary>
        /// Coroutine responsible for establishing connections between walls by iterating through the list of walls.
        /// Displays edges and nodes during its execution for debugging purposes and applies a delay between processing each wall.
        /// </summary>
        private IEnumerator MakeConnections()
        {
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
        }

        #region CleaningUp

        /// <summary>
        /// Coroutine responsible for cutting down the number of enabled rooms in the dungeon based on a target percentage.
        /// The process iteratively disables rooms until the target amount is reached, while ensuring the remaining rooms stay connected.
        /// </summary>
        private IEnumerator CuttingRooms()
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
        }

        ///<summary>
        /// Coroutine that iterates through all doors in the dungeon, temporarily disables them,
        /// and verifies their impact on graph connectivity using a breadth-first search (BFS) algorithm.
        /// If disabling a door, breaks the graph's connectivity, the door is re-enabled.
        /// Intended to clean up unnecessary doors in the dungeon structure.
        /// </summary>
        private IEnumerator CleanUpDoors()
        {
            var tree = DFS(graph.GetList().Keys.First(a => a.Enabled), graph);

            foreach (var door in doors)
            {
                if(tree.Contains(door)) continue;
                if (leftCycleProb <= rnd.Next(0, 100)) door.Enabled = false;
                if(!IsInstant()) yield return new WaitForSeconds(CoroutineDelay());
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
        

        ///<summary>
        /// Spawns a wall at a specified grid position with the given pattern and attaches it to a parent transform.
        /// </summary>
        private void SpawnWall(WallPattern pattern, int x, int y, Transform parent)
        {
            var spawnPosition = new Vector3(x, 0, y) + pattern.SpawnOffset;
            var prefab = PickRandomPrefab(wallsPrefabs[pattern.PrefabType], rnd);
            var tempObject = Instantiate(prefab, spawnPosition, pattern.DefaultRotation, parent);
            tempObject.SetActive(false);
            tempObject.name = pattern.Name;
            chunksLoader.AddToChunk(tempObject);
        }

        /// <summary>
        /// Coroutine for placing wall meshes in the dungeon.
        /// Creates a parent object for walls, prepares the dungeon matrix for processing,
        /// scans the matrix for defined patterns, and spawns matching wall segments.
        /// </summary>
        private IEnumerator PlacingWallsMeshes()
        {
            var wallsParentalObject = new GameObject("Walls");
            wallsParentalObject.transform.SetParent(meshes.transform);

            DungeonMatrix();
        
            // Process all patterns
            foreach (var wallPattern in WallPatterns)
            {
                yield return ScanMatrix((x, y) => SpawnWall(wallPattern, x, y, wallsParentalObject.transform), wallPattern.Pattern);
            }
            
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
        private IEnumerator PlacingFloorMeshes()
        {
            // Create a parent object for the floor
            var floorParentalObject = new GameObject("Floor");
            floorParentalObject.transform.SetParent(meshes.transform);

            DungeonMatrix(); // Generate the matrix

            // Determine a starting point for flood-fill
            var fillStartPoint = graph.GetList().Keys.First(a => a.Enabled).Bounds.center;
            var startPoint = new Vector2Int((int)fillStartPoint.x, (int)fillStartPoint.z);

            yield return StartCoroutine(FloodFill(startPoint, floorParentalObject));
        }

        /// <summary>
        /// Performs a flood fill algorithm starting from a given point, filling the area within the specified bounds and creating objects along the way.
        /// </summary>
        private IEnumerator FloodFill(Vector2Int startPoint, GameObject parentObject)
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
        }

        /// <summary>
        /// Fills a specific cell in the dungeon grid and instantiates a floor tile at that position.
        /// </summary>
        private void Fill(Vector2Int point, GameObject parentObject)
        {
            // Mark the cell as filled
            matrix[point.x, point.y] = 1;

            // Spawn floor prefab
            var tempObject = Instantiate(
                PickRandomPrefab(floorPrefab, rnd),
                new Vector3(point.x + 0.5f, 0, point.y + 0.5f),
                Quaternion.identity,
                parentObject.transform
            );
            tempObject.SetActive(false);
            chunksLoader.AddToChunk(tempObject);
        }
    
        #endregion

        private IEnumerator MakeNavMap()
        {
            DungeonMatrix();

            var directions = new List<Vector2Int>
            {
                new(0, 1),   // Up
                new(0, -1),  // Down
                new(-1, 0),  // Left
                new(1, 0),   // Right
                new(1, 1),   // Up-Right
                new(1, -1),  // Down-Right
                new(-1, 1),  // Up-Left
                new(-1, -1)  // Down-Left
            };

            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            // Pass 1: Create nodes for all walkable tiles
            for (int x = 0; x < rows; x++)
            {
                for (int y = 0; y < cols; y++)
                {
                    if (matrix[x, y] == 0) // Walkable
                    {
                        if (navManager.map[x, y] == null)
                        {
                            navManager.map[x, y] = new Navigation.Classes.NavNode(new Vector2Int(x, y));
                            if (!IsInstant()) yield return new WaitForSeconds(CoroutineDelay());
                        }
                    }
                }
            }

            // Pass 2: Connect neighbors
            for (int x = 0; x < rows; x++)
            {
                for (int y = 0; y < cols; y++)
                {
                    var node = navManager.map[x, y];
                    if (node == null) continue; // Skip non-walkable cells

                    foreach (var dir in directions)
                    {
                        var neighbourCoords = new Vector2Int(x + dir.x, y + dir.y);

                        if (InBounds(neighbourCoords, rows, cols) && matrix[neighbourCoords.x, neighbourCoords.y] == 0)
                        {
                            var neighbourNode = navManager.map[neighbourCoords.x, neighbourCoords.y];
                            if (neighbourNode == null)
                            {
                                neighbourNode = new Navigation.Classes.NavNode(neighbourCoords);
                                navManager.map[neighbourCoords.x, neighbourCoords.y] = neighbourNode;
                            }

                            // Add a link both ways (undirected graph)
                            node.neighbours.Add(neighbourNode);
                            neighbourNode.neighbours.Add(node);
                            if (!IsInstant()) yield return new WaitForSeconds(CoroutineDelay());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Selects and assigns a spawn location for the player in the dungeon.
        /// </summary>
        private void PickSpawnLocation()
        {
            var navMeshAgent = Player.Instance.GetComponent<NavMeshAgent>();
            navMeshAgent.enabled = false;
            var enabledRooms = rooms.Where(room => room.Enabled).ToList();
            Player.Instance.transform.position = enabledRooms[rnd.Next(enabledRooms.Count)].Bounds.center;
            Player.Instance.transform.position += new Vector3(0, 1, 0);
            navMeshAgent.enabled = true;
        }

        private void OnDrawGizmos()
        {
            if (Status.State == AlgorithmState.Running)
            {
                debugData.Rooms = rooms;
                debugData.Walls = walls;
                debugData.Doors = doors;
                debugData.RoomGraph = graph;
                debugData.NavigationMap = navManager.map;
            }
            DebugVisuals.DrawVisuals(debugData, visualFlags);
        }
    }
}
