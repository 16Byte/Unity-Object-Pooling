# PMG Pooling

An `Instantiate`-shaped object pooling system for Unity. Drop-in replacement for `Instantiate`/`Destroy` with automatic pool management, lazy initialization, and hierarchy organization. Built on `UnityEngine.Pool.ObjectPool<T>` internally — battle-tested infrastructure, ergonomic surface.

## Why pool

`Instantiate` allocates, deserializes the prefab, runs `Awake`/`OnEnable`, registers with the scene, and triggers GC pressure. Repeated at a steady rate, this produces visible hitches and frame-time spikes. Pooling reuses existing instances, skipping most of that per-spawn cost — the result is smoother game feel, more consistent frame times, and a much lower GC footprint.

## Quickstart

Anywhere you'd write:

```csharp
Instantiate(prefab, position, rotation);
```

Write instead:

```csharp
ObjectPoolManager.SpawnObject(prefab, position, rotation);
```

That's the whole API for the simple case. The pool for that prefab is created lazily on first call — no setup required.

To return a spawned object to the pool:

```csharp
ObjectPoolManager.ReturnObjectToPool(transform);
```

Typically called from the object itself when its work is done (timed lifetime, death, off-screen, etc.).

## Installation

### Unity Package Manager (recommended)

In Unity, open **Window → Package Manager**, click the **+** dropdown, choose **Add package from git URL**, and paste:

```
https://github.com/16Byte/PMG.Pooling.git
```

UPM will fetch the package and add it to your project's `Packages/manifest.json`. Updates pulled with the Package Manager's refresh button.

### .unitypackage

Download the latest `.unitypackage` from the [Releases](https://github.com/16Byte/PMG.Pooling/releases) page and drag it into your Unity project's Assets folder.

## API

### `ObjectPoolManager.SpawnObject`

```csharp
// GameObject overload — direct Instantiate replacement
GameObject SpawnObject(
    GameObject prefab,
    Vector3 position,
    Quaternion rotation,
    PoolType poolType = PoolType.GameObjects,
    Transform parent = null);

// Generic overload — returns a typed Component reference
T SpawnObject<T>(
    T prefab,
    Vector3 position,
    Quaternion rotation,
    PoolType poolType = PoolType.GameObjects,
    Transform parent = null) where T : Component;
```

The generic overload gives you a typed reference back without a `GetComponent` call at the call site:

```csharp
Bullet b = ObjectPoolManager.SpawnObject(bulletPrefab, muzzle.position, muzzle.rotation);
b.SetDamage(50f);
```

### `ObjectPoolManager.ReturnObjectToPool`

```csharp
void ReturnObjectToPool(
    Transform obj,
    PoolType poolType = PoolType.GameObjects);
```

Returns a spawned object to its pool. The object is deactivated and re-parented under the appropriate category empty. If the object was not originally spawned through `SpawnObject`, a warning is logged and the call is a no-op.

### `IPooledObject` — optional lifecycle hook

```csharp
public interface IPooledObject
{
    void OnObjectSpawn();
}
```

Implement on any `MonoBehaviour` that needs per-spawn initialization. `OnObjectSpawn` fires after position/rotation are set and the GameObject is reactivated — the right place to reset velocity, restart coroutines, re-trigger particle effects, or reset gameplay state.

```csharp
public class Bullet : MonoBehaviour, IPooledObject
{
    private Rigidbody rb;
    public float speed = 25f;

    private void Awake() => rb = GetComponent<Rigidbody>();

    public void OnObjectSpawn()
    {
        rb.linearVelocity = transform.forward * speed;
    }
}
```

Note: `OnObjectSpawn` is **not** called on first instantiation. Use `Awake` for one-time setup, `OnObjectSpawn` for per-spawn state.

### `PoolType` — hierarchy categories

```csharp
ObjectPoolManager.PoolType.GameObjects      // default
ObjectPoolManager.PoolType.ParticleSystems
ObjectPoolManager.PoolType.SoundFX
```

Each value maps to a child `GameObject` of the `Object Pools` root in the scene hierarchy. Categorizing spawned instances keeps the hierarchy clean under heavy spawn load — your scene doesn't fill with 500 `Cube(Clone)` entries at the root.

## Configuration

The `ObjectPoolManager` MonoBehaviour is **optional**. The system works with no manager in the scene — pools and category empties are created on demand by the first `SpawnObject` call.

Add the component to a GameObject only if you need:

- **`Add To DontDestroyOnLoad`** — keeps the pool hierarchy alive across scene transitions. Useful for game-wide pools (persistent VFX, UI floaters, audio sources).
- **Per-scene pool reset** — placing the component triggers fresh dictionaries on its `Awake`. Without a manager in scene, pool state persists across loads.

## Sample

Import the `Basic Spawning` sample via **Window → Package Manager → PMG Pooling → Samples → Import**.

The sample includes:

- `CubeExample.cs` — a poolable rigidbody cube that randomizes spawn velocity and self-returns after 1–5 seconds.
- `PooledSpawnerExample.cs` — click-and-drag spawner; toggle between `Update` and `FixedUpdate` timing.
- `ObjectCounterDebugExample.cs` — logs the active cube count each fixed step.

**Note:** the sample uses the new Input System (`Mouse.current`), so the [Input System package](https://docs.unity3d.com/Manual/com.unity.inputsystem.html) must be installed and enabled for the sample to compile. The core runtime has no such dependency.

## Requirements

- Unity 6 (6000.0) or newer — the runtime depends on `UnityEngine.Pool.ObjectPool<T>` and uses the C# `new()` target-typed constructor syntax.
- Samples additionally require the Input System package.

## License

MIT — see [LICENSE.md](LICENSE.md). Free for commercial and personal use, including in shipped games. Attribution appreciated but not required.

## Contributing

Issues and pull requests welcome at [github.com/16Byte/PMG.Pooling](https://github.com/16Byte/PMG.Pooling).
