using UnityEngine;

public class ObjectCounterDebugExample : MonoBehaviour
{
    void FixedUpdate() => Debug.Log($"{FindObjectsByType<CubeExample>(sortMode: FindObjectsSortMode.None).Length} cubes in the scene.");
}
