using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// Drives the gif/video capture by entering Play mode in batch mode (with a
/// graphics device, no window) so the DemoRecorder renders the simulation to
/// PNG frames at full speed. Exits the editor when the recording finishes.
///
/// Run: Unity -batchmode -projectPath . -executeMethod DemoFilm.Capture
/// (note: NO -nographics and NO -quit)
public static class DemoFilm
{
    const string CapturePath = "Assets/Scenes/FactoryFloorCapture.unity";
    static double startTime;

    public static void Capture()
    {
        AssetDatabase.Refresh();
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += SafetyTimeout;
        startTime = EditorApplication.timeSinceStartup;
        EditorSceneManager.OpenScene(CapturePath, OpenSceneMode.Single);
        Debug.Log("DemoFilm: entering play mode to capture frames...");
        EditorApplication.EnterPlaymode();
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // The DemoRecorder stops play mode when it has captured every frame.
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            Debug.Log("DemoFilm: capture finished, exiting.");
            EditorApplication.Exit(0);
        }
    }

    static void SafetyTimeout()
    {
        if (EditorApplication.timeSinceStartup - startTime > 240.0)
        {
            Debug.LogError("DemoFilm: safety timeout reached, exiting.");
            EditorApplication.Exit(1);
        }
    }
}
