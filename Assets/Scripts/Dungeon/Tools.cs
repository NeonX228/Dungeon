using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Dungeon.Enums;
using static Dungeon.Classes;
using Unity.EditorCoroutines.Editor;
using Random = UnityEngine.Random;

namespace Dungeon
{
    public static class Tools
    {
        /// <summary>
        /// Performs a Breadth-First Search (BFS) from the given starting node and validates the connectivity
        /// of the graph ensuring all enabled rooms are reachable.
        /// </summary>
        public static bool BFS(Node startNode, Graph<Node, Connection> graph, List<Room> rooms)
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

        public static HashSet<Node> DFS(Node startNode, Graph<Node, Connection> graph)
        {
            var visited = new HashSet<Node>();
            var keepDoors = new HashSet<Node>();

            Stack<Node> stack = new Stack<Node>();
            stack.Push(startNode);
            visited.Add(startNode);

            while (stack.Count > 0)
            {
                Node current = stack.Pop();

                foreach (var edge in graph.GetList()[current])
                {
                    if (!edge.Node.Enabled) continue;
                    if (visited.Add(edge.Node))
                    {
                        keepDoors.Add(edge.Via);
                        stack.Push(edge.Node);
                    }
                }
            }

            return keepDoors;
        }

/*
        public class DSU
        {
            private int[] parent; // parent[x] < 0 => x is root, value = -size

            public DSU(int n)
            {
                parent = new int[n];
                for (int i = 0; i < n; i++)
                    parent[i] = i; // each node is its own set of size 1
            }

            // Find with path compression
            public int Find(int x)
            {
                if (parent[x] < 0)
                    return x; // x is root
                return parent[x] = Find(parent[x]); // path compression
            }

            // Union by size
            public bool Union(int a, int b)
            {
                a = Find(a);
                b = Find(b);

                if (a == b)
                    return false; // already in the same set

                // Ensure a is the larger set
                if (parent[a] > parent[b])
                    (a, b) = (b, a);

                parent[a] += parent[b]; // update size
                parent[b] = a;          // make 'a' the parent

                return true;
            }

            // Optional: check if two nodes are in the same set
            public bool Connected(int a, int b)
            {
                return Find(a) == Find(b);
            }
        }
*/

        public static SplitMethod? AnalyzeSplit(Room r, SplitMethod defaultMethod, int sizeConstrain, float acceptableRatio)
        {
            bool canH = r.RectBounds.height >= sizeConstrain * 2;
            bool canV = r.RectBounds.width  >= sizeConstrain * 2;
            bool tall = r.RectBounds.width  * acceptableRatio < r.RectBounds.height;
            bool wide = r.RectBounds.height * acceptableRatio < r.RectBounds.width;

            if (canH && canV) return defaultMethod;
            if (canH && tall) return SplitMethod.Horizontaly;
            if (canV && wide) return SplitMethod.Verticaly;
            return null; // no valid split
        }

        ///<summary>
        /// Splits the given room into two new rooms based on the specified splitting method and offset.
        /// </summary>
        public static List<Room> SplitRoom(Room room, SplitMethod splitMethod, int offset, System.Random rnd, int sizeConstrain, DungeonGenerator generator)
        {
            var newRooms = new List<Room>();
            switch (splitMethod)
            {
                case SplitMethod.Verticaly:
                    var newRoom1 = new RectInt(room.RectBounds.x, room.RectBounds.y, rnd.Next(sizeConstrain, room.RectBounds.width - sizeConstrain) + offset,  room.RectBounds.height);
                    var newRoom2 = new RectInt(newRoom1.xMax - offset, room.RectBounds.y, room.RectBounds.width - newRoom1.width + offset, room.RectBounds.height);
                    newRooms.Add(new Room(newRoom1, SplitMethod.Horizontaly, Color.cyan, generator));
                    newRooms.Add(new Room(newRoom2, SplitMethod.Horizontaly, Color.cyan, generator));
                    break;
                case SplitMethod.Horizontaly:
                    newRoom1 = new RectInt(room.RectBounds.x, room.RectBounds.y, room.RectBounds.width, rnd.Next(sizeConstrain, room.RectBounds.height - sizeConstrain) + offset);
                    newRoom2 = new RectInt(room.RectBounds.x, newRoom1.yMax - offset, room.RectBounds.width, room.RectBounds.height - newRoom1.height + offset);
                    newRooms.Add(new Room(newRoom1, SplitMethod.Verticaly, Color.cyan, generator));
                    newRooms.Add(new Room(newRoom2, SplitMethod.Verticaly,Color.cyan, generator));
                    break;
            }
            return newRooms;
        }

        /// <summary>
        /// Randomizes the seed value used for procedural generation.
        /// </summary>
        public static int RandomizeSeed()
        {
            return Random.Range(0, int.MaxValue);
        }

        /// <summary>
        /// Selects and returns a random prefab from the given array of prefabs.
        /// </summary>
        public static GameObject PickRandomPrefab(GameObject[] prefabs, System.Random rnd)
        {
            return rnd.Next(prefabs.Length) == 0 ? prefabs[0] : prefabs[rnd.Next(prefabs.Length)];
        }

        /// <summary>
        /// Determines whether a specified submatrix within the dungeon matrix matches a given pattern.
        /// </summary>
        public static bool MatchesPattern<T>(T[,] pattern, int startRow, int startCol, int[,] matrix)
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
        public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                queue.Enqueue(item);
            }
        }
        public static bool InBounds(Vector2Int coords, int rows, int cols)
        {
            return coords.x >= 0 && coords.x < rows && coords.y >= 0 && coords.y < cols;
        }
        
        public static readonly List<WallPattern> WallPatterns = new()
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
        
        public enum AlgorithmState {
            Idle,
            Running,
            Completed,
            Error
        }

        public class AlgorithmStatus {
            public AlgorithmState State { get; private set; } = AlgorithmState.Idle;
            public float Progress { get; private set; } = 0f; // 0..1
            public string Message { get; private set; } = "";

            public event Action<AlgorithmStatus> OnChanged;

            public void Update(AlgorithmState state, float progress = 0f, string message = "") {
                State = state;
                Progress = progress;
                Message = message;
                OnChanged?.Invoke(this);
            }
        }
    }
}
