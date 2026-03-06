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
        public float Avg;
        public float Max;
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
        public static readonly SharedStatic<FixedList512Bytes<Entry>> Entries = SharedStatic<FixedList512Bytes<Entry>>.GetOrCreate<EntriesTag>();

        private struct Timer
        {
            public float Time;
            public float Sum;
        }
        private static readonly SharedStatic<FixedList128Bytes<Timer>> s_Timers = SharedStatic<FixedList128Bytes<Timer>>.GetOrCreate<Timer>();

        private class RunningTag { }
        private static readonly SharedStatic<bool> s_Running = SharedStatic<bool>.GetOrCreate<RunningTag>();

        private static readonly int s_Main;
        private static readonly int s_Render;

        static Profile()
        {
            Entries.Data = new() { new Entry() { Name = "Main" }, new Entry() { Name = "Render" } };
            s_Timers.Data = new() { default, default };
            s_Main = 0;
            s_Render = 1;
        }

        public static void Run()
        {
            if (s_Running.Data)
            {
                return;
            }

            float elapse = 0;
            int frames = 0;

            RenderPipelineManager.beginContextRendering += (context, cameras) =>
            {
                Begin(s_Render);
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
                End(s_Render);
                Delta(s_Main, mainRecorder.CurrentValue / 1000000);

                frames += 1;
                elapse += Time.unscaledDeltaTime;

                if (elapse < 1.0f)
                {
                    return;
                }

                FPS = math.round(frames / elapse);

                ref var entries = ref Entries.Data;
                for (int i = 0; i < entries.Length; i++)
                {
                    ref var timer = ref s_Timers.Data.ElementAt(i);
                    ref var entry = ref entries.ElementAt(i);
                    entry.Avg = timer.Sum / frames;
                    timer.Sum = 0;
                }

                frames = 0;
                elapse = 0;
            };

            Reset();

            s_Running.Data = true;
        }

        public static int DefineEntry(FixedString32Bytes name)
        {
            Entries.Data.Add(new Entry()
            {
                Name = name
            });
            s_Timers.Data.Add(default);
            return Entries.Data.Length - 1;
        }

        public static void Delta(int entry, float value)
        {
            s_Timers.Data.ElementAt(entry).Sum += value;
            ref var e = ref Entries.Data.ElementAt(entry);
            e.Max = math.max(value, e.Max);
        }

        public static void Begin(int entry)
        {
            JobHandle.ScheduleBatchedJobs();
            s_Timers.Data.ElementAt(entry).Time = Time.realtimeSinceStartup;
        }

        public static void End(int entry)
        {
            JobHandle.ScheduleBatchedJobs();
            Delta(entry, (Time.realtimeSinceStartup - s_Timers.Data[entry].Time) * 1000);
        }

        public static void Reset()
        {
            s_Timers.Data = new()
            {
                Length = s_Timers.Data.Length
            };
        }
    }
}