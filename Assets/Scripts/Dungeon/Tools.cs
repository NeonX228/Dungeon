using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Dungeon.Enums;
using static Dungeon.Classes;
using Unity.EditorCoroutines.Editor;

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
    }
}
