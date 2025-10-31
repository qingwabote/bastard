using System;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Bastard
{
    public class Profiler : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            for (int index = 0; index < loop.subSystemList.Length; index++)
            {
                ref var sub = ref loop.subSystemList[index];
                if (sub.type == typeof(UnityEngine.PlayerLoop.PostLateUpdate))
                {
                    // for (int i = 0; i < sub.subSystemList.Length; i++)
                    // {
                    //     ref var s = ref sub.subSystemList[i];
                    //     Debug.Log($"PostLateUpdate {s.type.FullName}");
                    // }
                    int canvas = Profile.DefineEntry("Canvas");

                    int UpdateCanvases = 0;
                    for (; UpdateCanvases < sub.subSystemList.Length; UpdateCanvases++)
                    {
                        ref var s = ref sub.subSystemList[UpdateCanvases];
                        if (s.type == typeof(UnityEngine.PlayerLoop.PostLateUpdate.PlayerUpdateCanvases))
                        {
                            break;
                        }
                    }
                    var systems = new PlayerLoopSystem[sub.subSystemList.Length + 2];
                    Array.Copy(sub.subSystemList, 0, systems, 0, UpdateCanvases);
                    systems[UpdateCanvases] = new PlayerLoopSystem()
                    {
                        updateDelegate = () => { Profile.Begin(canvas); },
                        type = typeof(PlayerUpdateCanvasesBefore)
                    };
                    systems[UpdateCanvases + 1] = sub.subSystemList[UpdateCanvases];
                    systems[UpdateCanvases + 2] = new PlayerLoopSystem()
                    {
                        updateDelegate = () => { Profile.End(canvas); },
                        type = typeof(PlayerUpdateCanvasesAfter)
                    };
                    Array.Copy(sub.subSystemList, UpdateCanvases + 1, systems, UpdateCanvases + 3, sub.subSystemList.Length - UpdateCanvases - 1);
                    sub.subSystemList = systems;
                    break;
                }
            }
            PlayerLoop.SetPlayerLoop(loop);
        }

        private struct PlayerUpdateCanvasesBefore { }
        private struct PlayerUpdateCanvasesAfter { }

        static double GetRecorderFrameAverage(ProfilerRecorder recorder)
        {
            var samplesCount = recorder.Capacity;
            if (samplesCount == 0)
                return 0;

            double r = 0;
            unsafe
            {
                var samples = stackalloc ProfilerRecorderSample[samplesCount];
                recorder.CopyTo(samples, samplesCount);
                for (var i = 0; i < samplesCount; ++i)
                    r += samples[i].Value;
                r /= samplesCount;
            }

            return r;
        }

        private TextMeshProUGUI m_Label;

        void Start()
        {
            Profile.Run();

            m_Label = GetComponent<TextMeshProUGUI>();
        }

        void Update()
        {
            int PadRight = 12;
            int PadLeft = 8;

            string name = "FPS".PadRight(PadRight);
            string text = $"{name} {Profile.FPS.ToString().PadLeft(PadLeft)}";

            ref var entries = ref Profile.Entries.Data;
            for (int i = 1; i < entries.Length; i++)
            {
                ref var entry = ref entries.ElementAt(i);
                name = entry.Name.ToString().PadRight(PadRight);

                string average = entry.Average.ToString("F3").PadLeft(PadLeft);
                text += $"\n{name} {average}";
            }

            m_Label.text = text;
        }
    }
}
