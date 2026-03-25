namespace Bastard
{
    using UnityEditor;
    using UnityEngine;

    class Screenshot
    {
        [MenuItem("Screenshot/Capture")]
        static void Capture()
        {
            ScreenCapture.CaptureScreenshot("Screenshot.png", 1);
        }
    }
}