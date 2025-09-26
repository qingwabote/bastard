using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Bastard
{
    public class Profiler : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            // var system = PlayerLoop.GetCurrentPlayerLoop();
            // for (int i = 0; i < system.subSystemList.Length; i++)
            // {
            //     ref var sub = ref system.subSystemList[i];
            //     if (sub.type == typeof(UnityEngine.PlayerLoop.Initialization))
            //     {
            //         var systems = new PlayerLoopSystem[1 + sub.subSystemList.Length];
            //         systems[0] = new PlayerLoopSystem()
            //         {
            //             updateDelegate = FrameStartHandle,
            //             type = typeof(FrameStart)
            //         };
            //         Array.Copy(sub.subSystemList, 0, systems, 1, sub.subSystemList.Length);
            //         sub.subSystemList = systems;
            //         break;
            //     }
            // }
            // PlayerLoop.SetPlayerLoop(system);

            Profile.Initialize();
        }

        // private struct FrameStart { }
        // private static float s_FrameStartTime;
        // private static void FrameStartHandle()
        // {
        //     s_FrameStartTime = Time.realtimeSinceStartup;
        // }

        private TextMeshProUGUI m_Label;

        private string m_Text;

        void Start()
        {
            m_Label = GetComponent<TextMeshProUGUI>();

            float elapse = 0;
            uint frames = 1;

            int render_entry = Profile.DefineEntry("Render");
            float render_time = 0;
            RenderPipelineManager.beginContextRendering += (context, cameras) =>
            {
                render_time = Time.realtimeSinceStartup;
            };
            RenderPipelineManager.endContextRendering += (context, cameras) =>
            {
                Profile.Delta(render_entry, (Time.realtimeSinceStartup - render_time) * 1000);

                if (elapse < 1.0f)
                {
                    frames++;
                    elapse += Time.unscaledDeltaTime;
                    return;
                }

                int PadRight = 12;
                int PadLeft = 8;

                string name = "fps".PadRight(PadRight);
                string text = $"{name} {math.round(frames / elapse).ToString().PadLeft(PadLeft)}";

                ref var entries = ref Profile.Entries.Data;
                for (int i = 0; i < entries.Length; i++)
                {
                    ref var entry = ref entries.ElementAt(i);
                    name = entry.Name.ToString().PadRight(PadRight);

                    string average = (entry.Delta / frames).ToString("F3").PadLeft(PadLeft);
                    text += $"\n{name} {average}";
                    entry.Delta = 0;
                }

                m_Text = text;

                frames = 1;
                elapse = Time.unscaledDeltaTime;
            };
        }

        void Update()
        {
            m_Label.text = m_Text;
        }
    }
}
