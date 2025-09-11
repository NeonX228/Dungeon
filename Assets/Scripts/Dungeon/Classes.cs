using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using static Dungeon.Enums;
using static Navigation.Classes;

namespace Dungeon
{
    public static class Classes
    {
        /// <summary>
        /// Represents a connection between two nodes in a dungeon graph.
        /// </summary>
        public class Connection
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
        public class Node
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
        public class Room: Node
        {
            /// <summary>
            /// Instance of the <see cref="DungeonGenerator"/> class used to generate and manage dungeon structures.
            /// </summary>
            private DungeonGenerator generator;

            /// <summary>
            /// Represents a room within the dungeon, which is a subtype of a node.
            /// </summary>
            public Room(RectInt roomRect, Enums.SplitMethod lastSplitMethod, Color roomColor, DungeonGenerator generator)
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
            public Enums.SplitMethod SplitMethod;

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
                    if (wall.DoorDirection != Enums.DoorDirection.None)
                    {
                        wall.Door.Enabled = false;
                        wall.DoorDirection = Enums.DoorDirection.None;
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
                        wall.DoorDirection = Enums.DoorDirection.Z;
                    }
                    else if (wall.Bounds.size.z > generator.doorWidth + generator.wallWidth * 2)
                    {
                        wall.Color = Color.green;
                        wall.DoorDirection = Enums.DoorDirection.X;
                    }
                    else
                    {
                        wall.Color = Color.red;
                        wall.DoorDirection = Enums.DoorDirection.None;
                    }
                
                    if (wall.DoorDirection != Enums.DoorDirection.None) wall.Door.Enabled = true;
                }
            }
        }

        /// <summary>
        /// Represents a door within a dungeon generation system. Extends functionality from the Node class.
        /// </summary>
        public class Door: Node
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
        public class Wall: Node
        {
            /// <summary>
            /// Represents a wall in the dungeon, which is defined by its bounds, color, and door direction.
            /// </summary>
            public Wall(BoundsInt wallBounds, Color wallColor, Enums.DoorDirection targetDoorDirection)
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
            public Enums.DoorDirection DoorDirection;

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
        
        /// <summary>
        /// Represents a wall pattern structure used in the dungeon generation process.
        /// </summary>
        public class WallPattern
        {
            public string Name { get; }
            
            public int[,] Pattern { get; }
            
            public Vector3 SpawnOffset { get; }
            
            public Quaternion DefaultRotation { get; }
            
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
        
        // (DungeonDebugData class remains the same as before)
        public class DungeonDebugData
        {
            public List<Room> Rooms;
            public List<Door> Doors;
            public List<Wall> Walls;
            public Graph<Node, Connection> RoomGraph;
            public NavNode[,] NavigationMap;

            public DungeonDebugData(List<Room> rooms, List<Door> doors, List<Wall> walls, Graph<Node, Connection> graph,
                NavNode[,] navigationMap)
            {
                Rooms = rooms;
                Doors = doors;
                Walls = walls;
                RoomGraph = graph;
                NavigationMap = navigationMap;
            }
        }
    }
}
