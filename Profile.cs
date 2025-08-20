using System;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace Bastard
{
    public struct Entry
    {
        public FixedString32Bytes Name;
        public float Delta;
    }

    public struct Profile
    {
        public struct Scope : IDisposable
        {
            private int m_Entry;

            public Scope(int entry)
            {
                Begin(entry);
                m_Entry = entry;
            }

            public void Dispose()
            {
                End(m_Entry);
            }
        }

        private class EntriesTag { }
        public static readonly SharedStatic<NativeList<Entry>> Entries = SharedStatic<NativeList<Entry>>.GetOrCreate<Profile, EntriesTag>();

        private class TimesTag { }
        private static readonly SharedStatic<NativeList<float>> s_Times = SharedStatic<NativeList<float>>.GetOrCreate<Profile, TimesTag>();

        public static int DefineEntry(FixedString32Bytes name)
        {
            if (!Entries.Data.IsCreated)
            {
                Entries.Data = new NativeList<Entry>(Allocator.Persistent);
                s_Times.Data = new NativeList<float>(Allocator.Persistent);
            }

            Entries.Data.Add(new Entry()
            {
                Name = name
            });
            s_Times.Data.Add(0);
            return Entries.Data.Length - 1;
        }

        public static void Set(int entry, float value)
        {
            ref Entry ent = ref Entries.Data.ElementAt(entry);
            ent.Delta += value;
        }

        public static void Begin(int entry)
        {
            s_Times.Data[entry] = Time.realtimeSinceStartup;
        }

        public static void End(int entry)
        {
            Set(entry, (Time.realtimeSinceStartup - s_Times.Data[entry]) * 1000);
        }
    }
}