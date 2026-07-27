
using Unity.Collections;
using Unity.Jobs;

namespace MarchingSquares
{

    public static class Triangulation2D
    {
        public struct UnmanagedCache : System.IDisposable
        {
            public NativeArray<byte> ContourData;

            public NativeArray<byte> TriangleData;

            public UnmanagedCache(Allocator allocator)
            {
                ContourData = new NativeArray<byte>(Triangulation2D.ContourData, allocator);

                TriangleData = new NativeArray<byte>(Triangulation2D.TriangleData, allocator);
            }

            public void Dispose()
            {
                ContourData.Dispose();

                TriangleData.Dispose();
            }
            public void Dispose(JobHandle dependency = default)
            {
                ContourData.Dispose(dependency);

                TriangleData.Dispose(dependency);
            }
        }


        public const byte BL = 0;
        public const byte L = 1;
        public const byte TL = 2;
        public const byte T = 3;
        public const byte TR = 4;
        public const byte R = 5;
        public const byte BR = 6;
        public const byte B = 7;
        public const byte O = 255;

        public static readonly byte[] ContourData = new byte[]
        {
            O, O, O, O,
            L, B, O, O, // corner
            T, L, O, O, // corner
            T, B, O, O, // slab (L)
            R, T, O, O, // corner
            L, T, R, B, // saddle BL TR
            R, L, O, O, // slab (T)
            R, B, O, O, // chip
            B, R, O, O, // corner
            L, R, O, O, // slab (B)
            T, R, B, L, // saddle TL BR
            T, R, O, O, // chip
            B, T, O, O, // slab (R)
            L, T, O, O, // chip
            B, L, O, O, // chip
            O, O, O, O // full
        };

        public static readonly byte[] TriangleData = new byte[]
        {
             O,  O,  O,  O,  O,  O,  O,  O,  O,  O,  O,  O, // empty
             B, BL,  L,  O,  O,  O,  O,  O,  O,  O,  O,  O, // corner
             L, TL,  T,  O,  O,  O,  O,  O,  O,  O,  O,  O, // corner
            BL, TL,  T, BL,  T,  B,  O,  O,  O,  O,  O,  O, // slab
             T, TR,  R,  O,  O,  O,  O,  O,  O,  O,  O,  O, // corner
             B, BL,  L,  T, TR,  R,  B,  L,  T,  T,  R,  B, // saddle BL TR
            TL, TR,  R, TL,  R,  L,  O,  O,  O,  O,  O,  O, // slab
            TL, TR,  R, TL,  R,  B, TL,  B, BL,  O,  O,  O, // chip
             R, BR,  B,  O,  O,  O,  O,  O,  O,  O,  O,  O, // corner
            BR, BL,  L, BR,  L,  R,  O,  O,  O,  O,  O,  O, // slab
             L, TL,  T,  R, BR,  B,  L,  T,  R,  R,  B,  L, // saddle TL BR
            BL, TL,  T, BL,  T,  R, BL,  R, BR,  O,  O,  O, // chip
            TR, BR,  B, TR,  B,  T,  O,  O,  O,  O,  O,  O, // slab
            BR, BL,  L, BR,  L,  T, BR,  T, TR,  O,  O,  O, // chip
            TR, BR,  B, TR,  B,  L, TR,  L, TL,  O,  O,  O, // chip
            BL, TL, TR, BL, TR, BR,  O,  O,  O,  O,  O,  O  // full
        };
    }
}