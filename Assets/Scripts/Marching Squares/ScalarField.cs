using System;
using MarchingSquares.Util;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;


namespace MarchingSquares
{
    public struct ScalarField : IDisposable
    {
        public GridData geo;
        public NativeArray<float> naturalField;
        public NativeArray<float> userField;


        public float GetValue(int index)
        {
            return naturalField[index];
        }

        public float Scalar(int2 coord)
        {
            int index = GridUtil.Coord2Index(geo.Resolution, coord);
            return naturalField[index];
        }

        public void Dispose()
        {
            naturalField.Dispose();
            userField.Dispose();
        }
        public void Dispose(JobHandle dependency = default)
        {
            naturalField.Dispose(dependency);
            userField.Dispose(dependency);
        }
    }
}