using TMPro;
using UnityEngine;

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
        }

        // private struct FrameStart { }
        // private static float s_FrameStartTime;
        // private static void FrameStartHandle()
        // {
        //     s_FrameStartTime = Time.realtimeSinceStartup;
        // }

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
