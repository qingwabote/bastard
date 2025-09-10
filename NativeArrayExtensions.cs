using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Budget
{
    public static unsafe class NativeArrayExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ElementAt<T>(this NativeArray<T> array, int index) where T : unmanaged
        {
            CheckElementWriteAccess(array, index);
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly T ElementAtRO<T>(this NativeArray<T> array, int index) where T : unmanaged
        {
            CheckElementReadAccess(array, index);
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckElementReadAccess<T>(NativeArray<T> array, int index) where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index < 0 || index > array.Length)
            {
                FailOutOfRangeError(index, array.Length);
            }
            AtomicSafetyHandle.CheckReadAndThrow(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(array));
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckElementWriteAccess<T>(NativeArray<T> array, int index) where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (index < 0 || index > array.Length)
            {
                FailOutOfRangeError(index, array.Length);
            }
            AtomicSafetyHandle.CheckWriteAndThrow(NativeArrayUnsafeUtility.GetAtomicSafetyHandle(array));
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void FailOutOfRangeError(int index, int length)
        {
            throw new IndexOutOfRangeException($"Index {index} is out of range of '{length}' Length.");
        }
    }
}