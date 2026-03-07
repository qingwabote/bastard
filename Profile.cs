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
        public struct Handle
        {
            public ref struct Scope
            {
                private Handle m_Hanlde;

                internal Scope(Handle hanlde)
                {
                    m_Hanlde = hanlde;
                    m_Hanlde.Begin();
                }

                public void Dispose()
                {
                    m_Hanlde.End();
                }
            }

            public readonly int Entry;

            private double m_Time;

            internal Handle(int entry)
            {
                Entry = entry;
                m_Time = 0;
            }

            public Scope MakeScope()
            {
                return new(this);
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            [System.Runtime.InteropServices.DllImport("__Internal")]
            private static extern double emscripten_get_now();

            public void Begin()
            {
                m_Time = emscripten_get_now(); 
            }

            public void End()
            {
                Profile.Delta(Entry, (float)(emscripten_get_now() - m_Time));
            }
#else
            public void Begin()
            {
                JobHandle.ScheduleBatchedJobs();
                m_Time = Time.realtimeSinceStartupAsDouble;
            }

            public void End()
            {
                JobHandle.ScheduleBatchedJobs();
                Profile.Delta(Entry, (float)(Time.realtimeSinceStartupAsDouble - m_Time) * 1000);
            }
#endif

            public void Delta(float value)
            {
                Profile.Delta(Entry, value);
            }
        }

        public static float FPS { get; private set; }

        private class EntriesTag { }
        public static readonly SharedStatic<FixedList512Bytes<Entry>> Entries = SharedStatic<FixedList512Bytes<Entry>>.GetOrCreate<EntriesTag>();

        private struct Timer
        {
            public float Sum;
        }
        private static readonly SharedStatic<FixedList64Bytes<Timer>> s_Timers = SharedStatic<FixedList64Bytes<Timer>>.GetOrCreate<Timer>();

        private class RunningTag { }
        private static readonly SharedStatic<bool> s_Running = SharedStatic<bool>.GetOrCreate<RunningTag>();

        private static Handle s_Main;
        private static Handle s_Render;

        static Profile()
        {
            Entries.Data = new() { new Entry() { Name = "Main" }, new Entry() { Name = "Render" } };
            s_Timers.Data = new() { default, default };
            s_Main = new(0);
            s_Render = new(1);
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
                s_Render.Begin();
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
                s_Render.End();
                s_Main.Delta(mainRecorder.CurrentValue / 1000000);

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

        public static Handle DefineEntry(FixedString32Bytes name)
        {
            Entries.Data.Add(new Entry()
            {
                Name = name
            });
            s_Timers.Data.Add(default);
            return new(Entries.Data.Length - 1);
        }

        private static void Delta(int entry, float value)
        {
            s_Timers.Data.ElementAt(entry).Sum += value;
            ref var e = ref Entries.Data.ElementAt(entry);
            e.Max = math.max(value, e.Max);
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