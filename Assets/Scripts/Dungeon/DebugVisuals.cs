using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Dungeon.Classes;

namespace Dungeon
{
    public abstract class DebugVisuals
    {
        public static void DrawVisuals(bool showWalls, bool showFloor, bool showLabels, bool showDoors, bool showNodes, bool showEdges, List<Room> rooms, List<Door> doors, List<Wall> walls, Graph<Node, Connection> graph)
        {
            var style = new GUIStyle
            {
                normal =
                {
                    textColor = Color.white
                },
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            if (showWalls)
            {
                for (var i = 0; i < walls.Count; i++)
                {
                    var wall = walls[i];
                    var color = wall.Color;
                    color.a = 0.3f;
                    Gizmos.color = color;
                    var center = wall.Bounds.center;
                    Gizmos.DrawCube(center, wall.Bounds.size);
                    if (showLabels)
                    {
                        Handles.Label(center, $"*{i}", style);
                    }
                }
            }
    
            style.normal.textColor = Color.gray;
            if (showFloor)
            {
                for (var i = 0; i < rooms.Count; i++)
                {
                    var room = rooms[i];
                    if (!room.Enabled) continue;
                    var color = room.Color;
                    color.a = 0.3f;
                    Gizmos.color = color;
                    var center = new Vector3(room.RectBounds.center.x, 0, room.RectBounds.center.y);
                    var size = new Vector3(room.RectBounds.size.x, 0.01f, room.RectBounds.size.y);
                    Gizmos.DrawCube(center, size);
                    if (showLabels)
                    {
                        Handles.Label(center, $"#{i}", style);
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
                    var center = door.Bounds.center;
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

                if (!showEdges) continue;
                
                foreach (var connection in connectionsList[node].Where(connection => connection.Node.Enabled && connection.Via.Enabled))
                {
                    Gizmos.DrawLine(node.Bounds.center, connection.Via.Bounds.center);
                    Gizmos.DrawLine(connection.Via.Bounds.center, connection.Node.Bounds.center);
                }
            }
        }
    }
}
