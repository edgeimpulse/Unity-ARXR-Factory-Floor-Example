using UnityEngine;

/// Attached to a rejected bottle once it is shoved off the belt: it shatters
/// (Bottle.Explode) on its first solid impact after dropping below the belt, so
/// failed products smash on the ground instead of vanishing in place.
[RequireComponent(typeof(Rigidbody))]
public class SmashOnImpact : MonoBehaviour
{
    [Tooltip("Scale applied to the spawned shards so they match a resized bottle.")]
    public float shardScale = 1f;

    [Tooltip("Only smash once the bottle has fallen below this world height.")]
    public float smashBelowY = float.PositiveInfinity;

    [Tooltip("Safety cleanup if it never lands.")]
    public float maxLifetime = 6f;

    Bottle bottle;
    float age;
    bool done;

    void Awake() { bottle = GetComponent<Bottle>(); }

    void Update()
    {
        age += Time.deltaTime;
        if (!done && age > maxLifetime) Smash();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (done) return;
        if (transform.position.y <= smashBelowY) Smash();
    }

    void Smash()
    {
        done = true;
        if (bottle) bottle.Explode(shardScale);
        else Destroy(gameObject);
    }
}
