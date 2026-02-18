using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Bastard
{
    public class TransientArrayPool<T>
    {
        public static readonly TransientArrayPool<T> Shared = new();

        private const int MaxPower = 16; // 支持到 2^16 = 65536
        private readonly Bucket[] m_Buckets = new Bucket[MaxPower];

        public TransientArrayPool()
        {
            for (int i = 0; i < MaxPower; i++)
                m_Buckets[i] = new Bucket(1 << i);
        }

        public unsafe T[] Rent(int minLength, out void* ptr)
        {
            int power = math.ceillog2(math.max(1, minLength));
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (power >= MaxPower)
                throw new ArgumentOutOfRangeException(nameof(minLength), $"max supported length is {1 << (MaxPower - 1)}");
#endif
            ref var bucket = ref m_Buckets[power];
            return bucket.Rent(out ptr);
        }

        private struct Bucket
        {
            private unsafe struct Slot
            {
                public T[] Array;
                public ulong Handle;
                public void* Ptr;
            }

            private readonly int m_Size;
            private readonly List<Slot> m_Slots;
            private Transient<int> m_Cursor;

            public Bucket(int size)
            {
                m_Size = size;
                m_Slots = new(4);
                m_Cursor = new();
            }

            public unsafe T[] Rent(out void* ptr)
            {
                if (m_Cursor.Value < m_Slots.Count)
                {
                    var slot = m_Slots[m_Cursor.Value++];
                    ptr = slot.Ptr;
                    return slot.Array;
                }

                var arr = new T[m_Size];
                void* p = UnsafeUtility.PinGCArrayAndGetDataAddress(arr, out var handle);
                m_Slots.Add(new()
                {
                    Array = arr,
                    Handle = handle,
                    Ptr = p
                });

                ptr = p;
                m_Cursor.Value++;
                return arr;
            }
        }
    }
}