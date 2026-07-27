using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace MarchingSquares.Util
{
    public static class NativeArrayExtensions
    {
        public static void Fill<T>(this NativeArray<T> nativeArray, T value) where T : unmanaged
        {
            unsafe
            {
                void* ptr = nativeArray.GetUnsafePtr();

                UnsafeUtility.MemCpyReplicate(ptr, &value, sizeof(T), nativeArray.Length);
            }
        }
    }
}