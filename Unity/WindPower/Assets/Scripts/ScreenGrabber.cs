#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public class ScreenshotGrabber
{
    [MenuItem("Screenshot/Grab")]
    public static void Grab()
    {
        ScreenCapture.CaptureScreenshot("Assets\\Screenshot.png", 1);
        // var asd = ScreenCapture.CaptureScreenshotAsTexture();
        // byte[] bytes = asd.EncodeToPNG();
        // File.WriteAllBytes("Assets\\Screenshot2.png", bytes);
    }
}
#endif
