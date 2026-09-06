using UnityEngine;

/// Defines the conveyor path (spawn -> exit), the travel speed and where along
/// the belt inspection happens. Also scrolls the belt material for a moving look.
public class ConveyorBelt : MonoBehaviour
{
    [Header("Path")]
    public Transform spawnPoint;
    public Transform exitPoint;

    [Header("Motion")]
    [Min(0f)] public float speed = 0.35f;          // metres / second
    [Range(0f, 1f)] public float inspectAt = 0.55f; // fraction of the belt

    [Header("Belt visual (optional)")]
    public Renderer beltRenderer;
    public string scrollProperty = "_BaseMap";
    public float scrollSpeed = 0.5f;

    Vector3 A => spawnPoint ? spawnPoint.position : transform.position;
    Vector3 B => exitPoint ? exitPoint.position : transform.position + transform.forward * 3f;

    public float Length => Vector3.Distance(A, B);
    public Vector3 PositionAt(float t) => Vector3.Lerp(A, B, Mathf.Clamp01(t));
    public Quaternion Facing =>
        (B - A).sqrMagnitude > 1e-4f ? Quaternion.LookRotation((B - A).normalized, Vector3.up) : transform.rotation;

    void Update()
    {
        if (!beltRenderer) return;
        var m = beltRenderer.material;
        if (!m.HasProperty(scrollProperty)) return;
        var o = m.GetTextureOffset(scrollProperty);
        o.x = Mathf.Repeat(o.x + scrollSpeed * Time.deltaTime, 1f);
        m.SetTextureOffset(scrollProperty, o);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(A, B);
        Gizmos.DrawWireSphere(PositionAt(inspectAt), 0.05f);
    }
}
