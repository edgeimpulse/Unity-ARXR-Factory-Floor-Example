using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bottle : MonoBehaviour
{
    [SerializeField] GameObject brokenBottlePrefab;

    /// Shatters the bottle into its broken pieces and removes the intact bottle.
    public void Explode() => Explode(1f);

    /// Shatters the bottle, scaling the spawned shards to match a resized bottle.
    public void Explode(float shardScale)
    {
        if (brokenBottlePrefab)
        {
            GameObject brokenBottle = Instantiate(brokenBottlePrefab, transform.position, transform.rotation);
            if (!Mathf.Approximately(shardScale, 1f))
                brokenBottle.transform.localScale *= shardScale;
            var bb = brokenBottle.GetComponent<BrokenBottle>();
            if (bb) bb.RandomVelocities();
        }
        Destroy(gameObject);
    }
}
