using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ChunksSys
{
    public class ChunksManager
    {
        public Vector2Int chunkSize;
        private Vector2Int dungeonSize;
        private Chunk[,] chunks;

        public ChunksManager(Vector2Int dungeonSize, Vector2Int chunkSize)
        {
            this.dungeonSize = dungeonSize;
            this.chunkSize = chunkSize;
            int chunkCountX = Mathf.CeilToInt(dungeonSize.x / chunkSize.x);
            int chunkCountY = Mathf.CeilToInt(dungeonSize.y / chunkSize.y);

            chunks = new Chunk[chunkCountX, chunkCountY];
            
            for (int x = 0; x < chunkCountX; x++)
            {
                for (int y = 0; y < chunkCountY; y++)
                {
                    chunks[x, y] = new Chunk(new Vector2Int(x, y));
                }
            }
        }

        public void AddToChunk(GameObject targetObject)
        {
            if (chunks == null) return;

            Vector3 pos = targetObject.transform.position;

            int chunkX = Mathf.FloorToInt(pos.x / chunkSize.x);
            int chunkY = Mathf.FloorToInt(pos.z / chunkSize.y);

            if (chunkX < 0 || chunkY < 0 || chunkX >= chunks.GetLength(0) || chunkY >= chunks.GetLength(1))
            {
                Debug.LogWarning($"Object {targetObject.name} is outside dungeon bounds at {pos}");
                return;
            }

            chunks[chunkX, chunkY].objects.Add(targetObject);
        }

        public Chunk GetChunk(Vector2Int chunkCoords)
        {
            if (chunkCoords.x < 0 || chunkCoords.y < 0 || 
                chunkCoords.x >= chunks.GetLength(0) || chunkCoords.y >= chunks.GetLength(1))
            {
                Debug.LogError("Chunk coordinates out of bounds");
                return null;
            }

            return chunks[chunkCoords.x, chunkCoords.y];
        }

        public Chunk[,] GetAllChunks()
        {
            return chunks;
        }

        public HashSet<Vector2Int> GetAllChunkCoords()
        {
            HashSet<Vector2Int> coordinates = new HashSet<Vector2Int>();
            int rows = chunks.GetLength(0);
            int cols = chunks.GetLength(1);

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    coordinates.Add(new Vector2Int(x, y));
                }
            }

            return coordinates;
        }
        
        public Vector2Int WorldToChunk(Vector3 worldPos)
        {
            int chunkX = Mathf.FloorToInt(worldPos.x / chunkSize.x);
            int chunkY = Mathf.FloorToInt(worldPos.y / chunkSize.y);
            return new Vector2Int(chunkX, chunkY);
        }
    }
}
