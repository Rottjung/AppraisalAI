using UnityEngine;

public class EnemyWander : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Controller controller;
    [SerializeField] private LevelBounds levelBounds;

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 6f;
    [SerializeField] private float minDistanceFromCurrent = 2f;
    [SerializeField] private float reachDistance = 0.75f;
    [SerializeField] private float minWaitTime = 0.5f;
    [SerializeField] private float maxWaitTime = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private Vector3 currentTarget;
    private bool hasTarget;
    private float waitTimer;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<Controller>();
    }

    private void Start()
    {
        PickNewTarget();
    }

    private void Update()
    {
        if (controller == null || levelBounds == null)
            return;

        if (!hasTarget)
        {
            waitTimer -= Time.deltaTime;

            if (waitTimer <= 0f)
            {
                PickNewTarget();
            }

            return;
        }

        Vector3 position = transform.position;
        float distanceSq = MathUtil.DistanceXZSq(position, currentTarget);

        if (distanceSq <= reachDistance * reachDistance)
        {
            hasTarget = false;
            waitTimer = Random.Range(minWaitTime, maxWaitTime);
            controller.Move(Vector3.zero);
            return;
        }

        Vector3 direction = MathUtil.DirectionXZ(position, currentTarget);
        controller.Move(direction);
    }

    private void PickNewTarget()
    {
        Bounds bounds = levelBounds.GetWorldBounds();

        currentTarget = MathUtil.RandomPointInBounds(
            bounds,
            transform.position,
            wanderRadius,
            minDistanceFromCurrent
        );

        currentTarget = levelBounds.ClampPointInside(currentTarget);
        hasTarget = true;
    }

    public Vector3 GetCurrentTarget()
    {
        return currentTarget;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        if (hasTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(currentTarget, 0.2f);
            Gizmos.DrawLine(transform.position, currentTarget);
        }
    }
}