using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Builds the complete conveyor + Edge Impulse inspection demo at runtime and
/// wires everything in code, so the scene needs only this single component.
[DisallowMultipleComponent]
public class FactoryDemoBootstrap : MonoBehaviour
{
    [Header("Placement")]
    [Tooltip("Belt length in metres (runs left-to-right across the viewer).")]
    public float beltLength = 2.4f;
    public float beltHeight = 0.9f;
    public float distanceInFront = 2.6f;
    [Tooltip("If set, the belt is placed relative to this transform instead of the main camera.")]
    public Transform placeRelativeTo;

    [Header("Tuning")]
    public float beltSpeed = 0.35f;
    public float spawnInterval = 2.0f;
    [Range(0f, 1f)] public float defectRate = 0.5f;
    [Range(0f, 1f)] public float threshold = 0.5f;

    void Start()
    {
        Transform anchor = placeRelativeTo ? placeRelativeTo : (Camera.main ? Camera.main.transform : null);

        Vector3 center;
        Vector3 right, faceDir;
        if (anchor)
        {
            faceDir = anchor.forward; faceDir.y = 0f;
            faceDir = faceDir.sqrMagnitude > 1e-3f ? faceDir.normalized : Vector3.forward;
            right = Vector3.Cross(Vector3.up, faceDir).normalized;
            center = anchor.position + faceDir * distanceInFront;
            center.y = beltHeight;
        }
        else
        {
            faceDir = Vector3.back; right = Vector3.right;
            center = new Vector3(0f, beltHeight, distanceInFront);
        }

        var root = new GameObject("FactoryDemo").transform;
        root.position = center;

        // --- belt visual + support -------------------------------------------------
        var beltRot = Quaternion.LookRotation(-faceDir, Vector3.up);
        var belt = GameObject.CreatePrimitive(PrimitiveType.Cube);
        belt.name = "Belt";
        belt.transform.SetParent(root, false);
        belt.transform.localScale = new Vector3(beltLength, 0.06f, 0.5f);
        belt.transform.rotation = beltRot;
        var beltRenderer = belt.GetComponent<Renderer>();
        beltRenderer.material = MakeMaterial(new Color(0.12f, 0.12f, 0.14f));
        Destroy(belt.GetComponent<Collider>());

        var legs = GameObject.CreatePrimitive(PrimitiveType.Cube);
        legs.name = "Support";
        legs.transform.SetParent(root, false);
        legs.transform.localScale = new Vector3(beltLength * 0.98f, beltHeight, 0.4f);
        legs.transform.position = center + Vector3.down * beltHeight * 0.5f;
        legs.transform.rotation = beltRot;
        legs.GetComponent<Renderer>().material = MakeMaterial(new Color(0.25f, 0.27f, 0.3f));

        var spawn = new GameObject("SpawnPoint").transform;
        spawn.SetParent(root, false);
        spawn.position = center - right * (beltLength * 0.5f) + Vector3.up * 0.06f;

        var exit = new GameObject("ExitPoint").transform;
        exit.SetParent(root, false);
        exit.position = center + right * (beltLength * 0.5f) + Vector3.up * 0.06f;

        // inspection marker (a scanning gantry over the belt)
        var gantry = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gantry.name = "InspectionGantry";
        gantry.transform.SetParent(root, false);
        gantry.transform.localScale = new Vector3(0.05f, 0.5f, 0.6f);
        gantry.transform.position = Vector3.Lerp(spawn.position, exit.position, 0.55f) + Vector3.up * 0.25f;
        gantry.transform.rotation = beltRot;
        gantry.GetComponent<Renderer>().material = MakeMaterial(new Color(0.9f, 0.6f, 0.1f));
        Destroy(gantry.GetComponent<Collider>());

        // --- conveyor logic --------------------------------------------------------
        var belted = root.gameObject.AddComponent<ConveyorBelt>();
        belted.spawnPoint = spawn;
        belted.exitPoint = exit;
        belted.speed = beltSpeed;
        belted.inspectAt = 0.55f;
        belted.beltRenderer = beltRenderer;
        belted.scrollProperty = "_BaseMap";
        belted.scrollSpeed = 0.6f;

        // --- world-space UI (monitor + HUD) ---------------------------------------
        var canvasGO = new GameObject("DemoScreen");
        canvasGO.transform.SetParent(root, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        var crt = canvas.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(640, 360);
        canvasGO.transform.position = center + Vector3.up * 0.75f - faceDir * 0.05f;
        canvasGO.transform.rotation = Quaternion.LookRotation(faceDir, Vector3.up);
        canvasGO.transform.localScale = Vector3.one * 0.0016f;

        var panel = new GameObject("Panel").AddComponent<Image>();
        panel.transform.SetParent(canvasGO.transform, false);
        panel.color = new Color(0.05f, 0.06f, 0.09f, 0.85f);
        StretchFull(panel.rectTransform);

        var monitor = new GameObject("Monitor").AddComponent<RawImage>();
        monitor.transform.SetParent(canvasGO.transform, false);
        monitor.rectTransform.sizeDelta = new Vector2(300, 300);
        monitor.rectTransform.anchoredPosition = new Vector2(-150, 0);

        var title = MakeText(canvasGO.transform, "Title", new Vector2(150, 140), 30, Color.white, "Edge Impulse — Cap Inspection");
        var verdict = MakeText(canvasGO.transform, "Verdict", new Vector2(150, 40), 40, Color.white, "");
        var passed = MakeText(canvasGO.transform, "Passed", new Vector2(150, -30), 28, new Color(0.5f, 0.9f, 0.6f), "Passed: 0");
        var rejected = MakeText(canvasGO.transform, "Rejected", new Vector2(150, -80), 28, new Color(0.95f, 0.5f, 0.5f), "Rejected: 0");
        var status = MakeText(canvasGO.transform, "Status", new Vector2(150, -140), 20, new Color(0.7f, 0.75f, 0.85f), "");

        var hud = root.gameObject.AddComponent<FactoryHUD>();
        hud.passedText = passed; hud.rejectedText = rejected; hud.statusText = status;

        var inspection = root.gameObject.AddComponent<InspectionStation>();
        inspection.threshold = threshold;
        inspection.hud = hud;
        inspection.monitorImage = monitor;
        inspection.verdictText = verdict;

        // --- spawner ---------------------------------------------------------------
        var spawner = root.gameObject.AddComponent<BottleSpawner>();
        spawner.belt = belted;
        spawner.inspection = inspection;
        spawner.bottlePrefab = Resources.Load<GameObject>("Bottle");
        spawner.interval = spawnInterval;
        spawner.defectRate = defectRate;
        spawner.correctImages = LoadSamples("cap_correct");
        spawner.defectImages = LoadSamples("cap_incorrect");

        Debug.Log($"FactoryDemoBootstrap: ready. EI native = {EdgeImpulseFOMO.Available}, " +
                  $"correct imgs = {spawner.correctImages.Length}, defect imgs = {spawner.defectImages.Length}, " +
                  $"bottle prefab = {(spawner.bottlePrefab ? "loaded" : "MISSING")}");
    }

    static Texture2D[] LoadSamples(string prefix)
    {
        var all = Resources.LoadAll<Texture2D>("Samples");
        var list = new System.Collections.Generic.List<Texture2D>();
        foreach (var t in all) if (t.name.StartsWith(prefix)) list.Add(t);
        return list.ToArray();
    }

    static Material MakeMaterial(Color c)
    {
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        return m;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static TMP_Text MakeText(Transform parent, string name, Vector2 pos, float size, Color col, string text)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = col;
        t.alignment = TextAlignmentOptions.Center;
        t.rectTransform.sizeDelta = new Vector2(320, 60);
        t.rectTransform.anchoredPosition = pos;
        return t;
    }
}
