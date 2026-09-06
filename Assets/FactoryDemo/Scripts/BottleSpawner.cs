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

            if (it.flaggedDefect && it.progress >= 0.72f) { Retire(it); items.RemoveAt(i); continue; }
            if (it.progress >= 1f) { Retire(it); items.RemoveAt(i); }
        }

        timer += Time.deltaTime;
        if (timer >= interval && items.Count < maxLiveItems) { timer = 0f; Spawn(); }
    }

    void Spawn()
    {
        var go = Instantiate(bottlePrefab, belt.PositionAt(0f), Quaternion.identity, transform);
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true; // moved by script; don't topple
        NormalizeHeight(go);
        var it = go.GetComponent<ProductItem>();
        if (!it) it = go.AddComponent<ProductItem>();

        bool defect = (spawnCount++ % 2) == 1; // alternate pass/reject for a clear demo
        it.defectGroundTruth = defect;
        it.inspectionImage = Pick(defect ? defectImages : correctImages);
        it.progress = 0f;
        it.inspected = false;
        it.flaggedDefect = false;
        items.Add(it);
    }

    void Retire(ProductItem it)
    {
        if (it.flaggedDefect)
        {
            var b = it.GetComponent<Bottle>();
            if (b) { b.Explode(); return; }
        }
        Destroy(it.gameObject);
    }

    static Texture2D Pick(Texture2D[] arr) =>
        (arr != null && arr.Length > 0) ? arr[Random.Range(0, arr.Length)] : null;

    void NormalizeHeight(GameObject go)
    {
        if (itemHeight <= 0f) return;
        var rends = go.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;
        var b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        if (b.size.y > 1e-4f) go.transform.localScale *= itemHeight / b.size.y;
    }
}
