using System.Collections.Generic;
using UnityEngine;

/// Spawns bottles at the head of the belt, advances every product along it,
/// triggers inspection at the inspection point and retires products at the end.
public class BottleSpawner : MonoBehaviour
{
    [Header("References")]
    public ConveyorBelt belt;
    public InspectionStation inspection;
    public GameObject bottlePrefab;

    [Header("Spawning")]
    [Min(0.25f)] public float interval = 2.5f;
    [Range(0f, 1f)] public float defectRate = 0.4f;
    public int maxLiveItems = 12;
    [Tooltip("Spawned bottles are scaled to roughly this height in metres.")]
    public float itemHeight = 0.4f;

    [Header("Reject pusher")]
    public RejectArm rejectArm;
    [Range(0f, 1f)] public float rejectAt = 0.72f;
    [Tooltip("Sideways speed the arm shoves a failed bottle off the belt.")]
    public float rejectPushSpeed = 1.3f;

    [Header("Inspection photos (real bottle-cap images)")]
    public Texture2D[] correctImages;
    public Texture2D[] defectImages;

    readonly List<ProductItem> items = new List<ProductItem>();
    float timer;
    int spawnCount;

    void Update()
    {
        if (!belt || !bottlePrefab) return;

        float perSec = belt.Length > 0.01f ? belt.speed / belt.Length : belt.speed;
        for (int i = items.Count - 1; i >= 0; i--)
        {
            var it = items[i];
            if (!it) { items.RemoveAt(i); continue; }

            it.progress += perSec * Time.deltaTime;
            it.transform.position = belt.PositionAt(it.progress);

            if (!it.inspected && it.progress >= belt.inspectAt && inspection)
                inspection.Inspect(it);

            if (it.flaggedDefect && it.progress >= rejectAt) { RejectBottle(it); items.RemoveAt(i); continue; }
            if (it.progress >= 1f) { Retire(it); items.RemoveAt(i); }
        }

        timer += Time.deltaTime;
        if (timer >= interval && items.Count < maxLiveItems) { timer = 0f; Spawn(); }
    }

    void Spawn()
    {
        var go = Instantiate(bottlePrefab, belt.PositionAt(0f), Quaternion.identity, transform);
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true; // moved by script; don't topple
        float f = NormalizeHeight(go);
        var it = go.GetComponent<ProductItem>();
        if (!it) it = go.AddComponent<ProductItem>();
        it.shardScale = f;

        bool defect = (spawnCount++ % 2) == 1; // alternate pass/reject for a clear demo
        it.defectGroundTruth = defect;
        it.inspectionImage = Pick(defect ? defectImages : correctImages);
        it.progress = 0f;
        it.inspected = false;
        it.flaggedDefect = false;
        items.Add(it);
    }

    // A failed bottle is detached from the belt, shoved sideways by the reject
    // arm and left to fall; SmashOnImpact shatters it when it hits the floor.
    void RejectBottle(ProductItem it)
    {
        it.transform.SetParent(null, true);

        Rigidbody root = null;
        foreach (var rb in it.GetComponentsInChildren<Rigidbody>())
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            if (root == null) root = rb;
        }
        foreach (var c in it.GetComponentsInChildren<Collider>()) c.enabled = true;

        Vector3 push = rejectArm ? rejectArm.PushDirection : Vector3.forward;
        if (root)
        {
            root.linearVelocity = push * rejectPushSpeed + Vector3.up * 0.35f;
            root.angularVelocity = Random.insideUnitSphere * 3f;
        }

        var smash = it.gameObject.AddComponent<SmashOnImpact>();
        smash.shardScale = it.shardScale;
        smash.smashBelowY = it.transform.position.y - 0.12f;

        if (rejectArm) rejectArm.Punch();
    }

    void Retire(ProductItem it) => Destroy(it.gameObject);

    static Texture2D Pick(Texture2D[] arr) =>
        (arr != null && arr.Length > 0) ? arr[Random.Range(0, arr.Length)] : null;

    float NormalizeHeight(GameObject go)
    {
        if (itemHeight <= 0f) return 1f;
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return 1f;
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        if (b.size.y <= 1e-4f) return 1f;
        float f = itemHeight / b.size.y;
        go.transform.localScale *= f;
        return f;
    }
}
