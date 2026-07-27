using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using MarchingSquares;


// needed since the shapes use generic jobs and Burst needs to know about them to compile them AoT
using SimplexShapeJob = MarchingSquares.ShapeEvaluatorJob<SimplexShapeEvaluator>;

[assembly: RegisterGenericJobType(typeof(SimplexShapeJob))]


[System.Serializable]
public struct SimplexShapeEvaluator : IShapeEvaluator
{
    [SerializeField]
    private float2 offset;

    [SerializeField]
    private float scaleFactor;


    public float Evaluate(float x, float y)
    {
        return noise.snoise(scaleFactor * new float2(x, y) + offset);
    }
}

[CreateAssetMenu(fileName = "Simplex Shape", menuName = "Marching Squares/Shapes/Basic Simplex")]
public class SimplexShape : ShapeDefinition<SimplexShapeEvaluator>
{
    // nothing needed but the definition for unity to create assets with
    // all is handled in the evaluator and base class
    [SerializeField] private SimplexShapeEvaluator shapeEvaluator;

    protected override SimplexShapeEvaluator ShapeEvaluator => shapeEvaluator;
}
