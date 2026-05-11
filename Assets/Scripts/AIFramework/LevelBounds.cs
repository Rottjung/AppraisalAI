using UnityEngine;

public class LevelBounds : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private float inset = 0.5f;

    private Bounds cachedBounds;
    private bool hasCachedBounds;

    public Bounds CachedBounds => cachedBounds;

    private void Awake()
    {
        RebuildCache();
    }

    private void OnValidate()
    {
        RebuildCache();
    }

    [ContextMenu("Rebuild Cache")]
    public void RebuildCache()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<Renderer>();
        }

        if (targetRenderer == null)
        {
            hasCachedBounds = false;
            return;
        }

        cachedBounds = targetRenderer.bounds;

        Vector3 size = cachedBounds.size;
        size.x = Mathf.Max(0f, size.x - inset * 2f);
        size.z = Mathf.Max(0f, size.z - inset * 2f);

        cachedBounds = new Bounds(cachedBounds.center, size);
        hasCachedBounds = true;
    }

    public Vector3 GetRandomPointInside()
    {
        if (!hasCachedBounds)
        {
            RebuildCache();
        }

        return MathUtil.RandomPointInBounds(cachedBounds);
    }

    public Vector3 ClampPointInside(Vector3 point)
    {
        if (!hasCachedBounds)
        {
            RebuildCache();
        }

        point.x = Mathf.Clamp(point.x, cachedBounds.min.x, cachedBounds.max.x);
        point.z = Mathf.Clamp(point.z, cachedBounds.min.z, cachedBounds.max.z);
        point.y = cachedBounds.center.y;

        return point;
    }

    internal Bounds GetWorldBounds()
    {
        if (!hasCachedBounds)
        {
            RebuildCache();
        }

        return cachedBounds;
    }

    private void OnDrawGizmosSelected()
    {
        if (!hasCachedBounds)
        {
            RebuildCache();
        }

        if (!hasCachedBounds)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(cachedBounds.center, cachedBounds.size);
    }
}