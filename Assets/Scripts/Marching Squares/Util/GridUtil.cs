using Unity.Mathematics;
using UnityEngine;


namespace MarchingSquares.Util
{
    /// <summary>
    /// Blittable
    /// </summary>
    public struct GridData
    {
        public readonly int Resolution;
        public readonly float Spacing;
        public readonly float WorldSize;
        public readonly int PointCount;
        public int InterResolution;

        public GridData(float worldSize, int resolution)
        {
            this.WorldSize = worldSize;
            this.Resolution = resolution;
            this.InterResolution = resolution - 1;
            this.Spacing = worldSize / InterResolution;
            this.PointCount = resolution * resolution;
        }
    }
    public static class GridUtil
    {
        public static int Coord2Index(int res, int2 coord)
        {
            return res * coord.y + coord.x;
        }

        public static int Coord2Index(int res, int coordX, int coordY)
        {
            return res * coordY + coordX;
        }

        public static int2 Index2Coord(int res, int index)
        {
            return new int2(
                index % res,
                index / res
            );
        }
    }
}