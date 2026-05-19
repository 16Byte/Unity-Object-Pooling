using UnityEngine;
using UnityEngine.InputSystem;
using PMG.Pooling;

public class PooledSpawnerExample : MonoBehaviour
{
    [SerializeField] private GameObject prefab;

    enum UpdateType { Default, Fixed, }
    [SerializeField] private UpdateType spawnTiming = UpdateType.Default;

    private bool canSpawn = false;

    private void Update()
    {
        canSpawn = Mouse.current.leftButton.isPressed;

        if(canSpawn && spawnTiming == UpdateType.Default)
            ObjectPoolManager.SpawnObject(prefab, transform.position, Quaternion.identity);
    }

    void FixedUpdate()
    {
        if(canSpawn && spawnTiming == UpdateType.Fixed)
            ObjectPoolManager.SpawnObject(prefab, transform.position, Quaternion.identity);
    }
}
