using UnityEditor;
using UnityEngine;
using static Dungeon.Classes;
using static Dungeon.Enums;

namespace Dungeon
{
    public static class DebugVisuals
    {
        private static GUIStyle _labelStyle;

        private static GUIStyle LabelStyle => _labelStyle ??= new GUIStyle
        {
            normal = { textColor = Color.white },
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        // The new, cleaner method signature
        public static void DrawVisuals(DungeonDebugData data, VisualFlags options)
        {
            if (data == null) return;

            // Use the properties from the options struct
            if ((options & VisualFlags.Walls) != 0 && data.Walls != null)
            {
                for (var i = 0; i < data.Walls.Count; i++)
                {
                    DrawGizmoCube(data.Walls[i].Bounds, data.Walls[i].Color, $"*{i}", (options & VisualFlags.Labels) != 0, Color.white);
                }
            }

            if ((options & VisualFlags.Floor) != 0 && data.Rooms != null)
            {
                // ... same logic, just using options.ShowFloor and options.ShowLabels
            }

            if ((options & VisualFlags.Doors) != 0 && data.Doors != null)
            {
                // ... same logic, using options.ShowDoors
            }

            // Pass the relevant booleans to the helper methods
            if ((options & (VisualFlags.RoomNodes | VisualFlags.RoomEdges)) != 0 && data.RoomGraph != null)
            {
                DrawRoomGraph(data.RoomGraph, (options & VisualFlags.RoomNodes) != 0, (options & VisualFlags.RoomEdges) != 0);
            }

            if ((options & (VisualFlags.NavNodes | VisualFlags.NavEdges)) != 0 && data.NavigationMap != null)
            {
                DrawNavGraph(data.NavigationMap, (options & VisualFlags.NavNodes) != 0, (options & VisualFlags.NavEdges) != 0);
            }
        }

        // (Helper methods like DrawGizmoCube, DrawRoomGraph, etc. remain the same)
        private static void DrawGizmoCube(BoundsInt bounds, Color color, string label = null, bool showLabel = false,
            Color labelColor = default)
        {
            color.a = 0.3f;
            Gizmos.color = color;
            Gizmos.DrawCube(bounds.center, bounds.size);

            if (showLabel && !string.IsNullOrEmpty(label))
            {
                LabelStyle.normal.textColor = labelColor;
                Handles.Label(bounds.center, label, LabelStyle);
            }
        }

        private static void DrawRoomGraph(Graph<Node, Connection> graph, bool showNodes, bool showEdges)
        {
            var connectionsList = graph.GetList();

            foreach (var node in connectionsList.Keys)
            {
                if (!node.Enabled) continue;

                if (showNodes)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawSphere(node.Bounds.center, 1);
                }

                if (!showEdges) continue;

                // Using a simple foreach and if-check is generally more performant than LINQ in hot paths like this.
                foreach (var connection in connectionsList[node])
                {
                    if (connection.Node.Enabled && connection.Via.Enabled)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(node.Bounds.center, connection.Via.Bounds.center);
                        Gizmos.DrawLine(connection.Via.Bounds.center, connection.Node.Bounds.center);
                    }
                }
            }
        }

        private static void DrawNavGraph(Navigation.Classes.NavNode[,] map, bool showNodes, bool showEdges)
        {
            int width = map.GetLength(0);
            int height = map.GetLength(1);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var node = map[x, y];
                    if (node == null) continue;

                    var nodePos = new Vector3(x, 0, y);

                    if (showNodes)
                    {
                        Gizmos.color = Color.violet;
                        Gizmos.DrawWireSphere(nodePos, 0.2f);
                    }

                    if (showEdges)
                    {
                        Gizmos.color = Color.magenta;
                        foreach (var neighbour in node.neighbours)
                        {
                            var neighbourPos = new Vector3(neighbour.position.x, 0, neighbour.position.y);
                            Gizmos.DrawLine(nodePos, neighbourPos);
                        }
                    }
                }
            }
        }
    }
}