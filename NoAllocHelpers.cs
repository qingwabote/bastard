using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Bastard
{
    // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Runtime/Export/Scripting/NoAllocHelpers.bindings.cs
    public static class NoAllocHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ResetListContents<T>(List<T> list, T[] array, int length)
        {
            var tListAccess = UnsafeUtility.As<List<T>, ListPrivateFieldAccess<T>>(ref list);
            tListAccess._items = array;
            tListAccess._size = length;
            tListAccess._version++;
        }

        private class ListPrivateFieldAccess<T>
        {
            internal T[] _items;
            internal int _size;
            internal int _version;
        }
    }
}