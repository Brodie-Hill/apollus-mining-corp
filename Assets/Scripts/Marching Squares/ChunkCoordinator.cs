using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

using ChunkCoord = UnityEngine.Vector2Int;


namespace MarchingSquares
{
    public class ChunkCoordinator : MonoBehaviour
    {
        [SerializeField]
        private ChunkSettings GlobalSettings;

        [SerializeField]
        private Chunk prefabbedChunk;

        [SerializeField]
        private Transform[] pointsOfInterest;

        [SerializeField]
        private float distanceFactor;

        [SerializeField]
        [Tooltip("If true, forces jobs to complete before the frame renders. If false, chunks load asynchronously over possibley multiple frames.")]
        private bool forceSynchronousGeneration = true;

        private ChunkCoord[] poiChunkCoords;

        private Dictionary<ChunkCoord, Chunk> loadedChunks = new Dictionary<ChunkCoord, Chunk>();
        private Queue<Chunk> unloadedChunks = new Queue<Chunk>();
        private HashSet<ChunkCoord> interestingChunkPositions = new HashSet<ChunkCoord>();
        private HashSet<ChunkCoord> boringChunkPositions = new HashSet<ChunkCoord>();
        private List<Chunk> chunksGeneratingThisFrame = new List<Chunk>();
        bool refresh;


        void OnValidate()
        {
            if (pointsOfInterest == null) return;

            poiChunkCoords = new ChunkCoord[pointsOfInterest.Length];
            for (int i = 0; i < poiChunkCoords.Length; i++)
            {
                poiChunkCoords[i] = World2Coord(pointsOfInterest[i].position);
            }
        }

        void Awake()
        {
            if (pointsOfInterest == null) return;

            poiChunkCoords = new ChunkCoord[pointsOfInterest.Length];
            for (int i = 0; i < poiChunkCoords.Length; i++)
            {
                poiChunkCoords[i] = World2Coord(pointsOfInterest[i].position);
            }

            refresh = true;
        }

        void FixedUpdate()
        {
            for (int i = 0; i < pointsOfInterest.Length; i++)
            {
                TrackPoi(i);
            }

            if (!refresh) return;

            // dop chunk loading

            UpdateChunks();

            refresh = false;
        }

        void LateUpdate()
        {
            if (!forceSynchronousGeneration) return;

            if (chunksGeneratingThisFrame.Count == 0) return;

            foreach (var chunk in chunksGeneratingThisFrame)
            {
                chunk.Complete();
            }
            //TODO hmm?
            chunksGeneratingThisFrame.Clear();
        }

        private void UpdateChunks()
        {
            interestingChunkPositions.Clear();
            int d = Mathf.RoundToInt(distanceFactor);

            for (int i = 0; i < poiChunkCoords.Length; i++)
            {
                ChunkCoord center = poiChunkCoords[i];
                for (int x = -d; x <= d; x++)
                {
                    for (int z = -d; z <= d; z++)
                    {
                        interestingChunkPositions.Add(new ChunkCoord(center.x + x, center.y + z));
                    }
                }
            }

            boringChunkPositions.Clear();
            foreach (var kvp in loadedChunks)
            {
                if (!interestingChunkPositions.Contains(kvp.Key))
                {
                    boringChunkPositions.Add(kvp.Key);
                }
            }

            // real unload chunks
            foreach (var coord in boringChunkPositions)
            {
                Chunk chunkToUnload = loadedChunks[coord];
                chunkToUnload.Unload();
                unloadedChunks.Enqueue(chunkToUnload);
                loadedChunks.Remove(coord);
            }

            // real load chunks
            float chunkSize = GlobalSettings.ChunkSize;
            foreach (var coord in interestingChunkPositions)
            {
                if (loadedChunks.ContainsKey(coord)) continue;

                Chunk newChunk;

                if (unloadedChunks.Count > 0)
                {
                    newChunk = unloadedChunks.Dequeue();
                    newChunk.gameObject.SetActive(true);
                }
                else
                {
                    newChunk = Instantiate(prefabbedChunk, transform);
                }

                // reinit chunk data
                newChunk.transform.position = new Vector2(coord.x * chunkSize, coord.y * chunkSize);

                // load with new data
                newChunk.Load();
                chunksGeneratingThisFrame.Add(newChunk);

                loadedChunks.Add(coord, newChunk);
            }
        }

        void TrackPoi(int poiIndex)
        {
            var transform = pointsOfInterest[poiIndex];
            var lastCoord = poiChunkCoords[poiIndex];

            // check pos
            var currentChunkCoord = World2Coord(transform.position);
            if (lastCoord == currentChunkCoord) return;

            poiChunkCoords[poiIndex] = currentChunkCoord;
            refresh = true;
        }

        public ChunkCoord World2Coord(Vector3 worldPos)
        {
            return GlobalSettings.W2C(worldPos);
        }
    }
}
