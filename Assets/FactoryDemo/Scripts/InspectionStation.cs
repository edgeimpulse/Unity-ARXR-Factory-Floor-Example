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

        if (monitorImage && item.inspectionImage) monitorImage.texture = item.inspectionImage;
        if (verdictText)
        {
            verdictText.text = defect ? $"REJECT  {label}  {conf:P0}" : $"PASS  {label}  {conf:P0}";
            verdictText.color = defect ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.2f, 0.8f, 0.3f);
        }
        if (hud) hud.Record(defect);
    }
}
