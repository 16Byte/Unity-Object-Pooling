using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace PMG.Pooling
{
    public class ObjectPoolManager : MonoBehaviour
    {
        [SerializeField]
        private bool _addToDontDestroyOnLoad = false;

        private GameObject _emptyHolder;

        private static GameObject _particleSystemsEmpty;
        private static GameObject _gameObjectsEmpty;
        private static GameObject _soundFXEmpty;

        private static Dictionary<GameObject, ObjectPool<GameObject>> _objectPools = new();
        private static Dictionary<GameObject, GameObject> _cloneToPrefabMap = new();

        public enum PoolType
        {
            ParticleSystems,
            GameObjects,
            SoundFX,
        }

        /// <summary>
        /// Retrieves a pooled instance of <paramref name="typePrefab"/> and places it at the given position and rotation.
        /// If no inactive instance is available, a new one is created and added to the pool.
        /// </summary>
        /// <typeparam name="T">A <see cref="Component"/> type to retrieve from the spawned GameObject.</typeparam>
        /// <param name="typePrefab">The prefab (as a component reference) to spawn.</param>
        /// <param name="spawnPosition">World-space position to place the object at.</param>
        /// <param name="spawnRotation">World-space rotation to apply to the object.</param>
        /// <param name="poolType">Which pool category to use. Defaults to <see cref="PoolType.GameObjects"/>.</param>
        /// <param name="parent">Optional parent transform. Defaults to the pool's category holder.</param>
        /// <returns>The requested component on the spawned GameObject, or <c>null</c> if the component is missing.</returns>
        public static T SpawnObject<T>(T typePrefab, Vector3 spawnPosition, Quaternion spawnRotation, PoolType poolType = PoolType.GameObjects, Transform parent = null) where T : Component
        {
            return SpawnObject<T>(typePrefab.gameObject, spawnPosition, spawnRotation, poolType, parent);
        }

        /// <summary>
        /// Retrieves a pooled <see cref="GameObject"/> instance of <paramref name="objectToSpawn"/> and places it at the given position and rotation.
        /// If no inactive instance is available, a new one is created and added to the pool.
        /// </summary>
        /// <param name="objectToSpawn">The prefab to spawn.</param>
        /// <param name="spawnPosition">World-space position to place the object at.</param>
        /// <param name="spawnRotation">World-space rotation to apply to the object.</param>
        /// <param name="poolType">Which pool category to use. Defaults to <see cref="PoolType.GameObjects"/>.</param>
        /// <param name="parent">Optional parent transform. Defaults to the pool's category holder.</param>
        /// <returns>The spawned <see cref="GameObject"/>.</returns>
        public static GameObject SpawnObject(GameObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation, PoolType poolType = PoolType.GameObjects, Transform parent = null)
        {
            return SpawnObject<GameObject>(objectToSpawn, spawnPosition, spawnRotation, poolType, parent);
        }

        /// <summary>
        /// Returns a previously spawned object back to its pool and deactivates it.
        /// The object must have been spawned via <see cref="SpawnObject"/> — untracked objects are ignored with a warning.
        /// </summary>
        /// <param name="obj">Transform of the object to return.</param>
        /// <param name="poolType">Pool category the object belongs to. Defaults to <see cref="PoolType.GameObjects"/>.</param>
        public static void ReturnObjectToPool(Transform obj, PoolType poolType = PoolType.GameObjects)
        {
            if (_cloneToPrefabMap.TryGetValue(obj.gameObject, out GameObject prefab))
            {
                GameObject parentObject = SetParentObject(poolType);

                if (obj.parent != parentObject.transform)
                    obj.SetParent(parentObject.transform);

                if (_objectPools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
                    pool.Release(obj.gameObject);
            }
            else
                Debug.LogWarning("Trying to return an object that is not pooled: " + obj.name);
        }

        private void Awake()
        {
            // Reset pool state on manager wake. Cross-scene pool persistence requires
            // _addToDontDestroyOnLoad; otherwise each scene gets a fresh pool. 
            _objectPools = new Dictionary<GameObject, ObjectPool<GameObject>>();
            _cloneToPrefabMap = new Dictionary<GameObject, GameObject>();

            SetupEmpties();
        }

        private void SetupEmpties()
        {
            _emptyHolder = new GameObject("Object Pools");

            _particleSystemsEmpty = new GameObject("Particle Effects");
            _particleSystemsEmpty.transform.SetParent(_emptyHolder.transform);

            _gameObjectsEmpty = new GameObject("GameObjects");
            _gameObjectsEmpty.transform.SetParent(_emptyHolder.transform);

            _soundFXEmpty = new GameObject("SoundFX");
            _soundFXEmpty.transform.SetParent(_emptyHolder.transform);

            EnsureEmptiesExist();

            if(_addToDontDestroyOnLoad)
                DontDestroyOnLoad(_particleSystemsEmpty.transform.root);
        }

        private static void CreatePool(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation, PoolType poolType = PoolType.GameObjects)
        {
            ObjectPool<GameObject> pool = new ObjectPool<GameObject>
            (
                createFunc: () => CreateObject(prefab, parent.position, parent.rotation, poolType),
                actionOnGet: OnGetObject,
                actionOnRelease: OnReleaseObject,
                actionOnDestroy: OnDestroyObject
            );

            _objectPools.Add(prefab, pool);
        }

        private static GameObject CreateObject(GameObject prefab, Vector3 position, Quaternion rotation, PoolType poolType = PoolType.GameObjects, Transform parent = null)
        {
            GameObject obj = Instantiate(prefab, position, rotation, parent);
            obj.SetActive(false);

            // IMPORTANT: deactivate the clone, never the prefab. Toggling the prefab's
            // active state is unsafe — scene-instance references will flicker visibly,
            // and any exception between SetActive(false) and SetActive(true) leaves the
            // prefab stranded inactive permanently.

            return obj;
        }

        // Spawns a POOLED object, if none are available it instantiates a new one (increasing the pool size dynamically to a peak capacity)
        private static T SpawnObject<T>(GameObject objectToSpawn, Vector3 spawnPosition, Quaternion spawnRotation, PoolType poolType = PoolType.GameObjects, Transform parent = null) where T : Object
        {
            if (parent == null)
                parent = SetParentObject(poolType).transform;

            if (!_objectPools.ContainsKey(objectToSpawn)) // no objects in the pool are available (if there are any)
                CreatePool(objectToSpawn, parent, spawnPosition, spawnRotation, poolType); // let's add one to the pool

            GameObject obj = _objectPools[objectToSpawn].Get(); // now let's put our hand in the pool

            if (obj != null) // we reach in and find and object in the pool, let's use it.
            {
                if (!_cloneToPrefabMap.ContainsKey(obj))
                    _cloneToPrefabMap.Add(obj, objectToSpawn);

                obj.transform.SetParent(parent);
                obj.transform.position = spawnPosition;
                obj.transform.rotation = spawnRotation;
                obj.SetActive(true);

                // Fire the spawn hook for any component implementing IPooledObject.
                // Done after positioning + reactivation so the component sees a fully-set-up object.
                if (obj.TryGetComponent<IPooledObject>(out var pooledObject))
                    pooledObject.OnObjectSpawn();

                if (typeof(T) == typeof(GameObject))
                    return obj as T;

                T component = obj.GetComponent<T>();
                if(component == null)
                {
                    Debug.LogError($"Object {objectToSpawn.name} doesn't have component of type {typeof(T)}");
                    return null;
                }
                return component; // return the reference to the object we're 'spawning' just like instantiate does.
            }
            return null; // we reach in and don't find anything. Something is wrong.
        }

        private static GameObject SetParentObject(PoolType poolType)
        {
            return poolType switch
            {
                PoolType.ParticleSystems => _particleSystemsEmpty,
                PoolType.GameObjects     => _gameObjectsEmpty,
                PoolType.SoundFX         => _soundFXEmpty,
                _                        => null,
            };
        }

        private static void EnsureEmptiesExist()
        {
            if (_gameObjectsEmpty != null) return;
            
            GameObject holder = new GameObject("Object Pools");
            _particleSystemsEmpty = new GameObject("Particle Effects");
            _gameObjectsEmpty     = new GameObject("GameObjects");
            _soundFXEmpty         = new GameObject("SoundFX");
            
            _particleSystemsEmpty.transform.SetParent(holder.transform);
            _gameObjectsEmpty.transform.SetParent(holder.transform);
            _soundFXEmpty.transform.SetParent(holder.transform);
        }

        // optional logic for when we get the object
        private static void OnGetObject(GameObject obj)
        {
            
        }

        // putting our object back in the pool
        private static void OnReleaseObject(GameObject obj)
        {
            obj.SetActive(false);
        }

        private static void OnDestroyObject(GameObject obj)
        {
            if (_cloneToPrefabMap.ContainsKey(obj))
            {
                _cloneToPrefabMap.Remove(obj);
            }
        }
    }
}
