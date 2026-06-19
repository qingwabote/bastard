using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
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

    /* A fixed size profile struct supports burst compile time evaluation */
    public struct Profile
    {
        /* A readonly handle can easily use in burst as static readonly field */
        public readonly struct Handle
        {
            public readonly ref struct Scope
            {
                private readonly Handle m_Hanlde;

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

            internal Handle(int entry)
            {
                Entry = entry;
            }

            public Scope Auto()
            {
                return new(this);
            }

            public void Begin()
            {
                Profile.Begin(Entry);
            }

            public void End()
            {
                Profile.End(Entry);
            }

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
            public double Now;
            public float Sum;
            public float Max;
        }
        private static readonly SharedStatic<FixedList512Bytes<Timer>> s_Timers = SharedStatic<FixedList512Bytes<Timer>>.GetOrCreate<Timer>();

        private class RunningTag { }
        private static readonly SharedStatic<bool> s_Running = SharedStatic<bool>.GetOrCreate<RunningTag>();

        private static readonly Handle s_Render;

        static Profile()
        {
            Entries.Data = new() { new Entry() { Name = "Render" } };
            s_Timers.Data = new() { default };
            s_Render = new(0);
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
            RenderPipelineManager.endContextRendering += (context, cameras) =>
            {
                s_Render.End();

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
                    entry.Max = timer.Max;
                    timer = default;
                }

                frames = 0;
                elapse = 0;
            };

            Reset();

            s_Running.Data = true;
        }

        public static Handle DefineEntry(FixedString32Bytes name)
        {
            if (Entries.Data.Length >= Entries.Data.Capacity)
            {
                return default;
            }

            Entries.Data.Add(new Entry()
            {
                Name = name
            });
            s_Timers.Data.Add(default);
            return new(Entries.Data.Length - 1);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern double emscripten_get_now();

        private static void Begin(int entry)
        {
            s_Timers.Data.ElementAt(entry).Now = emscripten_get_now();
        }

        private static void End(int entry)
        {
            Delta(entry, (float)(emscripten_get_now() - s_Timers.Data.ElementAt(entry).Now));
        }
#else
        private static void Begin(int entry)
        {
            if (!s_Running.Data)
            {
                return;
            }

            JobHandle.ScheduleBatchedJobs();
            s_Timers.Data.ElementAt(entry).Now = Time.realtimeSinceStartupAsDouble;
        }

        private static void End(int entry)
        {
            if (!s_Running.Data)
            {
                return;
            }

            JobHandle.ScheduleBatchedJobs();
            Delta(entry, (float)(Time.realtimeSinceStartupAsDouble - s_Timers.Data.ElementAt(entry).Now) * 1000);
        }
#endif

        private static void Delta(int entry, float value)
        {
            if (!s_Running.Data)
            {
                return;
            }

            ref var timer = ref s_Timers.Data.ElementAt(entry);
            timer.Sum += value;
            timer.Max = math.max(value, timer.Max);
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
