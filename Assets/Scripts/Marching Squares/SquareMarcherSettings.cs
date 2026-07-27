using UnityEngine;
namespace MarchingSquares
{
    [System.Serializable]
    public struct SquareMarcherSettings
    {
        [field: SerializeField]
        public bool PerformSmoothing { get; private set; }

        [field: SerializeField]
        public float SurfaceLevel { get; private set; }
    }

}