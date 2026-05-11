using UnityEngine;

public class WorldTarget : MonoBehaviour
{
    private void OnEnable()
    {
        WorldTargetRegistry.Register(this);
    }

    private void OnDisable()
    {
        WorldTargetRegistry.Unregister(this);
    }
}