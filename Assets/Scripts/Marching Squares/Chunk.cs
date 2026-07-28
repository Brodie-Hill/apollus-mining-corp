using System;
using System.Collections.Generic;
using System.Linq;
using MarchingSquares.Util;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
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

        private struct EdgePath
        {
            public readonly LineRenderer line;
            public readonly EdgeCollider2D collider;

            public readonly GameObject gameObject;

            public EdgePath(Vector2[] points, Transform parent)
            {
                gameObject = new GameObject("Edge Path");
                collider = gameObject.AddComponent<EdgeCollider2D>();
                line = gameObject.AddComponent<LineRenderer>();

                gameObject.transform.SetParent(parent, false);

                line.useWorldSpace = false;
                line.widthMultiplier = 0.03f;

                SetPoints(points);
            }

            public void SetPoints(Vector2[] points)
            {
                collider.points = points;
                line.positionCount = points.Length;
                for (int i = 0; i < line.positionCount; i++)
                {
                    line.SetPosition(i, (Vector3)points[i]);
                }
            }
        }

        [field: SerializeField]
        public ChunkSettings Settings { get; private set; }

        private Mesh generatedMesh;
        private MeshFilter meshFilter;
        private ChunkData data;
        private JobHandle generationHandle;
        private SquareMarcher selfMarcher;
        private List<EdgePath> edgePaths = new List<EdgePath>();
        private int activeEdgePaths;

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

                    BuildPaths();

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
        private void BuildPaths()
        {
            ClearPaths();

            // vec2 is blittable so its ok
            var allPoints = data.pathData.Points.AsArray().Reinterpret<Vector2>();
            int totalPaths = data.pathData.Count;

            for (int i = 0; i < data.pathData.Count; i++)
            {
                int start = data.pathData.PathStarts[i];
                // one past the end actually, but it works out in the math for length
                int end = (i + 1 < totalPaths) ? data.pathData.PathStarts[i + 1] : allPoints.Length;

                int length = end - start;
                if (length < -0) continue;

                // make lsit span over the sub data
                var natList = allPoints.GetSubArray(start, length);
                var managed = natList.ToArray();

                AddPath(managed);
            }
        }

        private void AddPath(Vector2[] points)
        {
            // if all existing ones active, make new one, otherwise recycle old one
            if (activeEdgePaths == edgePaths.Count)
            {
                EdgePath path = new EdgePath(points, transform);

                ++activeEdgePaths;
                edgePaths.Add(path);
                path.line.material = Settings.LineMat;
                return;
            }

            edgePaths[activeEdgePaths].SetPoints(points);

            edgePaths[activeEdgePaths].gameObject.SetActive(true);

            ++activeEdgePaths;

        }

        /// <summary>
        /// Doesnt actually remove colliders, just disables them so
        /// they can be recycled when AddCollider() is called next.
        /// </summary>
        private void ClearPaths()
        {
            foreach (var path in edgePaths)
            {
                path.gameObject.SetActive(false);
            }
            activeEdgePaths = 0;
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