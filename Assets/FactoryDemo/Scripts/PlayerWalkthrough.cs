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
    public Vector3 standPos = new Vector3(0f, 1.5f, 1.55f);
    public Vector3 beltLook = new Vector3(0f, 1.0f, 2.55f);
    public Vector3 pickupPos = new Vector3(0f, 0.98f, 2.4f);

    Transform hand;
    Transform bottle;
    float t;
    bool grabbed;

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

        // --- head position: walk in, then a slow strafe while inspecting --------
        Vector3 hp = (t < 3.2f) ? Vector3.Lerp(startPos, standPos, S(0f, 3.2f, t)) : standPos;
        if (t > 6.2f) hp += new Vector3(Mathf.Sin((t - 6.2f) * 0.7f) * 0.55f, 0f, 0f);
        if (t < 3.2f) hp.y += Mathf.Sin(t * 8f) * 0.02f; // subtle head-bob while walking
        head.position = hp;

        // head looks at a stable point (never at the hand, to avoid feedback)
        Vector3 lookTarget = (t < 3.4f) ? beltLook : new Vector3(head.position.x, 1.16f, 2.85f);
        var want = Quaternion.LookRotation((lookTarget - head.position).normalized, Vector3.up);
        head.rotation = Quaternion.Slerp(head.rotation, want, 0.14f);

        // bottle: on the belt, then lifted to a held pose in front of the camera
        Vector3 bottleHold = head.position + head.forward * 0.37f + head.right * 0.02f - head.up * 0.13f;
        Vector3 bottlePos = pickupPos;
        float spin = 0f;
        if (t >= 4.35f)
        {
            bottlePos = Vector3.Lerp(pickupPos, bottleHold, S(4.35f, 6.0f, t));
            spin = (t - 4.35f) * 28f;
        }
        if (grabbed && bottle)
        {
            bottle.position = bottlePos;
            bottle.rotation = Quaternion.Euler(0f, spin, 4f);   // upright, slowly inspected
        }

        // hand: rest -> reach down to the belt -> cup just below the bottle
        Vector3 rest = head.position + head.forward * 0.34f + head.right * 0.17f - head.up * 0.26f;
        Vector3 handPos;
        if (t < 3.4f) handPos = rest;
        else if (t < 4.2f) handPos = Vector3.Lerp(rest, pickupPos - Vector3.up * 0.03f, S(3.4f, 4.2f, t));
        else if (t < 4.35f) { handPos = pickupPos - Vector3.up * 0.03f; grabbed = true; }
        else handPos = bottlePos - head.up * 0.03f - head.forward * 0.02f;
        hand.position = handPos;
        hand.rotation = Quaternion.LookRotation(head.forward, Vector3.up);
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
        NormalizeHeight(go, 0.30f);
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

    static void NormalizeHeight(GameObject go, float target)
    {
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        if (b.size.y > 1e-4f) go.transform.localScale *= target / b.size.y;
    }

    static float S(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));
}
