using System;
using TMPro;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Bastard
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class Profiler : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern UIntPtr emscripten_get_sbrk_ptr();

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern UIntPtr emscripten_get_heap_size();
#endif

        // [RuntimeInitializeOnLoadMethod]
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
                    var canvas = Profile.DefineEntry("Canvas");

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
                        updateDelegate = () =>
                        {
                            if (!Application.isPlaying) return;
                            canvas.Begin();
                        },
                        type = typeof(PlayerUpdateCanvasesBefore)
                    };
                    systems[UpdateCanvases + 1] = sub.subSystemList[UpdateCanvases];
                    systems[UpdateCanvases + 2] = new PlayerLoopSystem()
                    {
                        updateDelegate = () =>
                        {
                            if (!Application.isPlaying) return;
                            canvas.End();
                        },
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

        private struct Timer
        {
            public float Avg;
            public float Max;

            private float m_Sum;
            private float m_Max;

            public void Step(float value)
            {
                m_Sum += value;
                m_Max = Mathf.Max(m_Max, value);
            }

            public void Snap(int frames)
            {
                Avg = frames > 0 ? m_Sum / frames : 0;
                Max = m_Max;
                m_Sum = 0;
                m_Max = 0;
            }
        }

        private TextMeshProUGUI m_Label;
        private ProfilerRecorder m_MainRecorder;
        private ProfilerRecorder m_DrawCallRecorder;
        private Timer m_Main;
        private Timer m_DrawCall;
        private int m_Frames;
        private float m_Elapse;

        void Start()
        {
            m_Label = GetComponent<TextMeshProUGUI>();
            m_MainRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "CPU Main Thread Frame Time");
            m_DrawCallRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");

            Profile.Run();
        }

        void OnDestroy()
        {
            m_MainRecorder.Dispose();
            m_DrawCallRecorder.Dispose();
        }

        void Update()
        {
            int PadRight = 11;
            int PadLeft = 9;

            UpdateRecorderStats();

            System.Text.StringBuilder sb = new();
            sb.Append("FPS".PadRight(PadRight));
            sb.Append(Profile.FPS.ToString().PadLeft(PadLeft));

#if UNITY_WEBGL && !UNITY_EDITOR
            sb.AppendLine();
            sb.Append("Memory".PadRight(PadRight));
            unsafe
            {
                sb.Append(((*(uint *)emscripten_get_sbrk_ptr()) / 1048576 + "/" + (uint)emscripten_get_heap_size() / 1048576).PadLeft(PadLeft));
            }
#endif

            sb.AppendLine();
            sb.Append("DrawCall".PadRight(PadRight));
            sb.Append((m_DrawCall.Avg.ToString("F0") + "/" + m_DrawCall.Max.ToString("F0")).PadLeft(PadLeft));

            sb.AppendLine();
            sb.Append("Main".PadRight(PadRight));
            sb.Append((m_Main.Avg.ToString("F2") + "/" + m_Main.Max.ToString("F1")).PadLeft(PadLeft));

            ref var entries = ref Profile.Entries.Data;
            for (int i = 0; i < entries.Length; i++)
            {
                sb.AppendLine();

                ref var entry = ref entries.ElementAt(i);
                sb.Append(entry.Name.ToString().PadRight(PadRight));
                sb.Append((entry.Avg.ToString("F2") + "/" + entry.Max.ToString("F1")).PadLeft(PadLeft));
            }

            m_Label.text = sb.ToString();
        }

        private void UpdateRecorderStats()
        {
            m_Main.Step(m_MainRecorder.LastValue / 1_000_000f);
            m_DrawCall.Step(m_DrawCallRecorder.LastValue);

            m_Frames += 1;
            m_Elapse += Time.unscaledDeltaTime;
            if (m_Elapse < 1.0f)
            {
                return;
            }

            m_Main.Snap(m_Frames);
            m_DrawCall.Snap(m_Frames);
            m_Frames = 0;
            m_Elapse = 0;
        }
    }
}
