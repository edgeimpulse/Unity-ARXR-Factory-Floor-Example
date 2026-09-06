using UnityEditor;
using UnityEngine;

/// Configures the Edge Impulse native plugins for the right platforms.
/// Run headless with:
///   Unity -batchmode -quit -projectPath . -executeMethod EIPluginSetup.ConfigureAll
public static class EIPluginSetup
{
    public static void ConfigureAll()
    {
        ConfigureMac("Assets/Plugins/macOS/libei_fomo.dylib");
        ConfigureAndroid("Assets/Plugins/Android/arm64-v8a/libei_fomo.so");
        ConfigureSamples();
        AssetDatabase.SaveAssets();
        Debug.Log("EIPluginSetup: configuration complete");
    }

    // Sample cap images must be CPU-readable and uncompressed so the inference
    // path can read their raw sRGB pixels.
    static void ConfigureSamples()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/FactoryDemo/Resources/Samples" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is TextureImporter imp)
            {
                imp.isReadable = true;
                imp.mipmapEnabled = false;
                imp.sRGBTexture = true;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
                Debug.Log($"EIPluginSetup: made sample readable '{path}'");
            }
        }
    }

    static void ConfigureMac(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as PluginImporter;
        if (imp == null) { Debug.LogWarning($"EIPluginSetup: '{path}' not found or not a plugin"); return; }
        imp.SetCompatibleWithAnyPlatform(false);
        imp.SetCompatibleWithEditor(true);
        imp.SetEditorData("OS", "OSX");
        imp.SetEditorData("CPU", "ARM64");
        imp.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, true);
        imp.SetPlatformData(BuildTarget.StandaloneOSX, "CPU", "ARM64");
        imp.SaveAndReimport();
        Debug.Log($"EIPluginSetup: configured macOS plugin '{path}'");
    }

    static void ConfigureAndroid(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as PluginImporter;
        if (imp == null) { Debug.Log($"EIPluginSetup: Android plugin '{path}' not present yet (build it with native/build_android.sh)"); return; }
        imp.SetCompatibleWithAnyPlatform(false);
        imp.SetCompatibleWithEditor(false);
        imp.SetCompatibleWithPlatform(BuildTarget.Android, true);
        imp.SetPlatformData(BuildTarget.Android, "CPU", "ARM64");
        imp.SaveAndReimport();
        Debug.Log($"EIPluginSetup: configured Android plugin '{path}'");
    }
}
