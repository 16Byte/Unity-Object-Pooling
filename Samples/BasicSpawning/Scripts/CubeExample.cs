using System.Collections;
using UnityEngine;
using PMG.Pooling;

public class CubeExample : MonoBehaviour, IPooledObject
{
    // This script is an example of the full life cycle of a pooled object.

    public float upForce = 10f;
    public float sideForce = 1f;

    private Rigidbody rb;

    private void OnEnable() => StartCoroutine(Destroy());
    private void Awake() => rb = GetComponent<Rigidbody>();

    private IEnumerator Destroy()
    {
        yield return new WaitForSeconds(Random.Range(1, 5));
        ObjectPoolManager.ReturnObjectToPool(transform);
    }

    public void OnObjectSpawn()
    {
        float xForce = Random.Range(-sideForce, sideForce);
        float yForce = Random.Range(upForce / 2f, upForce);
        float zForce = Random.Range(-sideForce, sideForce);

        Vector3 force = new Vector3(xForce, yForce, zForce);

        rb.linearVelocity = force;
    }
}
