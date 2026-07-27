

using MarchingSquares.Util;
using Unity.Mathematics;
using UnityEngine;

namespace MarchingSquares
{
    [CreateAssetMenu(fileName = "ChunkSettings", menuName = "Marching Squares/Chunk Settings")]
    public class ChunkSettings : ScriptableObject
    {
        [field: SerializeField]
        public int Cells { get; private set; } = 16;

        [field: SerializeField]
        public float ChunkSize { get; private set; } = 10f;

        [field: SerializeFieldReadOnly]
        private float CellSize;

        [field: SerializeField]
        public ShapeDefinition ShapeSettings { get; private set; }

        [field: SerializeField]
        public SquareMarcherSettings GenerationSettings { get; private set; }

        public bool Valid => Validate();

        void OnValidate()
        {
            CellSize = ChunkSize / Cells;
        }

        public Vector2Int W2C(Vector2 worldPos)
        {
            float chunkSize = ChunkSize;

            int x = Mathf.FloorToInt(worldPos.x / chunkSize);
            int y = Mathf.FloorToInt(worldPos.y / chunkSize);

            return new Vector2Int(x, y);
        }
        public int2 W2C(float2 worldPos)
        {
            float chunkSize = ChunkSize;

            int x = (int)(worldPos.x / chunkSize);
            int y = (int)(worldPos.y / chunkSize);

            return new int2(x, y);
        }

        public Vector2 C2W(Vector2Int chunkPos)
        {
            float chunkSize = ChunkSize;

            float x = chunkPos.x * chunkSize;
            float y = chunkPos.y * chunkSize;

            return new Vector2(x, y);
        }
        public float2 C2W(int2 chunkPos)
        {
            float chunkSize = ChunkSize;

            float x = chunkPos.x * chunkSize;
            float y = chunkPos.y * chunkSize;

            return new float2(x, y);
        }

        public GridData GetGridData()
        {
            return new GridData(ChunkSize, Cells + 1);
        }

        private bool Validate()
        {
            // gridsettings is struct so shouldnt be nullable
            // shape settings could be null
            return ShapeSettings != null;
        }
    }
}