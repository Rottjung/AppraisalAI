using System;
using System.Collections.Generic;
using UnityEngine;

public enum ActivationType
{
    Linear,
    Clamped01,
    Sigmoid,
    Tanh
}

public static class MathUtil
{
    public struct Contribution
    {
        public string SourceId;
        public float SourceValue;
        public float Weight;
        public float ContributionValue;

        public Contribution(string sourceId, float sourceValue, float weight, float contributionValue)
        {
            SourceId = sourceId;
            SourceValue = sourceValue;
            Weight = weight;
            ContributionValue = contributionValue;
        }

        public override string ToString()
        {
            return $"{SourceId} value={SourceValue:F3} * weight={Weight:F3} => {ContributionValue:F3}";
        }
    }

    public static float ApplyActivation(float x, ActivationType type)
    {
        switch (type)
        {
            case ActivationType.Linear:
                return x;

            case ActivationType.Clamped01:
                return Mathf.Clamp01(x);

            case ActivationType.Sigmoid:
                return Sigmoid(x);

            case ActivationType.Tanh:
                return Tanh(x);

            default:
                return Mathf.Clamp01(x);
        }
    }

    public static float Sigmoid(float x)
    {
        return 1f / (1f + Mathf.Exp(-x));
    }

    public static float Tanh(float x)
    {
        return (float)Math.Tanh(x);
    }

    public static float Normalize(float value, float min, float max, bool clamp = true)
    {
        if (Mathf.Approximately(min, max))
        {
            return 0f;
        }

        float t = Mathf.InverseLerp(min, max, value);
        return clamp ? Mathf.Clamp01(t) : t;
    }

    public static float Summation(
        DecisionBrain brain,
        List<Connection> connections,
        float bias,
        List<Contribution> debug = null)
    {
        float sum = bias;

        if (debug != null)
        {
            debug.Clear();
        }

        for (int i = 0; i < connections.Count; i++)
        {
            Connection connection = connections[i];

            if (connection == null || !connection.IsEnabled)
                continue;

            NodeBase source = brain.GetNode(connection.SourceNodeId);

            if (source == null || !source.IsEnabled)
                continue;

            float value = source.OutputValue;
            float contribution = value * connection.Weight;

            sum += contribution;

            if (debug != null)
            {
                debug.Add(new Contribution(
                    source.Id,
                    value,
                    connection.Weight,
                    contribution
                ));
            }
        }

        return sum;
    }

    public static float WeightedDistance(
        List<BehaviorCoordinate> recordCoordinates,
        DecisionBrain brain)
    {
        float sum = 0f;

        for (int i = 0; i < recordCoordinates.Count; i++)
        {
            BehaviorCoordinate coordinate = recordCoordinates[i];

            float queryValue = brain.GetBehaviorValue(coordinate.BehaviorNodeId);
            float delta = queryValue - coordinate.Value;
            float weighted = delta * delta * coordinate.Weight;

            sum += weighted;
        }

        return Mathf.Sqrt(sum);
    }

    public static float DistanceXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    public static float DistanceXZSq(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    public static Vector3 DirectionXZ(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        return dir.normalized;
    }

    public static Vector3 RandomDirectionXZ()
    {
        Vector2 v = UnityEngine.Random.insideUnitCircle;

        if (v.sqrMagnitude <= 0.0001f)
        {
            return Vector3.forward;
        }

        v.Normalize();
        return new Vector3(v.x, 0f, v.y);
    }

    public static Vector3 ClampToBoundsXZ(Bounds bounds, Vector3 point, float y)
    {
        point.x = Mathf.Clamp(point.x, bounds.min.x, bounds.max.x);
        point.y = y;
        point.z = Mathf.Clamp(point.z, bounds.min.z, bounds.max.z);
        return point;
    }

    public static Vector3 RandomPointInBounds(
        Bounds bounds,
        Vector3? referencePoint = null,
        float radius = 0f,
        float minDistance = 0f)
    {
        if (referencePoint == null || radius <= 0f)
        {
            return new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                bounds.center.y,
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
            );
        }

        Vector3 refPoint = referencePoint.Value;

        minDistance = Mathf.Max(0f, minDistance);
        radius = Mathf.Max(minDistance, radius);

        float distance = UnityEngine.Random.Range(minDistance, radius);
        Vector2 dir2D = UnityEngine.Random.insideUnitCircle;

        if (dir2D.sqrMagnitude <= 0.0001f)
        {
            dir2D = Vector2.right;
        }
        else
        {
            dir2D.Normalize();
        }

        Vector3 point = new Vector3(
            refPoint.x + dir2D.x * distance,
            bounds.center.y,
            refPoint.z + dir2D.y * distance
        );

        return ClampToBoundsXZ(bounds, point, bounds.center.y);
    }
}