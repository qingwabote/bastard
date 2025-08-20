using Bastard;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Budget
{
    public class Profiler : MonoBehaviour
    {
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
                Profile.Set(render_entry, (Time.realtimeSinceStartup - render_time) * 1000);

                if (elapse < 1.0f)
                {
                    frames++;
                    elapse += Time.unscaledDeltaTime;
                    return;
                }

                int PadRight = 12;
                int PadLeft = 8;

                string name = "FPS".PadRight(PadRight);
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
