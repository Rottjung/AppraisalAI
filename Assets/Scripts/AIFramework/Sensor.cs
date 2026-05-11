using System;
using System.Collections.Generic;
using UnityEngine;

public enum SensorType
{
    NearestProximity,
    CountInRadius,
    AnyInRadius,
    StateDrift,
    RecentInRadius
}

public enum SensorMobility
{
    Static,
    NonStatic,
    Dynamic
}

public enum SensorOrigin
{
    Owner,
    Custom
}

[Serializable]
public class Sensor
{
    [SerializeField] private string signalId = string.Empty;
    [SerializeField] private SensorType type = SensorType.NearestProximity;
    [SerializeField] private SensorMobility mobility = SensorMobility.Static;

    [Header("External Sensor Settings")]
    [SerializeField] private LayerMask layerMask = ~0;
    [SerializeField] private string tagFilter = string.Empty;
    [SerializeField] private float radius = 10f;
    [NonSerialized] private WorldTarget lastDetectedWorldTarget;

    [Header("State Drift Settings")]
    [SerializeField] private float driftPerSecond = 0.05f;
    [SerializeField] private float initialValue = 0f;
    [SerializeField] private float minValue = 0f;
    [SerializeField] private float maxValue = 1f;

    [Header("Recent Any In Radius Settings")]
    [SerializeField] private float recentMemoryDuration = 1f;

    [Header("Origin Settings")]
    [SerializeField] private SensorOrigin originMode = SensorOrigin.Owner;
    [SerializeField] private Transform customOrigin;

    [NonSerialized] private float stateValue;

    [NonSerialized] private float recentMemoryRemaining;

    public SensorType Type
    {
        get => type;
        set => type = value;
    }

    public SensorMobility Mobility
    {
        get => mobility;
        set => mobility = value;
    }

    public string SignalId
    {
        get => signalId;
        set => signalId = value;
    }

    public float Radius
    {
        get => radius;
        set => radius = value;
    }

    public string TagFilter
    {
        get => tagFilter;
        set => tagFilter = value;
    }

    public LayerMask LayerMask
    {
        get => layerMask;
        set => layerMask = value;
    }

    public WorldTarget LastDetectedWorldTarget => lastDetectedWorldTarget;

    public float DriftPerSecond
    {
        get => driftPerSecond;
        set => driftPerSecond = value;
    }

    public float MinValue
    {
        get => minValue;
        set => minValue = value;
    }

    public float MaxValue
    {
        get => maxValue;
        set => maxValue = value;
    }

    public float InitialValue
    {
        get => initialValue;
        set => initialValue = value;
    }

    public float RecentMemoryDuration
    {
        get => recentMemoryDuration;
        set => recentMemoryDuration = value;
    }

    public SensorOrigin OriginMode
    {
        get => originMode;
        set => originMode = value;
    }

    public Transform CustomOrigin
    {
        get => customOrigin;
        set => customOrigin = value;
    }

    public (string signalId, float initialValue) Initialize()
    {
        stateValue = Mathf.Clamp(initialValue, minValue, maxValue);
        recentMemoryRemaining = 0f;

        return (signalId, stateValue);
    }

    public (string signalId, float value) Update(float deltaTime, Transform origin)
    {
        if (string.IsNullOrWhiteSpace(signalId))
            return (null, 0f);

        Transform usedOrigin = origin;
        if (originMode == SensorOrigin.Custom && customOrigin != null)
            usedOrigin = customOrigin;

        lastDetectedWorldTarget = null;
        if (recentMemoryRemaining > 0f)
            recentMemoryRemaining -= deltaTime;

        float value = type switch
        {
            SensorType.NearestProximity => ComputeNearestProximity(usedOrigin),
            SensorType.CountInRadius => ComputeCountInRadius(usedOrigin),
            SensorType.AnyInRadius => ComputeAnyInRadius(usedOrigin),
            SensorType.StateDrift => UpdateStateDrift(deltaTime),
            SensorType.RecentInRadius => ComputeRecentInRadius(usedOrigin, deltaTime),
            _ => 0f
        };

        return (signalId, value);
    }

    public void ApplyStateDelta(float delta, Sensors container)
    {
        if (type != SensorType.StateDrift)
        {
            return;
        }

        stateValue += delta;
        stateValue = Mathf.Clamp(stateValue, minValue, maxValue);

        if (container != null && !string.IsNullOrWhiteSpace(signalId))
        {
            container.SetSignal(signalId, stateValue);
        }
    }

    private int CountTargetsInRadius(Transform origin, out WorldTarget nearest)
    {
        nearest = null;

        if (origin == null || radius <= 0f)
            return 0;

        var targets = string.IsNullOrWhiteSpace(tagFilter)
            ? WorldTargetRegistry.AllTargets
            : WorldTargetRegistry.GetTargetsByTag(tagFilter);

        if (targets == null || targets.Count == 0)
            return 0;

        float radiusSqr = radius * radius;
        float bestSqr = float.MaxValue;
        int count = 0;

        for (int i = 0; i < targets.Count; i++)
        {
            var wt = targets[i];
            if (wt == null) continue;

            int layer = wt.gameObject.layer;
            if (((1 << layer) & layerMask) == 0)
                continue;

            Vector3 delta = wt.transform.position - origin.position;
            float sqr = delta.sqrMagnitude;

            if (sqr > radiusSqr)
                continue;

            count++;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                nearest = wt;
            }
        }

        return count;
    }

    private float ComputeNearestProximity(Transform origin)
    {
        int count = CountTargetsInRadius(origin, out var nearest);

        lastDetectedWorldTarget = nearest;

        if (count == 0 || nearest == null)
            return 0f;

        float dist = Vector3.Distance(origin.position, nearest.transform.position);
        return 1f - Mathf.Clamp01(dist / radius);
    }

    private float ComputeCountInRadius(Transform origin)
    {
        return CountTargetsInRadius(origin, out _);
    }
    private float ComputeAnyInRadius(Transform origin)
    {
        return CountTargetsInRadius(origin, out _) > 0 ? 1f : 0f;
    }

    private float UpdateStateDrift(float deltaTime)
    {
        stateValue += driftPerSecond * deltaTime;
        stateValue = Mathf.Clamp(stateValue, minValue, maxValue);
        return stateValue;
    }

    private float ComputeRecentInRadius(Transform origin, float deltaTime)
    {
        int detection = CountTargetsInRadius(origin, out var nearest);

        if (detection > 0f)
        {
            recentMemoryRemaining = recentMemoryDuration;
        }
        else if (recentMemoryRemaining > 0f)
        {
            recentMemoryRemaining -= deltaTime;
        }
        if (recentMemoryRemaining < 0f)
        {
            recentMemoryRemaining = 0f;
        }
        return recentMemoryRemaining > 0f ? 1f : 0f;
    }
}