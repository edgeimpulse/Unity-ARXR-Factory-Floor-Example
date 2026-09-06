using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// Builds the Android APK from the command line.
/// Run: Unity -batchmode -quit -projectPath . -executeMethod FactoryBuild.BuildAndroid
public static class FactoryBuild
{
    public static void BuildAndroid()
    {
        ConfigureAndroidTools();

        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.edgeimpulse.factoryfloorxr");
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/FactoryFloorDemo.unity" };

        const string outDir = "build/Android";
        System.IO.Directory.CreateDirectory(outDir);
        string apk = System.IO.Path.Combine(outDir, "FactoryFloorXR.apk");

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = apk,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None,
        };

        var report = BuildPipeline.BuildPlayer(options);
        var summary = report.summary;
        Debug.Log($"FactoryBuild: result={summary.result}, size={summary.totalSize} bytes, output={summary.outputPath}");
        if (summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"FactoryBuild: APK build FAILED with {summary.totalErrors} error(s).");
            EditorApplication.Exit(1);
        }
    }

    // Builds a macOS player of the capture scene (Mono, fast) so its DemoRecorder
    // can render the simulation to PNG frames for the gifs.
    public static void BuildMacCapture()
    {
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.runInBackground = true;
        try { UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.ARM64; }
        catch (System.Exception e) { Debug.LogWarning("FactoryBuild: could not set mac arch: " + e.Message); }
        Directory.CreateDirectory("build/Mac");
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/FactoryFloorCapture.unity" },
            locationPathName = "build/Mac/FactoryFloorCapture.app",
            target = BuildTarget.StandaloneOSX,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"FactoryBuild(Mac): result={report.summary.result}, output={report.summary.outputPath}");
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("FactoryBuild: macOS capture build FAILED.");
            EditorApplication.Exit(1);
        }
    }

    // Point Unity at the Android Studio SDK/NDK (via EditorPrefs, which does not
    // strict-validate cmdline-tools) and use Unity's embedded JDK, so the build
    // works in batchmode where these paths are not pre-configured.
    static void ConfigureAndroidTools()
    {
        string home = System.Environment.GetEnvironmentVariable("HOME");
        string sdk = Path.Combine(home, "Library/Android/sdk");

        if (Directory.Exists(sdk))
        {
            EditorPrefs.SetBool("SdkUseEmbedded", false);
            EditorPrefs.SetString("AndroidSdkRoot", sdk);
        }
        EditorPrefs.SetBool("JdkUseEmbedded", true);

        // Unity 6000.0.32 requires NDK r23b (23.1.7779620). Try installed versions
        // (preferring r23b) until the validating setter accepts one.
        string ndkBase = Path.Combine(sdk, "ndk");
        var candidates = Directory.Exists(ndkBase)
            ? Directory.GetDirectories(ndkBase)
                .OrderBy(d => Path.GetFileName(d).StartsWith("23.1.7779620") ? 0
                            : Path.GetFileName(d).StartsWith("23.") ? 1 : 2)
            : Enumerable.Empty<string>();

        bool ndkOk = false;
        foreach (var c in candidates)
        {
            try
            {
                AndroidExternalToolsSettings.ndkRootPath = c;
                EditorPrefs.SetString("AndroidNdkRootR27", c);
                Debug.Log($"FactoryBuild: NDK accepted -> {c}");
                ndkOk = true;
                break;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"FactoryBuild: NDK '{Path.GetFileName(c)}' rejected: {e.Message}");
            }
        }
        if (!ndkOk) Debug.LogError("FactoryBuild: no compatible NDK found under " + ndkBase);
        Debug.Log($"FactoryBuild: Android tools -> SDK='{sdk}' (JDK=embedded)");
    }
}
