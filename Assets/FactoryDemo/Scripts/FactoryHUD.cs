using UnityEngine;
using TMPro;

/// Simple pass/reject counter shown in the scene, plus an inference-mode label.
public class FactoryHUD : MonoBehaviour
{
    public TMP_Text passedText;
    public TMP_Text rejectedText;
    public TMP_Text statusText;

    int passed, rejected;

    void Start()
    {
        UpdateUI();
        if (statusText)
            statusText.text = EdgeImpulseFOMO.Available
                ? "Edge Impulse: on-device inference"
                : "Edge Impulse: simulated (native lib not loaded)";
    }

    public void Record(bool defect)
    {
        if (defect) rejected++; else passed++;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (passedText) passedText.text = $"Passed: {passed}";
        if (rejectedText) rejectedText.text = $"Rejected: {rejected}";
    }
}
