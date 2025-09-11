using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ChunksSys
{
    public class Chunk
    {
        public Chunk(Vector2Int chunkPosition)
        {
            chunkPos = chunkPosition;
            objects = new List<GameObject>();
            navMeshData = null;
        }
        public List<GameObject> objects;
        public Vector2Int chunkPos;
        public NavMeshData navMeshData;
    }
}
