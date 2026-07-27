using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using MarchingSquares;


// needed since the shapes use generic jobs and Burst needs to know about them to compile them AoT
using ConstantShapeJob = MarchingSquares.ShapeEvaluatorJob<ConstantShapeEvaluator>;

[assembly: RegisterGenericJobType(typeof(ConstantShapeJob))]


[System.Serializable]
public struct ConstantShapeEvaluator : IShapeEvaluator
{
    [SerializeField]
    private float value;

    public float Evaluate(float x, float y)
    {
        return value;
    }
}

[CreateAssetMenu(fileName = "Constant Shape", menuName = "Marching Squares/Shapes/Constant")]
public class ConstantShape : ShapeDefinition<ConstantShapeEvaluator>
{
    // nothing needed but the definition for unity to create assets with
    // all is handled in the evaluator and base class
    [SerializeField] private ConstantShapeEvaluator shapeEvaluator;

    protected override ConstantShapeEvaluator ShapeEvaluator => shapeEvaluator;
}
