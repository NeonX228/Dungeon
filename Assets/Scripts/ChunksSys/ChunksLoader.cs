using System;
using System.Collections.Generic;
using Dungeon;
using PlayersSystems;
using UnityEngine;

namespace ChunksSys
{
    public class ChunksLoader : MonoBehaviour
    {
        [SerializeField] private Vector2Int renderingDistance;
        [SerializeField] private Vector2Int chunkSize;
        
        private ChunksManager manager;
        private HashSet<Vector2Int> currentChunks = new();
        private HashSet<Vector2Int> loadedChunks = new();

        private void Awake()
        {
            EventManager.OnGenerationComplete += UpdateChunks;
        }

        private void Start()
        {
            manager = new ChunksManager(DungeonGenerator.Instance.dungeonSize, chunkSize);
        }

        private void Update()
        {
            if (Player.Instance.IsMoving)
            {
                UpdateChunks();
            }
        }

        public void AddToChunk(GameObject targetObject)
        {
            manager.AddToChunk(targetObject);
        }

        private void ProcessChunks()
        {
            
        }

        private void UpdateChunks()
        {
            currentChunks.Clear();
            Vector2Int playerChunk = manager.WorldToChunk(Player.Instance.transform.position);

            // Load new chunks
            for (int x = playerChunk.x - renderingDistance.x; x <= playerChunk.x + renderingDistance.x; x++)
            {
                for (int y = playerChunk.y - renderingDistance.y; y <= playerChunk.y + renderingDistance.y; y++)
                {
                    var coords = new Vector2Int(x, y);

                    if (loadedChunks.Contains(coords)) 
                    {
                        currentChunks.Add(coords);
                        continue;
                    }

                    var chunk = manager.GetChunk(coords);
                    if (chunk == null) continue;

                    currentChunks.Add(coords);
                    loadedChunks.Add(coords);

                    foreach (var obj in chunk.objects)
                    {
                        obj.SetActive(true);
                    }
                }
            }

            // Unload chunks no longer in range
            var toUnload = new List<Vector2Int>();
            foreach (var chunk in loadedChunks)
            {
                if (!currentChunks.Contains(chunk))
                    toUnload.Add(chunk);
            }

            foreach (var coords in toUnload)
            {
                var chunk = manager.GetChunk(coords);
                if (chunk == null) continue;

                foreach (var obj in chunk.objects)
                {
                    obj.SetActive(false);
                }

                loadedChunks.Remove(coords);
            }
        }
    }
}