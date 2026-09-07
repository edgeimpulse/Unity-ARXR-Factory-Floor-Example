using UnityEngine;

/// Scripted first-person walkthrough for video capture: walk up to the conveyor,
/// pick up a bottle with a gloved hand and hold it up to inspect it. Drives the
/// main camera (head) and the hand along an eased timeline.
[DisallowMultipleComponent]
public class PlayerWalkthrough : MonoBehaviour
{
    public Transform head;

    [Header("Path (world space)")]
    public Vector3 startPos = new Vector3(0f, 1.5f, 0.3f);
    public Vector3 standPos = new Vector3(0f, 1.5f, 1.6f);
    public Vector3 beltLook = new Vector3(0f, 1.0f, 2.55f);
    public Vector3 pickupPos = new Vector3(0.5f, 1.08f, 2.4f);
    public Vector3 detectorPos = new Vector3(0.12f, 1.08f, 2.6f);
    public Vector3 inspectLook = new Vector3(0f, 1.2f, 2.5f);
    public Vector3 detectorLook = new Vector3(0.12f, 1.18f, 2.6f);

    Transform hand;
    Transform bottle;
    Bottle bottleComp;
    Renderer scanLight;
    Material scanMat;
    Color scanBaseEmission;
    float bottleScale = 1f;
    float t;
    bool grabbed;
    bool broken;

    void Start()
    {
        if (head == null) head = Camera.main ? Camera.main.transform : transform;

        hand = new GameObject("GlovedHand").transform;
        BuildGlove(hand);

        bottle = BuildBottle(pickupPos);

        head.position = startPos;
        head.rotation = Quaternion.LookRotation(beltLook - startPos, Vector3.up);
    }

    void LateUpdate()
    {
        t += Time.deltaTime;
        TryBindScene();

        // --- head: walk in, then hold position -------------------------------
        Vector3 hp = (t < 3.0f) ? Vector3.Lerp(startPos, standPos, S(0f, 3.0f, t)) : standPos;
        if (t < 3.0f) hp.y += Mathf.Sin(t * 8f) * 0.02f;   // subtle head-bob while walking
        head.position = hp;

        // head look pans from the belt, to the inspected bottle, to the detector
        Vector3 lookTarget = (t >= 5.4f) ? detectorLook : (t >= 3.2f ? inspectLook : beltLook);
        var want = Quaternion.LookRotation((lookTarget - head.position).normalized, Vector3.up);
        head.rotation = Quaternion.Slerp(head.rotation, want, 0.12f);

        // --- bottle path: belt -> held inspect -> under the detector ----------
        Vector3 held = head.position + head.forward * 0.42f - head.up * 0.12f;
        Vector3 bottlePos = pickupPos;
        float spin = 0f;
        if (t >= 4.1f && t < 5.7f)            // lift up and inspect
        {
            bottlePos = Vector3.Lerp(pickupPos, held, S(4.1f, 5.5f, t));
            spin = (t - 4.1f) * 26f;
        }
        else if (t >= 5.7f)                   // carry down to the detector
        {
            bottlePos = Vector3.Lerp(held, detectorPos, S(5.7f, 7.2f, t));
            spin = 1.6f * 26f;
        }
        if (bottle)
        {
            bottle.position = bottlePos;
            bottle.rotation = Quaternion.Euler(0f, spin, 0f);
        }

        // --- scan light pulses while the bottle sits under the detector -------
        if (scanMat != null)
        {
            float pulse = (t >= 7.2f && t < 8.5f) ? 1f + 0.9f * Mathf.Abs(Mathf.Sin((t - 7.2f) * 6f)) : 1f;
            if (broken && t < 9.2f) pulse = 2.4f;   // bright flash on the break
            scanMat.SetColor("_EmissionColor", scanBaseEmission * pulse);
        }

        // --- the gloved hand smashes the bottle under the detector -----------
        if (!broken && t >= 8.5f && bottleComp)
        {
            bottleComp.Explode(bottleScale);
            broken = true;
        }

        // --- hand choreography ----------------------------------------------
        Vector3 rest = head.position + head.forward * 0.34f + head.right * 0.17f - head.up * 0.26f;
        Vector3 handPos;
        if (t < 3.2f) handPos = rest;                                                        // walking
        else if (t < 4.0f) handPos = Vector3.Lerp(rest, pickupPos - Vector3.up * 0.02f, S(3.2f, 4.0f, t)); // reach
        else if (t < 4.1f) { handPos = pickupPos - Vector3.up * 0.02f; grabbed = true; }      // grab
        else if (t < 7.2f) handPos = bottlePos - head.up * 0.03f - head.forward * 0.02f;      // cup the bottle
        else if (t < 8.5f)                                                                    // raise, poised to strike
            handPos = Vector3.Lerp(detectorPos + new Vector3(0f, 0.34f, -0.04f), detectorPos + new Vector3(0f, 0.12f, 0f), S(8.2f, 8.5f, t));
        else                                                                                 // smash + recoil
            handPos = Vector3.Lerp(detectorPos + new Vector3(0f, 0.10f, 0f), detectorPos + new Vector3(0.05f, 0.42f, -0.1f), S(8.5f, 9.4f, t));
        hand.position = handPos;
        hand.rotation = Quaternion.LookRotation(head.forward, Vector3.up);
    }

