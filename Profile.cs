using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bastard
{
    public struct Entry
    {
        public FixedString32Bytes Name;
        public float Delta;
        public float Average;
    }

    struct ProfileManaged
    {
        public static List<Entry> Entries = new() { default };

        public static readonly int Main = Profile.DefineEntry("Main");
        public static readonly int Render = Profile.DefineEntry("Render");
    }

    public struct Profile
    {
        public readonly ref struct Scope
        {
            private readonly int m_Entry;

            public Scope(int entry)
            {
                m_Entry = entry;
                Begin(entry);
            }

            public void Dispose()
            {
                End(m_Entry);
            }
        }

        public static float FPS { get; private set; }

        private class EntriesTag { }
        public static readonly SharedStatic<NativeList<Entry>> Entries = SharedStatic<NativeList<Entry>>.GetOrCreate<Profile, EntriesTag>();

        private class TimesTag { }
        private static readonly SharedStatic<NativeList<float>> s_Times = SharedStatic<NativeList<float>>.GetOrCreate<Profile, TimesTag>();

        private class RunningTag { }
        private static readonly SharedStatic<bool> s_Running = SharedStatic<bool>.GetOrCreate<Profile, RunningTag>();

        public static void Run()
        {
            if (s_Running.Data)
            {
                return;
            }

            float elapse = 0;
            uint frames = 1;

            RenderPipelineManager.beginContextRendering += (context, cameras) =>
            {
                Begin(ProfileManaged.Render);
            };
            // List<ProfilerRecorderHandle> list = new();
            // ProfilerRecorderHandle.GetAvailable(list);
            // foreach (var item in list)
            // {
            //     Debug.Log($"ProfilerRecorderHandle {ProfilerRecorderHandle.GetDescription(item).Name}");
            // }
            var mainRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Main Thread Frame Time");
            RenderPipelineManager.endContextRendering += (context, cameras) =>
            {
                End(ProfileManaged.Render);
                Delta(ProfileManaged.Main, mainRecorder.CurrentValue / 1000000);

                if (elapse < 1.0f)
                {
                    frames++;
                    elapse += Time.unscaledDeltaTime;
                    return;
                }

                FPS = math.round(frames / elapse);

                ref var entries = ref Entries.Data;
                for (int i = 1; i < entries.Length; i++)
                {
                    ref var entry = ref entries.ElementAt(i);
                    entry.Average = entry.Delta / frames;
                    entry.Delta = 0;
                }

                frames = 1;
                elapse = Time.unscaledDeltaTime;
            };

            Entries.Data = new NativeList<Entry>(Allocator.Persistent);
            s_Times.Data = new NativeList<float>(Allocator.Persistent);
            foreach (var entry in ProfileManaged.Entries)
            {
                Entries.Data.Add(entry);
                s_Times.Data.Add(0);
            }
            ProfileManaged.Entries = null;
            s_Running.Data = true;
        }

        [BurstDiscard]
        private static void DefineEntryManaged(FixedString32Bytes name, out int ID)
        {
            ProfileManaged.Entries.Add(new Entry()
            {
                Name = name
            });
            ID = ProfileManaged.Entries.Count - 1;
        }

        public static int DefineEntry(FixedString32Bytes name)
        {
            if (!s_Running.Data)
            {
                DefineEntryManaged(name, out int ID);
                return ID;
            }

            Entries.Data.Add(new Entry()
            {
                Name = name
            });
            s_Times.Data.Add(0);
            return Entries.Data.Length - 1;
        }

        public static void Delta(int entry, float value)
        {
            if (!s_Running.Data) return;

            ref Entry ent = ref Entries.Data.ElementAt(entry);
            ent.Delta += value;
        }

        public static void Begin(int entry)
        {
            if (!s_Running.Data) return;

            JobHandle.ScheduleBatchedJobs();
            s_Times.Data[entry] = Time.realtimeSinceStartup;
        }

        public static void End(int entry)
        {
            if (!s_Running.Data) return;

            JobHandle.ScheduleBatchedJobs();
            Delta(entry, (Time.realtimeSinceStartup - s_Times.Data[entry]) * 1000);
        }
    }
}