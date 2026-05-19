# PMG.Pooling API Reference

## Namespace

All public types live in the `PMG.Pooling` namespace.

```csharp
using PMG.Pooling;
```

## ObjectPoolManager

Static API for spawning and returning pooled GameObjects.

The class is a `MonoBehaviour`, but you do **not** need to place an instance in the scene to use it — the static methods lazy-bootstrap the required parent hierarchy on first call. Add it to a scene GameObject only when you want to configure `Add To DontDestroyOnLoad` or get explicit per-scene pool reset semantics.

### Static Methods

#### `SpawnObject` (GameObject overload)

```csharp
public static GameObject SpawnObject(
    GameObject objectToSpawn,
    Vector3 spawnPosition,
    Quaternion spawnRotation,
    PoolType poolType = PoolType.GameObjects,
    Transform parent = null);
```

Spawns a pooled instance of `objectToSpawn`. If no pool exists for this prefab yet, one is created automatically. If no instance is available in the pool, a new one is instantiated (the pool grows on demand).

Returns the spawned `GameObject`.

If the pool's `Get()` returns null (unusual — would indicate an internal pool failure), returns null and logs nothing further.

#### `SpawnObject<T>` (Generic overload)

```csharp
public static T SpawnObject<T>(
    T typePrefab,
    Vector3 spawnPosition,
    Quaternion spawnRotation,
    PoolType poolType = PoolType.GameObjects,
    Transform parent = null) where T : Component;
```

Generic version that takes a `Component` reference (any MonoBehaviour, Rigidbody, etc.) and returns a typed reference to the spawned component. Useful when you need to interact with the spawned object's components without a `GetComponent` call at the call site.

If the spawned object does not contain a component of type `T`, logs an error and returns null.

#### `ReturnObjectToPool`

```csharp
public static void ReturnObjectToPool(
    Transform obj,
    PoolType poolType = PoolType.GameObjects);
```

Returns a spawned object to its pool. The object is:

1. Reparented under the appropriate category empty (`PoolType`)
2. Deactivated (`SetActive(false)`)

If the object was not originally spawned through `SpawnObject`, a warning is logged and the call is a no-op.

### Configuration (MonoBehaviour fields)

#### `Add To DontDestroyOnLoad` (bool, Inspector)

When enabled, the pool's parent hierarchy survives scene transitions. Useful for game-wide pools.

When disabled (default), pool state resets on each scene load if a manager component is in the new scene.

## IPooledObject

```csharp
namespace PMG.Pooling
{
    public interface IPooledObject
    {
        void OnObjectSpawn();
    }
}
```

Optional interface for pooled object components. `OnObjectSpawn` is called automatically each time the object is spawned from the pool, after positioning and reactivation.

`OnObjectSpawn` is **not** called on first instantiation — only on subsequent pool retrievals. Use `Awake` for one-time setup, `OnObjectSpawn` for per-spawn state reset.

### Common uses

- Reset physics state (velocity, angular velocity)
- Reset gameplay state (health, ammo, timers)
- Restart coroutines (lifetime, despawn timers)
- Re-trigger particle systems
- Replay animation states

### Example

```csharp
using PMG.Pooling;
using UnityEngine;

public class Bullet : MonoBehaviour, IPooledObject
{
    private Rigidbody rb;
    private TrailRenderer trail;
    public float speed = 25f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        trail = GetComponent<TrailRenderer>();
    }

    public void OnObjectSpawn()
    {
        rb.linearVelocity = transform.forward * speed;
        trail.Clear(); // clear previous spawn's trail
    }
}
```

## PoolType

```csharp
public enum PoolType
{
    ParticleSystems,
    GameObjects,
    SoundFX,
}
```

Category labels used for hierarchy organization. Each value maps to a child `GameObject` of the scene's `Object Pools` root:

```
Object Pools
├── Particle Effects
├── GameObjects
└── SoundFX
```

Spawned instances are parented to the appropriate category. Returned instances are reparented back to their category.

## Lifecycle

The full lifecycle of a pooled object from the user's perspective:

1. **First spawn** — pool is empty, `Instantiate` is called, `Awake` and `OnEnable` fire as normal. `OnObjectSpawn` is **not** called on this initial path because the object hasn't been "returned to the pool" yet.
2. **Return to pool** — `ReturnObjectToPool` deactivates the object and reparents it. `OnDisable` fires.
3. **Re-spawn** — pool returns the existing instance, position/rotation are set, `SetActive(true)` is called (firing `OnEnable`), then `OnObjectSpawn` fires.
4. **Pool destroy (Unity Editor stop, scene change without DontDestroyOnLoad)** — the GameObject is destroyed by Unity, `OnDestroy` fires as normal.

Steps 2 and 3 are the hot path. `Awake` runs once per instance, `OnObjectSpawn` runs every time after the first.

## Threading

All API is main-thread only, matching the rest of Unity's GameObject API. Do not call `SpawnObject` or `ReturnObjectToPool` from Jobs, Tasks, or other background threads.

## Performance notes

- Pool lookup is a `Dictionary<GameObject, ObjectPool<GameObject>>` access — O(1) average.
- Clone-to-prefab reverse lookup is `Dictionary<GameObject, GameObject>` — O(1) average.
- The `_cloneToPrefabMap` adds one dictionary entry per unique spawned instance. For very large pools (10k+ unique objects) this is a noticeable memory cost but not a CPU one.
- `IPooledObject` lookup via `TryGetComponent<T>` is a cached component scan — fast, but cache the result yourself in `Awake` if you need to interact with it from hot code paths beyond `OnObjectSpawn`.
