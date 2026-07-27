using System;
using System.IO;
using System.Linq;
using MarchingSquares.Util;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;


namespace MarchingSquares
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class Chunk : MonoBehaviour
    {
        public enum ChunkLoadState
        {
            Unloaded,
            Loading,
            Loaded
        }

        internal struct ChunkData : IDisposable
        {
            public ScalarField field;
            public MeshBuffers meshData;
            public PathBuffers pathData;

            public ChunkData(GridData grid, Allocator alloc)
            {
                field = new ScalarField
                {
                    geo = grid,
                    naturalField = new NativeArray<float>(grid.PointCount, alloc),
                    userField = new NativeArray<float>(grid.PointCount, alloc)
                };
                meshData = new MeshBuffers(1024, alloc);
                pathData = new PathBuffers(alloc);
            }

            public void Clear()
            {
                meshData.Clear();
                pathData.Clear();
            }
            public void Dispose()
            {
                //TODO improve structs api
                if (field.naturalField.IsCreated) field.naturalField.Dispose();
                if (field.userField.IsCreated) field.userField.Dispose();
                if (meshData.Vertices.IsCreated) meshData.Dispose();
                if (pathData.Points.IsCreated) pathData.Dispose();
            }
        }


        [field: SerializeField]
        public ChunkSettings Settings { get; private set; }

        private Mesh generatedMesh;
        private MeshFilter meshFilter;
        private ChunkData data;
        private JobHandle generationHandle;
        private SquareMarcher selfMarcher;

        public ChunkLoadState State { get; private set; }
        public bool IsDirty { get; private set; }
        public bool Loaded => State == ChunkLoadState.Loaded;


        #region Unity Messages

        private void Awake()
        {
            data = new ChunkData(Settings.GetGridData(), Allocator.Persistent);

            State = ChunkLoadState.Unloaded;
            generatedMesh = new Mesh();
            generatedMesh.name = "Generated Mesh Pending";
            (meshFilter = GetComponent<MeshFilter>()).sharedMesh = generatedMesh;

            generatedMesh.MarkDynamic();

            var layout = MarchingSquares.Vertex.GetDescriptor();

            var ptCnt = data.field.geo.PointCount;
            var res = data.field.geo.Resolution;

            int maxVertexCount = 3 * ptCnt + 2 * res - 1;
            int maxIndexCount = 3 * ((res - 1) * (res - 1) * 4);

            generatedMesh.SetVertexBufferParams(maxVertexCount, layout);
            generatedMesh.SetIndexBufferParams(maxIndexCount, IndexFormat.UInt32); // Use UInt16 if maxVertexCount < 65535

            selfMarcher = new SquareMarcher(Settings.GenerationSettings);
        }

        void FixedUpdate()
        {
            if (IsDirty && State != ChunkLoadState.Loading)
            {
                // update without re-evaluating natural shape
                StartLoadingJob(false);
                IsDirty = false;
            }

            // handle loading completed
            if (State == ChunkLoadState.Loading)
            {
                if (generationHandle.IsCompleted)
                {
                    ApplyMeshData();
                    State = ChunkLoadState.Loaded;
                }
            }
        }

        private void OnDestroy()
        {
            generationHandle.Complete();
            data.Dispose();
        }

        #endregion

        public void Load()
        {
            StartLoadingJob(true);

            gameObject.SetActive(true);
        }

        public void RefreshInPlace()
        {
            if (State != ChunkLoadState.Loaded) return;
            StartLoadingJob(false);
        }

        public void Unload()
        {
            generationHandle.Complete();

            gameObject.SetActive(false);
            //generatedMesh.Clear();
            data.Clear();
            State = ChunkLoadState.Unloaded;
        }

        public void MarkDirty()
        {
            IsDirty = true;
        }


        public void Complete() => generationHandle.Complete();

        private void StartLoadingJob(bool updateNaturalField)
        {
            if (!generationHandle.IsCompleted) return;

            State = ChunkLoadState.Loading;

            JobHandle dependsOn = default;

            if (updateNaturalField)
            {
                float2 pos2 = new float2(transform.position.x, transform.position.y);
                dependsOn = Settings.ShapeSettings.GetValueBatch(pos2, data.field.geo, data.field.naturalField);
            }

            generationHandle = selfMarcher.Generate(data.field, ref data.meshData, ref data.pathData, dependsOn);
        }

        private void ApplyMeshData()
        {
            generationHandle.Complete();

            int actualVertCount = data.meshData.Vertices.Length;
            int actualIndexCount = data.meshData.Indices.Length;


            generatedMesh.SetVertexBufferData(data.meshData.Vertices.AsArray(), 0, 0, actualVertCount);
            generatedMesh.SetIndexBufferData(data.meshData.Indices.AsArray(), 0, 0, actualIndexCount);

            // 3. Slice the mesh so it only renders the indices we just generated
            var subMesh = new SubMeshDescriptor(0, actualIndexCount);
            generatedMesh.SetSubMesh(0, subMesh, MeshUpdateFlags.DontRecalculateBounds);

            generatedMesh.bounds = new Bounds(
                Vector3.one * Settings.ChunkSize * 0.5f,
                Vector3.one * Settings.ChunkSize
            );
        }

        #region Tests

        [ContextMenu("Load")]
        private void TestLoad() => Load();

        [ContextMenu("Refresh")]
        private void TestRefresh() => RefreshInPlace();

        [ContextMenu("Unload")]
        private void TestUnload() => Unload();

        [ContextMenu("Mark Dirty")]
        private void TestMarkDirty() => MarkDirty();

        #endregion
    }
}