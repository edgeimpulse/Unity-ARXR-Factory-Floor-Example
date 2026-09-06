using UnityEngine;

/// A product travelling along the conveyor. Carries the real bottle-cap photo
/// that the inspection station shows to the Edge Impulse model.
[DisallowMultipleComponent]
public class ProductItem : MonoBehaviour
{
    [Tooltip("Real bottle-cap photo presented to the inspection model.")]
    public Texture2D inspectionImage;

    [Tooltip("Ground truth, used for stats and as a fallback when the native model is unavailable.")]
    public bool defectGroundTruth;

    [HideInInspector] public float progress;   // 0..1 along the belt
    [HideInInspector] public bool inspected;
    [HideInInspector] public bool flaggedDefect;
}
