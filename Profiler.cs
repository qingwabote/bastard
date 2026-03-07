using System;
using TMPro;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Bastard
{
    [RequireComponent(typeof(TextMeshProUGUI))]
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

        private TextMeshProUGUI m_Label;

        void Start()
        {
            m_Label = GetComponent<TextMeshProUGUI>();

            Profile.Run();
        }

        void Update()
        {
            int PadRight = 9;
            int PadLeft = 16;

            string name = "FPS".PadRight(PadRight);
            string text = $"{name} {Profile.FPS.ToString().PadLeft(PadLeft)}";

            ref var entries = ref Profile.Entries.Data;
            for (int i = 0; i < entries.Length; i++)
            {
                ref var entry = ref entries.ElementAt(i);
                name = entry.Name.ToString().PadRight(PadRight);

                string value = entry.Avg.ToString("F3") + "/" + entry.Max.ToString("F3");
                text += $"\n{name} {value.PadLeft(PadLeft)}";
            }

            m_Label.text = text;
        }
    }
}
