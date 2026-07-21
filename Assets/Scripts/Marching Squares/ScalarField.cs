

using Unity.Mathematics;

namespace MarchingSquares
{
    public enum GridResolution
    {
        Res8 = 8,
        Res16 = 16,
        Res32 = 32,
        Res64 = 64,
        Res128 = 128,
        Res256 = 256
    }

    public struct GridData
    {
        public float worldX; // world position x
        public float worldY; // world position y
        public float stride; // cell size (space between samples)
        public GridResolution resolution; // samples (in x and y direction)

        // for bitwise shortcuts for converting between 1D and 2D indices (replacing y/res and x%res)
        private readonly int bitMask; //TODO rename?
        private readonly int bitShift;

        public readonly int ResInt => (int)resolution;


        public GridData(float worldX, float worldY, float stride, GridResolution resolution)
        {
            this.worldX = worldX;
            this.worldY = worldY;
            this.stride = stride;
            this.resolution = resolution;

            bitMask = (int)resolution - 1;

            bitShift = resolution switch
            {
                GridResolution.Res8 => 3,
                GridResolution.Res16 => 4,
                GridResolution.Res32 => 5,
                GridResolution.Res64 => 6,
                GridResolution.Res128 => 7,
                GridResolution.Res256 => 8,
                _ => throw new System.NotImplementedException()
            };
        }

        public int ToIndex(int x, int y)
        {
            return (y & bitMask) * ResInt + (x & bitMask);
        }

        public int2 FromIndex(int index)
        {
            int y = index >> bitShift;
            int x = index & bitMask;
            return new int2(x, y);
        }
    }

}