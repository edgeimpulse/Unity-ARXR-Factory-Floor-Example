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
    [Tooltip("When true (and no placeRelativeTo) the belt is placed in front of the main camera; when false it uses a fixed world position.")]
    public bool placeInFrontOfCamera = true;

    [Header("Tuning")]
    public float beltSpeed = 0.7f;
    public float spawnInterval = 1.0f;
    [Range(0f, 1f)] public float defectRate = 0.5f;
    [Range(0f, 1f)] public float threshold = 0.5f;

    void Start()
    {
        Transform anchor = placeRelativeTo ? placeRelativeTo : (placeInFrontOfCamera && Camera.main ? Camera.main.transform : null);

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
        var beltMat = MakeMaterial(new Color(0.16f, 0.17f, 0.2f));
        var stripes = MakeStripeTexture();
        if (beltMat.HasProperty("_BaseMap")) beltMat.SetTexture("_BaseMap", stripes);
        beltMat.mainTexture = stripes;
        beltMat.mainTextureScale = new Vector2(Mathf.Max(2f, Mathf.Round(beltLength * 6f)), 2f);
        beltRenderer.material = beltMat;
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

        // inspection gantry: two posts + a top beam with a red scan light
        Vector3 insCenter = Vector3.Lerp(spawn.position, exit.position, 0.55f);
        var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beam.name = "ScannerBeam"; beam.transform.SetParent(root, false);
        beam.transform.localScale = new Vector3(0.08f, 0.08f, 0.72f);
        beam.transform.rotation = beltRot;
        beam.transform.position = insCenter + Vector3.up * 0.44f;
        beam.GetComponent<Renderer>().material = MakeMaterial(new Color(0.82f, 0.85f, 0.9f));
        Destroy(beam.GetComponent<Collider>());
        for (int s = -1; s <= 1; s += 2)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "ScannerPost"; post.transform.SetParent(root, false);
            post.transform.localScale = new Vector3(0.06f, 0.46f, 0.06f);
            post.transform.rotation = beltRot;
            post.transform.position = insCenter + Vector3.up * 0.22f + beltRot * new Vector3(0f, 0f, s * 0.33f);
            post.GetComponent<Renderer>().material = MakeMaterial(new Color(0.3f, 0.32f, 0.36f));
            Destroy(post.GetComponent<Collider>());
        }
        var scan = GameObject.CreatePrimitive(PrimitiveType.Cube);
        scan.name = "ScanLight"; scan.transform.SetParent(root, false);
        scan.transform.localScale = new Vector3(0.05f, 0.02f, 0.66f);
        scan.transform.rotation = beltRot;
        scan.transform.position = insCenter + Vector3.up * 0.38f;
        var scanMat = MakeMaterial(new Color(1f, 0.25f, 0.2f));
        if (scanMat.HasProperty("_EmissionColor")) { scanMat.EnableKeyword("_EMISSION"); scanMat.SetColor("_EmissionColor", new Color(1f, 0.15f, 0.1f) * 2.5f); }
        scan.GetComponent<Renderer>().material = scanMat;
        Destroy(scan.GetComponent<Collider>());

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
        var camForCanvas = Camera.main ? Camera.main.transform.position : center - faceDir * 3f;
        canvasGO.transform.rotation = Quaternion.LookRotation((canvasGO.transform.position - camForCanvas).normalized, Vector3.up);
        canvasGO.transform.localScale = Vector3.one * 0.0016f;

        var panel = new GameObject("Panel").AddComponent<Image>();
        panel.transform.SetParent(canvasGO.transform, false);
        panel.color = new Color(0.05f, 0.06f, 0.09f, 0.85f);
        StretchFull(panel.rectTransform);

        var monitor = new GameObject("Monitor").AddComponent<RawImage>();
        monitor.transform.SetParent(canvasGO.transform, false);
        monitor.color = Color.white;
        monitor.texture = MakeSolidTexture(new Color(0.1f, 0.11f, 0.14f));
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

    static Texture2D MakeStripeTexture()
    {
        const int n = 16;
        var t = new Texture2D(n, 2);
        var a = new Color(0.13f, 0.14f, 0.17f);
        var b = new Color(0.22f, 0.23f, 0.27f);
        for (int x = 0; x < n; x++)
        {
            var c = (x % 4 < 2) ? a : b;
            t.SetPixel(x, 0, c); t.SetPixel(x, 1, c);
        }
        t.wrapMode = TextureWrapMode.Repeat;
        t.filterMode = FilterMode.Point;
        t.Apply();
        return t;
    }

    static Texture2D MakeSolidTexture(Color c)
    {
        var t = new Texture2D(4, 4);
        var px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = c;
        t.SetPixels(px);
        t.Apply();
        return t;
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
