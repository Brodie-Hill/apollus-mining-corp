using System;
using System.Runtime.InteropServices;
using MarchingSquares.Jobs;
using MarchingSquares.Util;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;


namespace MarchingSquares
{
    public sealed class SquareMarcher : IDisposable
    {
        private readonly SquareMarcherSettings settings;
        private readonly Triangulation2D.UnmanagedCache marcherLut;

        public SquareMarcher(SquareMarcherSettings settings)
        {
            this.settings = settings;
            marcherLut = new Triangulation2D.UnmanagedCache(Allocator.Persistent);
        }

        public void Dispose()
        {
            marcherLut.Dispose();
        }
        public void Dispose(JobHandle dependency = default)
        {
            marcherLut.Dispose(dependency);
        }

        public JobHandle Generate(
            in ScalarField field,
            ref MeshBuffers mesh,
            ref PathBuffers paths,
            JobHandle dependency = default
        )
        {
            var cache = new MarcherCache(field.geo, Allocator.TempJob);

            var marchJob = new JobMarchSquares
            {
                Settings = settings,
                Grid = field,
                TriTables = marcherLut,
                MeshOut = mesh,
                TempCache = cache
            };

            JobHandle marchHandle = marchJob.Schedule(dependency);

            var pathJob = new JobTracePaths
            {
                PointInfo = mesh,
                TempCache = cache,
                PathOut = paths
            };


            JobHandle pathHandle = pathJob.Schedule(marchHandle);
            cache.Dispose(pathHandle);
            return pathHandle;
            //return cache.Dispose(pathHandle);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public float3 Position;
        public float2 UV;
        public float3 Color;

        // later when used
        // public float3 Normal;

        public Vertex(float3 pos, float2 uv = default, float3 color = default)
        {
            this.Position = pos;
            this.UV = uv;
            this.Color = color;
        }

        public static VertexAttributeDescriptor[] GetDescriptor()
        {
            return new[] {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 3)
            };
        }
    }

    public struct MeshBuffers : IDisposable
    {
        public NativeList<Vertex> Vertices;
        public NativeList<int> Indices;

        public MeshBuffers(int capacity = 0, Allocator alloc = Allocator.TempJob)
        {
            Vertices = new(capacity, alloc);
            Indices = new(capacity * 3, alloc);
        }

        public void Clear()
        {
            Vertices.Clear();
            Indices.Clear();
        }

        public void Dispose()
        {
            Vertices.Dispose();
            Indices.Dispose();
        }

        public void Dispose(JobHandle dependency = default)
        {
            Vertices.Dispose(dependency);
            Indices.Dispose(dependency);
        }
    }

    public struct PathBuffers : IDisposable
    {
        public NativeList<float2> Points;
        public NativeList<int> PathStarts;

        public int Count => PathStarts.Length;

        public PathBuffers(Allocator allocator = Allocator.TempJob)
        {
            Points = new NativeList<float2>(allocator);
            PathStarts = new NativeList<int>(allocator);
        }

        public void Clear()
        {
            Points.Clear();
            PathStarts.Clear();
        }

        public void Dispose()
        {
            Points.Dispose();
            PathStarts.Dispose();
        }

        public void Dispose(JobHandle dependency = default)
        {
            Points.Dispose(dependency);
            PathStarts.Dispose(dependency);
        }
    }

    internal struct MarcherCache : IDisposable
    {
        // formatted in res x res
        public NativeArray<int> gridVerts;
        public NativeArray<int> midXVerts;
        public NativeArray<int> midYVerts;

        // formatted in flat list of only the verts on a boundary / outline of the shape
        // in no particular order
        public NativeList<int> boundaryVerts;

        // index in these will match vertex in output, tells us that vertex's connections
        public NativeList<int> forwardConnections;
        public NativeList<int> backwardConnections;


        public MarcherCache(GridData grid, Allocator alloc)
        {
            gridVerts = new NativeArray<int>(grid.PointCount, alloc);
            midXVerts = new NativeArray<int>(grid.PointCount, alloc);
            midYVerts = new NativeArray<int>(grid.PointCount, alloc);
            boundaryVerts = new NativeList<int>(alloc);
            forwardConnections = new NativeList<int>(alloc);
            backwardConnections = new NativeList<int>(alloc);
        }

        public void Reset()
        {
            boundaryVerts.Clear();
            forwardConnections.Clear();
            backwardConnections.Clear();

            gridVerts.Fill(-1);
            midXVerts.Fill(-1);
            midYVerts.Fill(-1);
        }
        public void Dispose()
        {
            gridVerts.Dispose();
            midXVerts.Dispose();
            midYVerts.Dispose();
            boundaryVerts.Dispose();
            forwardConnections.Dispose();
            backwardConnections.Dispose();
        }

        public void Dispose(JobHandle dependency = default)
        {
            midXVerts.Dispose(dependency);
            midYVerts.Dispose(dependency);
            gridVerts.Dispose(dependency);
            boundaryVerts.Dispose(dependency);
            forwardConnections.Dispose(dependency);
            backwardConnections.Dispose(dependency);
        }
    }

}

/// <summary>
/// /// OLD SHIT BELOW HERE //////
/// </summary>

//     public class SquareMarcher : IDisposable
//     {
//         SquareMarcherSettings settings;
//         Triangulation2D.UnmanagedCache marcherLut;

//         public SquareMarcher(SquareMarcherSettings settings)
//         {
//             marcherLut = new Triangulation2D.UnmanagedCache(Allocator.Persistent);
//         }

//         public void Dispose()
//         {
//             marcherLut.Dispose();
//         }

//         public JobHandle Generate(
//             ScalarField field,
//             MeshBuffers mesh


//             PathBuffer paths, // output vertex paths info
//             JobHandle dependency = default
//             )
//         {
//             // allocate unmanaged vars
//             var cache = new MarcherCache(grid, Allocator.TempJob);

//             cache.gridVerts.Fill(-1);
//             cache.midXVerts.Fill(-1);
//             cache.midYVerts.Fill(-1);

//             var marchJob = new MarchSquareJob
//             (
//                 settings,
//                 grid,
//                 procedural,
//                 edits,
//                 marcherLut,
//                 cache,
//                 verts,
//                 indices,
//                 paths
//             );

//             JobHandle marchHandle = marchJob.Schedule(dependency);

//             var pathJob = new PathTraceJob
//             {
//                 vertexInfo = verts,
//                 tempCache = cache,
//                 pathsOut = paths
//             };

//             JobHandle pathHandle = pathJob.Schedule(marchHandle);
//             // schedule disposal of unmanaged vars, depend on ms job

//             cache.Dispose(pathHandle);

//             return pathHandle;
//         }
//     }

