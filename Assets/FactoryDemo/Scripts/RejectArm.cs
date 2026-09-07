using UnityEngine;

/// A pneumatic reject pusher beside the belt. Call Punch() to shove a failed
/// product off the line; the piston snaps out along the arm's forward axis and
/// retracts. Purely visual — the shove itself is applied to the bottle in code.
[DisallowMultipleComponent]
public class RejectArm : MonoBehaviour
{
    public Transform piston;
    [Tooltip("How far the piston extends, in metres.")]
    public float stroke = 0.30f;
    public float extendTime = 0.08f;
    public float holdTime = 0.06f;
    public float retractTime = 0.24f;

    /// World direction the arm pushes (its forward axis).
    public Vector3 PushDirection => transform.forward;

    Vector3 pistonHome;
    float phase = -1f;   // <0 == idle

    void Awake() { if (piston) pistonHome = piston.localPosition; }

    public void Punch() { phase = 0f; }

    void Update()
    {
        if (phase < 0f || piston == null) return;
        phase += Time.deltaTime;

        float ext;
        if (phase < extendTime)
            ext = Mathf.SmoothStep(0f, 1f, phase / extendTime);
        else if (phase < extendTime + holdTime)
            ext = 1f;
        else
        {
            float r = (phase - extendTime - holdTime) / retractTime;
            ext = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(r));
            if (r >= 1f) phase = -1f;
        }

        piston.localPosition = pistonHome + Vector3.forward * (stroke * ext);
    }
}
