using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;
using MarchingSquares;


// needed since the shapes use generic jobs and Burst needs to know about them to compile them AoT
using TestSimplexJob = MarchingSquares.ShapeEvaluatorJob<TestSimplexEvaluator>;

[assembly: RegisterGenericJobType(typeof(TestSimplexJob))]


[System.Serializable]
public struct TestSimplexEvaluator : IShapeEvaluator
{
    [SerializeField] private float2 offset;

    public float Evaluate(float x, float y)
    {
        return noise.snoise(new float2(x, y) + offset);
    }
}

[CreateAssetMenu(fileName = "TestSimplexShape", menuName = "MarchingSquares/Shapes/BasicSimplex")]
public class TestSimplexShape : ShapeDefinition<TestSimplexEvaluator>
{
    // nothing needed but the definition for unity to create assets with
    // all is handled in the evaluator and base class
    [SerializeField] private TestSimplexEvaluator shapeEvaluator;

    protected override TestSimplexEvaluator ShapeEvaluator => shapeEvaluator;
}
