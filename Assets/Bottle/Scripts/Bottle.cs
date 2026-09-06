using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bottle : MonoBehaviour
{
    [SerializeField] GameObject brokenBottlePrefab;

    /// Shatters the bottle into its broken pieces and removes the intact bottle.
    public void Explode()
    {
        if (brokenBottlePrefab)
        {
            GameObject brokenBottle = Instantiate(brokenBottlePrefab, transform.position, transform.rotation);
            var bb = brokenBottle.GetComponent<BrokenBottle>();
            if (bb) bb.RandomVelocities();
        }
        Destroy(gameObject);
    }
}
