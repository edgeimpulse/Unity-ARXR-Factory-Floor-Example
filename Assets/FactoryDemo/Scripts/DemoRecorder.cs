using System.IO;
using UnityEngine;

/// Records the main camera to PNG frames during play, then quits. Used to
/// generate the simulation gifs without any screen-recording permissions.
/// Enabled only in the dedicated capture scene.
public class DemoRecorder : MonoBehaviour
{
    public int width = 960;
    public int height = 540;
    public int frames = 270;      // at captureFramerate 30 => 9 seconds
    public int captureFramerate = 30;
    public int randomSeed = 4242;
    public string outputDir = "";

    Camera cam;
    RenderTexture rt;
    Texture2D tex;
    int count;

    void Start()
    {
        Random.InitState(randomSeed);
        Time.captureFramerate = captureFramerate;
        Application.runInBackground = true; // render at full speed even when unfocused

        cam = Camera.main;
        rt = new RenderTexture(width, height, 24);
        rt.Create();
        if (cam) cam.targetTexture = rt;
        tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        if (string.IsNullOrEmpty(outputDir))
            outputDir = Path.Combine(Application.persistentDataPath, "frames");
        Directory.CreateDirectory(outputDir);
        Debug.Log($"DemoRecorder: writing frames to {outputDir}");
    }

    void LateUpdate()
    {
        if (!cam) return;
        if (count >= frames)
        {
            Debug.Log($"DemoRecorder: captured {count} frames to {outputDir}");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
            return;
        }

        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(Path.Combine(outputDir, $"frame_{count:0000}.png"), tex.EncodeToPNG());
        count++;
    }
}
