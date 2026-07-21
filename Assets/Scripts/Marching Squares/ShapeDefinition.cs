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

        public override JobHandle GetValueBatch(GridData batch, NativeArray<float> results, JobHandle dependency = default)
        {
            var job = new ShapeEvaluatorJob<T>
            (
                ShapeEvaluator,
                batch,
                results
            );

            return job.Schedule(results.Length, 32, dependency);
        }
    }
    public abstract class ShapeDefinition : ScriptableObject
    {

        public abstract JobHandle GetValueBatch(GridData batch, NativeArray<float> results, JobHandle dependency = default);
    }

    public interface IShapeEvaluator
    {
        float Evaluate(float x, float y);
    }

    [BurstCompile(CompileSynchronously = true)]
    public struct ShapeEvaluatorJob<T> : IJobParallelFor where T : struct, IShapeEvaluator
    {
        [ReadOnly]
        public T shapeEvaluator;

        [ReadOnly]
        public GridData input;

        [WriteOnly]
        public NativeArray<float> output;


        public ShapeEvaluatorJob(T shapeEvaluator, GridData input, NativeArray<float> output)
        {
            this.shapeEvaluator = shapeEvaluator;
            this.input = input;
            this.output = output;
        }

        public void Execute(int index)
        {
            int2 gridPos = input.FromIndex(index);

            float worldX = input.worldX + gridPos.x * input.stride;
            float worldY = input.worldY + gridPos.y * input.stride;

            output[index] = shapeEvaluator.Evaluate(worldX, worldY);
        }
    }
}