    // Bind the scanner light (for the pulse) and drop a collider on the belt so
    // the shattered glass has something to land on. Done lazily because the
    // bootstrap builds those objects in its own Start().
    void TryBindScene()
    {
        if (scanMat != null) return;
        var scan = GameObject.Find("ScanLight");
        var belt = GameObject.Find("Belt");
        if (scan == null || belt == null) return;
        scanLight = scan.GetComponent<Renderer>();
        if (scanLight == null) return;
        scanMat = scanLight.material;   // instance copy
        scanBaseEmission = scanMat.HasProperty("_EmissionColor") ? scanMat.GetColor("_EmissionColor") : new Color(1f, 0.15f, 0.1f) * 2.5f;
        scanMat.EnableKeyword("_EMISSION");

        var b = belt.GetComponent<Renderer>().bounds;
        var catcher = new GameObject("ShardCatcher");
        catcher.AddComponent<BoxCollider>();
        catcher.transform.position = new Vector3(b.center.x, b.max.y - 0.01f, b.center.z);
        catcher.transform.localScale = new Vector3(b.size.x, 0.02f, b.size.z);
    }

    // -- construction ---------------------------------------------------------
    Transform BuildBottle(Vector3 pos)
    {
        var prefab = Resources.Load<GameObject>("Bottle");
        GameObject go = prefab ? Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "HeldBottle";
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity;
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;
        foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false;
        bottleScale = NormalizeHeight(go, 0.30f);
        bottleComp = go.GetComponentInChildren<Bottle>();
        return go.transform;
    }

    static void BuildGlove(Transform root)
    {
        var glove = Mat(new Color(0.55f, 0.32f, 0.12f));
        var cuff = Mat(new Color(0.14f, 0.15f, 0.18f));
        Prim(root, "Palm", new Vector3(0.09f, 0.035f, 0.11f), Vector3.zero, Vector3.zero, glove);
        Prim(root, "Fingers", new Vector3(0.085f, 0.07f, 0.028f), new Vector3(0f, 0.05f, -0.035f), new Vector3(35f, 0f, 0f), glove);
        Prim(root, "Thumb", new Vector3(0.022f, 0.055f, 0.03f), new Vector3(0.052f, 0.03f, -0.01f), new Vector3(20f, 0f, -22f), glove);
        Prim(root, "Cuff", new Vector3(0.10f, 0.06f, 0.05f), new Vector3(0f, -0.015f, -0.09f), Vector3.zero, cuff);
    }

    static void Prim(Transform parent, string name, Vector3 scale, Vector3 localPos, Vector3 euler, Material m)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localScale = scale;
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.GetComponent<Renderer>().sharedMaterial = m;
        Object.Destroy(go.GetComponent<Collider>());
    }

    static Material Mat(Color c)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        return m;
    }

    static float NormalizeHeight(GameObject go, float target)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return 1f;
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        if (b.size.y <= 1e-4f) return 1f;
        float f = target / b.size.y;
        go.transform.localScale *= f;
        return f;
    }

    static float S(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));
}
