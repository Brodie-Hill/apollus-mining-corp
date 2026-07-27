using MarchingSquares.Util;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


namespace MarchingSquares
{

    public abstract class ShapeDefinition<T> : ShapeDefinition where T : struct, IShapeEvaluator
    {
        protected abstract T ShapeEvaluator { get; }

        public override JobHandle GetValueBatch(float2 worldOffset, GridData grid, NativeArray<float> results, JobHandle dependency = default)
        {
            var job = new ShapeEvaluatorJob<T>
            (
                ShapeEvaluator,
                worldOffset,
                grid,
                results
            );

            return job.Schedule(results.Length, 32, dependency);
        }
    }
    public abstract class ShapeDefinition : ScriptableObject
    {
        public abstract JobHandle GetValueBatch(float2 worldOffset, GridData grid, NativeArray<float> results, JobHandle dependency = default);
    }

    public interface IShapeEvaluator
    {
        float Evaluate(float x, float y);
    }

    [BurstCompile(CompileSynchronously = true)]
    internal struct ShapeEvaluatorJob<T> : IJobParallelFor where T : struct, IShapeEvaluator
    {
        [ReadOnly]
        public T shapeEvaluator;

        [ReadOnly]
        public GridData inputGrid;

        [ReadOnly]
        public float2 worldOffset;

        [WriteOnly]
        public NativeArray<float> output;


        public ShapeEvaluatorJob(T shapeEvaluator, float2 worldOffset, GridData grid, NativeArray<float> output)
        {
            this.shapeEvaluator = shapeEvaluator;
            this.inputGrid = grid;
            this.worldOffset = worldOffset;
            this.output = output;
        }

        public void Execute(int index)
        {
            int2 coord = GridUtil.Index2Coord(inputGrid.Resolution, index);

            float2 worldPoint = worldOffset + (float2)coord * inputGrid.Spacing;

            output[index] = shapeEvaluator.Evaluate(worldPoint.x, worldPoint.y);
        }
    }
}