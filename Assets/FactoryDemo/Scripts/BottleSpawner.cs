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

    [Header("Inspection photos (real bottle-cap images)")]
    public Texture2D[] correctImages;
    public Texture2D[] defectImages;

    readonly List<ProductItem> items = new List<ProductItem>();
    float timer;

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

            if (it.progress >= 1f) { Retire(it); items.RemoveAt(i); }
        }

        timer += Time.deltaTime;
        if (timer >= interval && items.Count < maxLiveItems) { timer = 0f; Spawn(); }
    }

    void Spawn()
    {
        var go = Instantiate(bottlePrefab, belt.PositionAt(0f), belt.Facing, transform);
        var it = go.GetComponent<ProductItem>();
        if (!it) it = go.AddComponent<ProductItem>();

        bool defect = Random.value < defectRate;
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
}
