using UnityEngine;


namespace MarchingSquares
{
    public class Chunk : MonoBehaviour
    {
        [SerializeField] private ShapeDefinition shape;

        [ContextMenu("Test")]
        public void Test()
        {
            Debug.Log("Testing");
            var output = new Unity.Collections.NativeArray<float>(16 * 16, Unity.Collections.Allocator.TempJob);
            var t = shape.GetValueBatch(new GridData(0, 0, 1, GridResolution.Res16), output);
            t.Complete();
            string prettyPrintedResultGrid = "";
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    prettyPrintedResultGrid += output[i * 16 + j] + " ";
                }
                prettyPrintedResultGrid += "\n";
            }
            Debug.Log("Testing complete: \n" + prettyPrintedResultGrid);

            // return arrs
            output.Dispose();
        }
    }
}