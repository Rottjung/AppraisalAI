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

    [Header("Pursuit")]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float chaseSpeed = 6f;

    [Header("Health")]
    [SerializeField] private float maxHealth = 3f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private Vector3 currentTarget;
    private bool hasTarget;
    private float waitTimer;
    private Transform creatureTarget;
    private bool isChasing;
    private float defaultSpeed;
    private float health;
    private CreatureBrainController cachedCreature;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<Controller>();
        defaultSpeed = controller.Speed;
        health = maxHealth;
        if (levelBounds == null)
            levelBounds = FindFirstObjectByType<LevelBounds>();
        cachedCreature = FindFirstObjectByType<CreatureBrainController>();
    }

    private void Start()
    {
        FindCreature();
        if (!isChasing)
            PickNewTarget();
    }

    private void Update()
    {
        if (controller == null || levelBounds == null)
            return;

        FindCreature();

        if (isChasing)
        {
            ChaseCreature();
            return;
        }

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

    private void FindCreature()
    {
        if (cachedCreature == null)
        {
            isChasing = false;
            creatureTarget = null;
            return;
        }

        float dist = Vector3.Distance(transform.position, cachedCreature.transform.position);

        if (isChasing)
            return;

        if (dist <= detectionRadius)
        {
            creatureTarget = cachedCreature.transform;
            isChasing = true;
            controller.Speed = chaseSpeed;
            hasTarget = false;
        }
    }

    private void ChaseCreature()
    {
        if (creatureTarget == null || creatureTarget.gameObject == null)
        {
            isChasing = false;
            creatureTarget = null;
            return;
        }

        Vector3 dir = MathUtil.DirectionXZ(transform.position, creatureTarget.position);
        if (dir.sqrMagnitude > 0.0001f)
            controller.Move(dir);
        else
            controller.Move(Vector3.zero);

        if (levelBounds != null)
            transform.position = levelBounds.ClampPointInsideXZ(transform.position);
    }

    public void TakeDamage(float amount)
    {
        health -= amount;

        if (health <= 0f)
        {
            if (cachedCreature != null)
                cachedCreature.OnEnemyKilled();

            Destroy(gameObject);
        }
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

    public void SetLevelBounds(LevelBounds bounds)
    {
        levelBounds = bounds;
    }

    public Vector3 GetCurrentTarget()
    {
        return currentTarget;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = isChasing ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, isChasing ? detectionRadius : wanderRadius);

        Gizmos.color = Color.yellow;
        if (hasTarget && !isChasing)
        {
            Gizmos.DrawSphere(currentTarget, 0.2f);
            Gizmos.DrawLine(transform.position, currentTarget);
        }

        if (isChasing && creatureTarget != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, creatureTarget.position);
        }
    }
}
