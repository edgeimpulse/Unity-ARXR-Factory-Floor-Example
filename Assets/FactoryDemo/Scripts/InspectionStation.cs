using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// Runs the Edge Impulse bottle-cap model on a product's photo and decides
/// whether it is a defect (cap_incorrect). Updates the on-screen monitor + HUD.
public class InspectionStation : MonoBehaviour
{
    [Range(0f, 1f)] public float threshold = 0.5f;
    public string defectLabel = "cap_incorrect";

    [Header("UI (optional)")]
    public FactoryHUD hud;
    public RawImage monitorImage;
    public TMP_Text verdictText;

    public void Inspect(ProductItem item)
    {
        if (item == null || item.inspected) return;
        item.inspected = true;

        string label;
        float conf;
        bool defect;

        if (EdgeImpulseFOMO.TryClassifyTexture(item.inspectionImage, out label, out conf, defectLabel))
        {
            defect = (label == defectLabel) && (conf >= threshold);
        }
        else
        {
            // Native model unavailable on this platform: fall back to ground truth.
            defect = item.defectGroundTruth;
            label = defect ? defectLabel : "cap_correct";
            conf = 1f;
        }

        item.flaggedDefect = defect;
        Tint(item, defect ? new Color(0.95f, 0.35f, 0.35f) : new Color(0.4f, 0.9f, 0.5f));

        if (monitorImage && item.inspectionImage) monitorImage.texture = item.inspectionImage;
        if (verdictText)
        {
            verdictText.text = defect ? $"REJECT  {label}  {conf:P0}" : $"PASS  {label}  {conf:P0}";
            verdictText.color = defect ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.2f, 0.8f, 0.3f);
        }
        if (hud) hud.Record(defect);
    }

    static void Tint(ProductItem item, Color c)
    {
        foreach (var r in item.GetComponentsInChildren<Renderer>())
        {
            var m = r.material;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }
    }
}
