using System.Collections.Generic;
using UnityEngine;

namespace Navigation
{
    public static class Classes
    {
        public class NavNode
        {
            public Vector2Int position;
            public HashSet<NavNode> neighbours = new();

            public NavNode(Vector2Int position)
            {
                this.position = position;
            }
        }
    }
}
