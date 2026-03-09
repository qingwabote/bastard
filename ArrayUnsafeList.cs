using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

namespace Bastard
{
    public enum ArrayType : byte
    {
        Float,
        Vector
    }

    public readonly struct ArrayAllocator
    {
        public unsafe delegate void* AllocDelegate(ArrayType arrayType, ref ushort length, out short location);

        private struct AllocTag { }
        static public readonly SharedStatic<FunctionPointer<AllocDelegate>> Alloc = SharedStatic<FunctionPointer<AllocDelegate>>.GetOrCreate<AllocTag>();
    }

    public class ArrayAllocatorManaged
    {
        private static Array[] s_Arrays = new Array[4];
        private static Transient<short> m_Cursor = new();

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            ArrayAllocator.Alloc.Data = new(System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(m_AllocDelegate));
        }

        private readonly unsafe static ArrayAllocator.AllocDelegate m_AllocDelegate = Alloc;
        [AOT.MonoPInvokeCallback(typeof(ArrayAllocator.AllocDelegate))]
        static public unsafe void* Alloc(ArrayType arrayType, ref ushort length, out short location)
        {
            void* ptr;
            Array array = arrayType switch
            {
                ArrayType.Float => TransientArrayPool<float>.Shared.Rent(length, out ptr),
                ArrayType.Vector => TransientArrayPool<Vector4>.Shared.Rent(length, out ptr),
                _ => throw new InvalidOperationException($"Unexpected ArrayType: {arrayType}"),
            };
            length = (ushort)array.Length;
            if (s_Arrays.Length == m_Cursor.Value)
            {
                Array.Resize(ref s_Arrays, s_Arrays.Length * 2);
            }
            s_Arrays[m_Cursor.Value] = array;
            location = m_Cursor.Value++;
            return ptr;
        }

        static public Array Get(int location)
        {
            return s_Arrays[location];
        }
    }

    public unsafe struct ArrayUnsafeList<T> where T : unmanaged
    {
        private T* m_Ptr;

        private short m_Location;
        private ushort m_Capacity;
        private ushort m_Length;
        public readonly ArrayType ArrayType;

        public readonly T* Ptr => m_Ptr;
        public readonly int Location => m_Location;
        public readonly int Length => m_Length;


        public ArrayUnsafeList(ArrayType arrayType, int capacity)
        {
            m_Ptr = null;
            m_Location = -1;
            m_Capacity = 0;
            m_Length = 0;
            ArrayType = arrayType;
            Resize(capacity);
        }

        public void Add(T* src, int count)
        {
            if (m_Length + count > m_Capacity)
            {
                Resize(m_Length + count);
            }
            if (src == null)
            {
                UnsafeUtility.MemSet(m_Ptr + m_Length, 0, count * sizeof(T));
            }
            else
            {
                UnsafeUtility.MemCpy(m_Ptr + m_Length, src, count * sizeof(T));
            }
            m_Length += (ushort)count;
        }

        private void Resize(int capacity)
        {
            var size = ArrayType switch
            {
                ArrayType.Float => sizeof(float),
                ArrayType.Vector => sizeof(Vector4),
                _ => throw new InvalidOperationException($"Unexpected ArrayType: {ArrayType}"),
            };
            var length = (ushort)math.ceil(sizeof(T) * capacity / (float)size);
            void*
#if UNITY_WEBGL && !UNITY_EDITOR
            ptr = ArrayAllocatorManaged.Alloc(ArrayType, ref length, out m_Location);
#else
            ptr = ArrayAllocator.Alloc.Data.Invoke(ArrayType, ref length, out m_Location);
#endif
            UnsafeUtility.MemCpy(ptr, m_Ptr, m_Length * sizeof(T));
            m_Ptr = (T*)ptr;
            m_Capacity = (ushort)(size * length / sizeof(T));
        }
    }
}