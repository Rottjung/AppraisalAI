using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CreatureBrainController : MonoBehaviour
{
    [SerializeField] private DecisionBrain brain;
    [SerializeField] private Controller controller;
    [SerializeField] private Sensors sensors;

    [SerializeField] private TMP_Text debugText;

    [SerializeField] private LevelBounds levelBounds;
    [SerializeField] private LearningController learningController;
    [SerializeField] private LearningState learningState;

    [Header("Decision Timing")]
    [SerializeField] private float decisionInterval = 0.15f;

    [Header("Movement")]
    [SerializeField] private float moveStrength = 1f;

    [Header("Eating")]
    [SerializeField] private float eatDistance = 1.2f;
    [SerializeField] private float hungerRestore = 0.6f;
    [SerializeField] private float energyRestoreOnEat = 0.35f;

    [Header("Idle/Wander Reservoir")]
    [SerializeField] private float idleChargePerSecond = 1f;
    [SerializeField] private float idleDrainPerSecond = 1f;

    [Header("Base Energy By State")]
    [SerializeField] private float idleEnergyGainPerSecond = 0.08f;

    [Header("Metabolic Stress Scaling")]
    [SerializeField] private float idleRecoveryStressPenalty = 0.5f;

    [Header("Wander Execution Parameters")]
    [SerializeField] private float wanderDistance = 6f;
    [SerializeField] private float retargetInterval = 2f;
    [SerializeField] private float minTargetDistance = 2f;
    [SerializeField, Range(0f, 1f)] private float forwardBias = 0.6f;
    [SerializeField] private Transform wanderTransform;

    [Header("Wander Target Sampling")]
    [SerializeField] private bool chainFromPreviousTarget = true;
    [SerializeField, Range(0f, 1f)] private float previousTargetBias = 0.8f;
    [SerializeField] private float minDistanceFromCurrentWhenChained = 1.5f;

    [Header("Exploration")]
    [SerializeField, Range(0f, 1f)] private float explorationRate = 0.15f;

    [Header("Health")]
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private float damageInvincibilityTime = 1f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float attackDamage = 1f;
    [SerializeField] private float attackEnergyCost = 0.06f;

    [Header("Energy Drain")]
    [SerializeField] private float fleeEnergyDrainMultiplier = 0.05f;
    [SerializeField] private float wanderSpeed = 0.6f;
    [SerializeField] private float seekFoodSpeed = 1f;
    [SerializeField] private float fleeBaseSpeed = 1.5f;
    [SerializeField] private float fleeMaxSpeed = 2.5f;
    [SerializeField] private float approachSpeed = 0.5f;

    [Header("Episodes")]
    [SerializeField] private int maxLifetimes = 100;
    [SerializeField] private float respawnDelay = 1f;

    [Header("Debug")]
    [SerializeField] private bool logDecisions = false;
    [SerializeField] private bool drawWanderTarget = true;

    private float decisionTimer;
    private string currentPayload = "Idle";

    private float wanderRetargetTimer;
    private Vector3 wanderTarget = Vector3.zero;
    private bool hasWanderTarget;

    private bool isDead;
    private string activeEpisodeType;
    private int currentLifetime;
    private float deathTimer;
    private bool waitingForRespawn;

    private FeatureNode cachedMetabolicStress;
    private FeatureNode cachedRecoveryNeed;

    private float health;
    private float invincibilityTimer;
    private float attackTimer;

    private void Awake()
    {
        if (brain != null)
        {
            cachedMetabolicStress = brain.GetFeatureNode("MetabolicStress");
            cachedRecoveryNeed = brain.GetFeatureNode("RecoveryNeed");
        }
        health = maxHealth;
    }

    private void Start()
    {
        sensors?.EnsureSignal("health", 1f);
        learningState?.EnsureSignal("enemyKilled", SignalType.Int, 0);
        SetupAttackDrive();
    }

    private void SetupAttackDrive()
    {
        if (brain == null)
            return;

        if (brain.GetBehaviorNode("AttackDrive") != null)
            return;

        var attackDrive = new BehaviorNode("AttackDrive", "Attack Drive");
        attackDrive.SetActivationType(ActivationType.Sigmoid);

        attackDrive.AddConnection(new Connection("enemyProximity", 1.5f));
        attackDrive.AddConnection(new Connection("energy", 0.8f));
        attackDrive.AddConnection(new Connection("hunger", 0.3f));

        brain.AddBehaviorNode(attackDrive);

        var attackRecord = new BehaviorRecord("attack_record", "Attack");
        attackRecord.AddCoordinate(new BehaviorCoordinate("AttackDrive", 1f));
        attackRecord.AddCoordinate(new BehaviorCoordinate("FoodDrive", 0f));
        attackRecord.AddCoordinate(new BehaviorCoordinate("Fear", 0.3f));
        attackRecord.AddCoordinate(new BehaviorCoordinate("WanderDrive", 0f));
        attackRecord.AddCoordinate(new BehaviorCoordinate("RepickDrive", 0f));
        attackRecord.AddFilter(new PayloadFilter
            { signalId = "enemyProximity", comparison = ComparisonType.Greater, value = 0f });
        attackRecord.AddFilter(new PayloadFilter
            { signalId = "energy", comparison = ComparisonType.Greater, value = 0.2f });

        brain.Cloud.AddRecord(attackRecord);
    }

    private void Update()
    {
        if (sensors == null || controller == null)
            return;

        float dt = Time.deltaTime;

        if (invincibilityTimer > 0f)
            invincibilityTimer -= dt;

        if (attackTimer > 0f)
            attackTimer -= dt;

        if (isDead)
        {
            controller.Stop();

            if (waitingForRespawn)
            {
                deathTimer -= dt;
                if (deathTimer <= 0f)
                    Respawn();
            }
            return;
        }

        CheckDeath();

        if (isDead)
        {
            controller.Stop();

            if (waitingForRespawn)
            {
                deathTimer -= dt;
                if (deathTimer <= 0f)
                    Respawn();
            }
            return;
        }

        decisionTimer -= dt;
        if (decisionTimer <= 0f)
        {
            decisionTimer += decisionInterval;
            EvaluateDecision();
        }

        ExecuteCurrentPayload();
        UpdateIdleTime();

        if (Input.GetKeyDown(KeyCode.L))
            learningController.ToggleLog();
    }

    private float GetMetabolicStress()
    {
        return cachedMetabolicStress != null ? Mathf.Clamp01(cachedMetabolicStress.Value) : 0f;
    }

    private float GetRecoveryNeed()
    {
        return cachedRecoveryNeed != null ? Mathf.Clamp01(cachedRecoveryNeed.Value) : 0f;
    }

    public void TakeDamage(float amount)
    {
        if (invincibilityTimer > 0f || isDead)
            return;

        health = Mathf.Max(0f, health - amount);
        sensors?.SetSignal("health", health / maxHealth);
        learningState?.Apply("health", health / maxHealth, false);
        invincibilityTimer = damageInvincibilityTime;

        if (logDecisions)
            Debug.Log($"Creature took {amount} damage, health={health}/{maxHealth}", this);
    }

    public void OnEnemyKilled()
    {
        learningState?.Apply("enemyKilled", 1f, true);
        if (logDecisions)
            Debug.Log("Creature killed an enemy!", this);
    }

    private void CheckDeath()
    {
        if (sensors.GetValue("energy") <= 0f || health <= 0f)
            Die();
    }

    public void Die()
    {
        if (isDead)
            return;

        learningState?.Apply("isDead", 1f, false);
        EndActiveEpisode();

        isDead = true;
        currentPayload = "Dead";
        hasWanderTarget = false;
        controller.Stop();
        debugText.text = "Dead";

        currentLifetime++;

        if (logDecisions)
            Debug.Log($"Creature died. Lifetime {currentLifetime}/{maxLifetimes}", this);

        if (currentLifetime >= maxLifetimes)
        {
            debugText.text = "Done";
            if (logDecisions)
                Debug.Log("All lifetimes complete.", this);
            return;
        }

        waitingForRespawn = true;
        deathTimer = respawnDelay;
    }

    private void Respawn()
    {
        waitingForRespawn = false;
        isDead = false;
        health = maxHealth;
        invincibilityTimer = 0f;
        attackTimer = 0f;
        decisionTimer = 0f;

        sensors?.SetSignal("health", 1f);
        sensors?.SetSignal("energy", 1f);
        sensors?.SetSignal("hunger", 0f);
        sensors?.SetSignal("idleTime", 0f);
        learningState?.Apply("isDead", 0f, false);

        if (levelBounds != null)
            transform.position = levelBounds.GetRandomPointInside();

        currentPayload = "Idle";
        hasWanderTarget = false;
        debugText.text = "Respawn";

        if (logDecisions)
            Debug.Log($"Creature respawned. Lifetime {currentLifetime}/{maxLifetimes}", this);
    }

    private void EvaluateDecision()
    {
        if (brain == null || sensors == null || isDead)
            return;

        sensors.Sense();
        brain.EvaluateAll();

        RecordStepForActiveEpisode();

        List<RetrievalCandidate> candidates = brain.QueryCloudCandidates();
        RetrievalCandidate result;

        if (explorationRate > 0f && Random.value < explorationRate)
        {
            result = DecisionFilter.SelectRandomValid(candidates, sensors);
        }
        else
        {
            result = DecisionFilter.SelectFirstValid(candidates, sensors);
        }

        if (result == null)
        {
            debugText.text = "Idle";
            if (currentPayload != "Idle")
                OnPayloadChanged(currentPayload, "Idle");
            currentPayload = "Idle";
            return;
        }

        string proposedPayload = result.Record.PayloadId ?? "Idle";
        debugText.text = proposedPayload;

        if (logDecisions)
            Debug.Log($"Proposed={proposedPayload} stress={GetMetabolicStress():F2} recovery={GetRecoveryNeed():F2}", this);

        if (currentPayload != proposedPayload)
            OnPayloadChanged(currentPayload, proposedPayload);

        currentPayload = proposedPayload;
    }

    private void RecordStepForActiveEpisode()
    {
        if (activeEpisodeType != null && learningController != null)
            learningController.RecordStep();
    }

    private void UpdateIdleTime()
    {
        if (currentPayload == "Idle")
        {
            sensors.ApplyToSignal("idleTime", idleChargePerSecond * Time.deltaTime * (idleRecoveryStressPenalty * GetMetabolicStress()));
        }
        else
        {
            sensors.ApplyToSignal("idleTime", -idleDrainPerSecond * Time.deltaTime);
        }
    }

    private void EndActiveEpisode()
    {
        if (activeEpisodeType != null && learningController != null)
        {
            learningController.EndEpisode();
            activeEpisodeType = null;
        }
    }

    private void OnPayloadChanged(string previousPayload, string nextPayload)
    {
        EndActiveEpisode();

        if (nextPayload == "FleeEnemy")
        {
            hasWanderTarget = false;
            wanderRetargetTimer = 0f;
        }

        if (nextPayload == "Wander")
        {
            if (!hasWanderTarget)
                PickNewWanderTarget();
            wanderRetargetTimer = retargetInterval;
        }
        else if (nextPayload == "Idle" || nextPayload == "Dead")
        {
            if (nextPayload == "Idle")
                controller.Stop();
            return;
        }

        BeginEpisodeFor(nextPayload);
    }

    private void BeginEpisodeFor(string payload)
    {
        if (learningController == null)
            return;

        switch (payload)
        {
            case "Wander":
            case "SeekFood":
            case "FleeEnemy":
            case "Attack":
                learningController.BeginEpisode(payload);
                activeEpisodeType = payload;
                break;
        }
    }

    private void ExecuteCurrentPayload()
    {
        switch (currentPayload)
        {
            case "SeekFood":
                MoveToFood();
                break;

            case "FleeEnemy":
                FleeEnemy();
                break;

            case "Wander":
                Wander();
                break;

            case "RepickTarget":
                RepickTarget();
                break;

            case "Attack":
                Attack();
                break;

            case "Idle":
                Idle();
                break;

            case "Dead":
            default:
                controller.Stop();
                break;
        }
    }

    private void MoveToFood()
    {
        Sensor foodSensor = sensors.GetSensor("foodProximity");
        WorldTarget food = foodSensor != null ? foodSensor.LastDetectedWorldTarget : null;

        if (food == null)
        {
            controller.Stop();
            return;
        }

        float distance = MathUtil.DistanceXZ(transform.position, food.transform.position);
        if (distance <= eatDistance)
        {
            ConsumeFood(food.gameObject);
            controller.Stop();
            return;
        }

        Vector3 dir = MathUtil.DirectionXZ(transform.position, food.transform.position);
        controller.SpeedMultiplier = seekFoodSpeed;
        controller.Move(dir * moveStrength);
    }

    private void FleeEnemy()
    {
        Sensor enemySensor = sensors.GetSensor("enemyProximity");
        WorldTarget enemy = enemySensor != null ? enemySensor.LastDetectedWorldTarget : null;

        if (enemy == null)
        {
            controller.Stop();
            return;
        }

        Vector3 dir = MathUtil.DirectionXZ(enemy.transform.position, transform.position);
        if (dir.sqrMagnitude <= 0.0001f)
        {
            controller.Stop();
            return;
        }

        float energy = sensors.GetValue("energy");
        float speedMod = Mathf.Lerp(fleeBaseSpeed, fleeMaxSpeed, energy);
        controller.SpeedMultiplier = speedMod;
        sensors.ApplyToSignal("energy", -fleeEnergyDrainMultiplier * speedMod * Time.deltaTime);
        controller.Move(dir * moveStrength);
    }

    private void Attack()
    {
        Sensor enemySensor = sensors.GetSensor("enemyProximity");
        WorldTarget enemy = enemySensor != null ? enemySensor.LastDetectedWorldTarget : null;

        if (enemy == null)
        {
            controller.Stop();
            return;
        }

        float distance = MathUtil.DistanceXZ(transform.position, enemy.transform.position);

        if (distance <= attackRange)
        {
            if (attackTimer <= 0f)
            {
                attackTimer = attackCooldown;

                EnemyWander enemyWander = enemy.GetComponent<EnemyWander>();
                if (enemyWander != null)
                    enemyWander.TakeDamage(attackDamage);

                sensors.ApplyToSignal("energy", -attackEnergyCost);
            }
            controller.Stop();
        }
        else
        {
            Vector3 dir = MathUtil.DirectionXZ(transform.position, enemy.transform.position);
            controller.SpeedMultiplier = approachSpeed;
            controller.Move(dir * moveStrength);
        }
    }

    private void Wander()
    {
        if (levelBounds == null)
        {
            Idle();
            return;
        }

        if (!hasWanderTarget)
            PickNewWanderTarget();

        wanderRetargetTimer -= Time.deltaTime;
        wanderTarget = levelBounds.ClampPointInside(wanderTarget);

        float distanceToTarget = MathUtil.DistanceXZ(transform.position, wanderTarget);

        if (distanceToTarget <= minTargetDistance)
        {
            PickNewWanderTarget();
            wanderRetargetTimer = retargetInterval;
        }
        else if (wanderRetargetTimer <= 0f)
        {
            PickNewWanderTarget();
            wanderRetargetTimer = retargetInterval;
        }

        if (wanderTransform)
            wanderTransform.position = wanderTarget;

        Vector3 dir = MathUtil.DirectionXZ(transform.position, wanderTarget);
        if (dir.sqrMagnitude <= 0.0001f)
        {
            controller.Stop();
            return;
        }

        controller.SpeedMultiplier = wanderSpeed;
        controller.Move(dir * moveStrength);
    }

    private void RepickTarget()
    {
        hasWanderTarget = false;
        PickNewWanderTarget();
        wanderRetargetTimer = retargetInterval;
        currentPayload = "Wander";
        BeginEpisodeFor("Wander");
    }

    private void Idle()
    {
        sensors.ApplyToSignal("energy", idleEnergyGainPerSecond * Time.deltaTime);
        controller.SpeedMultiplier = 0f;
        controller.Stop();
    }

    private void PickNewWanderTarget()
    {
        if (levelBounds == null)
        {
            wanderTarget = transform.position;
            hasWanderTarget = false;
            return;
        }

        Bounds bounds = GetLevelBounds();

        bool canChainFromPrevious = hasWanderTarget && chainFromPreviousTarget;
        bool usePreviousAsReference = canChainFromPrevious && Random.value <= previousTargetBias;

        Vector3 referencePoint = usePreviousAsReference
            ? wanderTarget
            : transform.position;

        float localMinDistance = usePreviousAsReference
            ? Mathf.Max(minTargetDistance, minDistanceFromCurrentWhenChained)
            : minTargetDistance;

        Vector3 forwardDir = GetWanderForwardDirection();
        Vector3 proposed = PickBiasedWanderPoint(
            bounds,
            referencePoint,
            localMinDistance,
            wanderDistance,
            forwardDir,
            forwardBias
        );

        proposed = levelBounds.ClampPointInside(proposed);

        if (usePreviousAsReference)
        {
            float distanceFromCurrent = MathUtil.DistanceXZ(transform.position, proposed);
            if (distanceFromCurrent < minDistanceFromCurrentWhenChained)
            {
                proposed = PickBiasedWanderPoint(
                    bounds,
                    transform.position,
                    minTargetDistance,
                    wanderDistance,
                    forwardDir,
                    forwardBias
                );
                proposed = levelBounds.ClampPointInside(proposed);
            }
        }

        wanderTarget = proposed;
        hasWanderTarget = true;

        if (logDecisions)
        {
            float distanceFromCurrent = MathUtil.DistanceXZ(transform.position, wanderTarget);
            Debug.Log($"New Wander Target | ref={(usePreviousAsReference ? "previous" : "current")} target={wanderTarget} distanceFromCurrent={distanceFromCurrent:F2}", this);
        }
    }

    private Vector3 PickBiasedWanderPoint(
        Bounds bounds,
        Vector3 referencePoint,
        float minDistanceLocal,
        float maxDistanceLocal,
        Vector3 forwardDir,
        float forwardBiasLocal)
    {
        minDistanceLocal = Mathf.Max(0f, minDistanceLocal);
        maxDistanceLocal = Mathf.Max(minDistanceLocal, maxDistanceLocal);

        if (forwardDir.sqrMagnitude <= 0.0001f || forwardBiasLocal <= 0f)
            return MathUtil.RandomPointInBounds(bounds, referencePoint, maxDistanceLocal, minDistanceLocal);

        float distance = Random.Range(minDistanceLocal, maxDistanceLocal);

        Vector3 randomDir = MathUtil.RandomDirectionXZ();
        Vector3 blendedDir = (randomDir * (1f - forwardBiasLocal)) + (forwardDir.normalized * forwardBiasLocal);
        blendedDir.y = 0f;

        if (blendedDir.sqrMagnitude <= 0.0001f)
            blendedDir = randomDir;
        else
            blendedDir.Normalize();

        Vector3 point = referencePoint + blendedDir * distance;
        point.x = Mathf.Clamp(point.x, bounds.min.x, bounds.max.x);
        point.z = Mathf.Clamp(point.z, bounds.min.z, bounds.max.z);
        point.y = bounds.center.y;

        return point;
    }

    private Vector3 GetWanderForwardDirection()
    {
        if (hasWanderTarget)
        {
            Vector3 targetDir = MathUtil.DirectionXZ(transform.position, wanderTarget);
            if (targetDir.sqrMagnitude > 0.0001f)
                return targetDir;
        }

        Vector3 forward = transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude > 0.0001f)
            return forward.normalized;

        return Vector3.zero;
    }

    private Bounds GetLevelBounds()
    {
        return levelBounds.GetWorldBounds();
    }

    private void ConsumeFood(GameObject food)
    {
        learningState?.Apply("foodConsumed", 1f, true);
        sensors.ApplyToSignal("hunger", -hungerRestore);
        sensors.ApplyToSignal("energy", energyRestoreOnEat);
        Destroy(food);
        hasWanderTarget = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawWanderTarget)
            return;

        if (hasWanderTarget)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(wanderTarget, 0.25f);
            Gizmos.DrawLine(transform.position, wanderTarget);
        }
    }
}
