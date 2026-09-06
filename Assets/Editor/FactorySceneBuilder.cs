using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Creates a self-contained demo scene (camera, light, floor + the runtime
/// bootstrap) and registers it as the build scene.
/// Run: Unity -batchmode -quit -projectPath . -executeMethod FactorySceneBuilder.Build
public static class FactorySceneBuilder
{
    const string ScenePath = "Assets/Scenes/FactoryFloorDemo.unity";
    const string CapturePath = "Assets/Scenes/FactoryFloorCapture.unity";
    const string WalkthroughPath = "Assets/Scenes/FactoryFloorWalkthrough.unity";

    public static void Build()
    {
        AssetDatabase.Refresh();
        ValidateRuntime();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 50f;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
        camGO.AddComponent<AudioListener>();
        camGO.transform.position = new Vector3(1.7f, 1.6f, -0.5f);
        camGO.transform.rotation = Quaternion.LookRotation((new Vector3(0f, 0.85f, 2.5f) - camGO.transform.position).normalized, Vector3.up);

        // Light
        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.34f, 0.36f, 0.42f);

        // Floor
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(3f, 1f, 3f);
        var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        if (floorMat.HasProperty("_BaseColor")) floorMat.SetColor("_BaseColor", new Color(0.18f, 0.19f, 0.22f));
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        // Bootstrap
        var demoGO = new GameObject("FactoryDemo Bootstrap");
        var boot = demoGO.AddComponent<FactoryDemoBootstrap>();
        boot.placeRelativeTo = null;
        boot.placeInFrontOfCamera = false; // fixed belt position; camera views it at an angle

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        Debug.Log($"FactorySceneBuilder: created {ScenePath} and set it as the build scene.");
    }

    // Duplicates the demo scene and adds a DemoRecorder that writes frames to
    // /tmp/factoryframes, then makes it the build scene (for the gif capture).
    public static void BuildCaptureScene()
    {
        Build();
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var boot = Object.FindFirstObjectByType<FactoryDemoBootstrap>();
        if (boot == null) { Debug.LogError("FactorySceneBuilder: bootstrap not found; run Build first."); return; }
        var rec = boot.gameObject.AddComponent<DemoRecorder>();
        rec.outputDir = "/tmp/factoryframes";
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, CapturePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(CapturePath, true) };
        Debug.Log($"FactorySceneBuilder: created {CapturePath} (with recorder) and set it as the build scene.");
    }

    // First-person "walk up and pick up a bottle" scene for the walkthrough video.
    public static void BuildWalkthroughScene()
    {
        AssetDatabase.Refresh();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGO = new GameObject("Head");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.fieldOfView = 62f;
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
        camGO.AddComponent<AudioListener>();
        camGO.transform.position = new Vector3(0f, 1.5f, 0.3f);

        var lightGO = new GameObject("Directional Light");
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.34f, 0.36f, 0.42f);

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.localScale = new Vector3(3f, 1f, 3f);
        var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        if (floorMat.HasProperty("_BaseColor")) floorMat.SetColor("_BaseColor", new Color(0.18f, 0.19f, 0.22f));
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        var demoGO = new GameObject("FactoryDemo Bootstrap");
        var boot = demoGO.AddComponent<FactoryDemoBootstrap>();
        boot.placeRelativeTo = null;
        boot.placeInFrontOfCamera = false;

        var dir = new GameObject("Director");
        var wt = dir.AddComponent<PlayerWalkthrough>();
        wt.head = camGO.transform;
        var rec = dir.AddComponent<DemoRecorder>();
        rec.frames = 360;
        rec.outputDir = "/tmp/factoryframes";

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, WalkthroughPath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(WalkthroughPath, true) };
        Debug.Log($"FactorySceneBuilder: created {WalkthroughPath} and set it as the build scene.");
    }

    // Construct the demo in a throwaway scene and run Start() to catch runtime
    // errors and confirm the Resources (bottle prefab + sample images) load.
    static void ValidateRuntime()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGO = new GameObject("VCam");
        camGO.tag = "MainCamera";
        camGO.AddComponent<Camera>();
        camGO.transform.position = new Vector3(0f, 1.4f, -0.2f);

        var go = new GameObject("VBoot");
        var boot = go.AddComponent<FactoryDemoBootstrap>();
        boot.placeRelativeTo = camGO.transform;
        go.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
        Debug.Log("FactorySceneBuilder: runtime validation constructed the demo without errors.");
    }
}

