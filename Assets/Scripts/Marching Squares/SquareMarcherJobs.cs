using MarchingSquares.Util;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MarchingSquares.Jobs
{
    [BurstCompile(CompileSynchronously = true)]
    internal struct JobMarchSquares : IJob
    {
        [ReadOnly]
        public SquareMarcherSettings Settings;

        [ReadOnly]
        public ScalarField Grid;

        [ReadOnly]
        public Triangulation2D.UnmanagedCache TriTables;

        public MeshBuffers MeshOut;

        public MarcherCache TempCache;


        private bool ValidateInputs => true; //TODO


        public void Execute()
        {
            if (!ValidateInputs) return;

            MeshOut.Clear();

            TempCache.Reset();

            var interRes = Grid.geo.InterResolution;

            for (int y = 0; y < interRes; ++y)
            {
                for (int x = 0; x < interRes; ++x)
                {
                    int index = x + y * interRes + y;

                    int configNumber = GetConf(index);

                    TriangulateSquare(index, configNumber);

                    ContourSquare(index, configNumber);
                }
            }
        }

        void TriangulateSquare(int cellIndex, int configNumber)
        {
            var triangulation = new NativeSlice<byte>(TriTables.TriangleData, configNumber * 12, 12);
            for (int triangle = 0; triangle < triangulation.Length; ++triangle)
            {
                if (triangulation[triangle] == Triangulation2D.O) break;

                MeshOut.Indices.Add(ResolveVertex(cellIndex, triangulation[triangle]));
            }
        }

        void ContourSquare(int cellIndex, int configNumber)
        {
            var contour = new NativeSlice<byte>(TriTables.ContourData, configNumber * 4, 4);
            for (int segment = 0; segment < contour.Length; segment += 2)
            {
                if (contour[segment] == Triangulation2D.O) break;

                int a = ResolveVertex(cellIndex, contour[segment]);
                int b = ResolveVertex(cellIndex, contour[segment + 1]);

                TempCache.forwardConnections[a] = b;
                TempCache.backwardConnections[b] = a;

                TempCache.boundaryVerts.Add(a);
            }
        }

        int ResolveVertex(int cellIndex, int localIndex)
        {
            // switch on the vertex local index 
            return localIndex switch
            {
                Triangulation2D.BL => ResolveGridVert(cellIndex),
                Triangulation2D.BR => ResolveGridVert(cellIndex + 1),
                Triangulation2D.TR => ResolveGridVert(cellIndex + Grid.geo.Resolution + 1),
                Triangulation2D.TL => ResolveGridVert(cellIndex + Grid.geo.Resolution),
                Triangulation2D.B => ResolveMidXVert(cellIndex),
                Triangulation2D.L => ResolveMidYVert(cellIndex),
                Triangulation2D.T => ResolveMidXVert(cellIndex + Grid.geo.Resolution),
                Triangulation2D.R => ResolveMidYVert(cellIndex + 1),

                _ => throw new System.Exception("Invalid vertex index")
            };
        }

        int ResolveGridVert(int cellIndex)
        {
            // get object space location of the vertex on the grid point (square bottom-left corner)

            if (TempCache.gridVerts[cellIndex] != -1) return TempCache.gridVerts[cellIndex];

            int2 vCoord = GridUtil.Index2Coord(Grid.geo.Resolution, cellIndex);

            EmitVertex(new float3(vCoord.x * Grid.geo.Spacing, vCoord.y * Grid.geo.Spacing, 0f));
            TempCache.gridVerts[cellIndex] = MeshOut.Vertices.Count - 1;

            return TempCache.gridVerts[cellIndex];
        }
        int ResolveMidXVert(int cellIndex)
        {
            if (TempCache.midXVerts[cellIndex] != -1) return TempCache.midXVerts[cellIndex];

            // cache index of bottom left corner of the cell specified
            // this a reference point for the other 3 corners of this cell
            // (to the right and top and top right of it)

            float bl = Grid.GetValue(cellIndex);
            float br = Grid.GetValue(cellIndex + 1);
            float3 blVert = GetTemporaryGridVert(cellIndex);
            float3 brVert = GetTemporaryGridVert(cellIndex + 1);

            float t = Settings.PerformSmoothing ? math.unlerp(bl, br, Settings.SurfaceLevel) : 0.5f; //TODO is 0 the surface point?
            EmitVertex(math.lerp(blVert, brVert, t));
            TempCache.midXVerts[cellIndex] = MeshOut.Vertices.Count - 1;

            return TempCache.midXVerts[cellIndex];
        }
        int ResolveMidYVert(int cellIndex)
        {
            if (TempCache.midYVerts[cellIndex] != -1) return TempCache.midYVerts[cellIndex];

            // cache index of bottom left corner of the cell specified
            // this a reference point for the other 3 corners of this cell
            // (to the right and top and top right of it)

            float bl = Grid.GetValue(cellIndex);
            float tl = Grid.GetValue(cellIndex + Grid.geo.Resolution);
            float3 blVert = GetTemporaryGridVert(cellIndex);
            float3 tlVert = GetTemporaryGridVert(cellIndex + Grid.geo.Resolution);

            float t = Settings.PerformSmoothing ? math.unlerp(bl, tl, Settings.SurfaceLevel) : 0.5f; //TODO is 0 the surface point?
            EmitVertex(math.lerp(blVert, tlVert, t));
            TempCache.midYVerts[cellIndex] = MeshOut.Vertices.Count - 1;

            return TempCache.midYVerts[cellIndex];
        }

        float3 GetTemporaryGridVert(int cellIndex)
        {
            // just getting a temp 3d grid point for lerping in MidX and MidY verts
            int2 vCoord = GridUtil.Index2Coord(Grid.geo.Resolution, cellIndex);

            return new float3(vCoord.x * Grid.geo.Spacing, vCoord.y * Grid.geo.Spacing, 0f);
        }

        void EmitVertex(float3 position, float2 uv = default, float3 color = default)
        {
            MeshOut.Vertices.Add(new Vertex(
                position,
                uv,
                color
            ));

            TempCache.forwardConnections.Add(-1);
            TempCache.backwardConnections.Add(-1);

        }

        internal int GetConf(int cellIndex)
        {
            /*
                clockwise order
                1---2
                |   |
                0---3
            */
            return
                  (IsInsideShape(Grid.GetValue(cellIndex)) ? 1 : 0)
                | (IsInsideShape(Grid.GetValue(cellIndex + Grid.geo.Resolution + 1)) ? 4 : 0)
                | (IsInsideShape(Grid.GetValue(cellIndex + Grid.geo.Resolution)) ? 2 : 0)
                | (IsInsideShape(Grid.GetValue(cellIndex + 1)) ? 8 : 0);
        }
        bool IsInsideShape(float scalar)
        {
            return scalar > Settings.SurfaceLevel; // surface level is 0
        }
    }

    internal struct JobTracePaths : IJob
    {
        // [ReadOnly]
        // public SquareMarcherSettings Settings;

        [ReadOnly]
        public MeshBuffers PointInfo;

        public PathBuffers PathOut;

        public MarcherCache TempCache;


        public void Execute()
        {
            for (int i = 0; i < TempCache.boundaryVerts.Count; i++)
            {
                var start = TempCache.boundaryVerts[i];

                if (TempCache.forwardConnections[start] == -1) continue;

                int pathI = start;
                while (true)
                {
                    // walk the path, disconnecting verts as we touch them as to not start walking same path from another vert
                    //outlinePaths[^1].Add(verts.positions[pathI]);
                    if (TempCache.backwardConnections[pathI] == -1) break;
                    int newPathI = TempCache.backwardConnections[pathI];
                    TempCache.backwardConnections[pathI] = -1;
                    pathI = newPathI;
                }
                PathOut.PathStarts.Add(PathOut.Points.Count);
                while (true)
                {
                    // walk the path, disconnecting verts as we touch them as to not start walking same path from another vert
                    PathOut.Points.Add(PointInfo.Vertices[pathI].Position);
                    if (TempCache.forwardConnections[pathI] == -1) break;
                    int newPathI = TempCache.forwardConnections[pathI];
                    TempCache.forwardConnections[pathI] = -1;
                    pathI = newPathI;
                }
            }
        }
    }
}