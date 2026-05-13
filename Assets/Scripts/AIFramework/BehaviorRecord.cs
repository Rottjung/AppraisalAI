using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class BehaviorRecord
{
    [SerializeField] private string id;
    [SerializeField] private string payloadId;
    [SerializeField] private List<BehaviorCoordinate> coordinates = new();
    [SerializeField] private List<PayloadFilter> filters = new();
    [SerializeField] private float score;

    public IReadOnlyList<PayloadFilter> Filters => filters;

    public string Id => id;
    public string PayloadId => payloadId;
    public IReadOnlyList<BehaviorCoordinate> Coordinates => coordinates;
    public float Score { get => score; set => score = value; }

    public BehaviorRecord(string id, string payloadId)
    {
        this.id = id;
        this.payloadId = payloadId;
    }

    public void AddCoordinate(BehaviorCoordinate coordinate)
    {
        if (coordinate == null)
        {
            return;
        }

        coordinates.Add(coordinate);
    }

    public void ClearCoordinates()
    {
        coordinates.Clear();
    }

    public void AddFilter(PayloadFilter filter)
    {
        filters.Add(filter);
    }

    public float DistanceTo(DecisionBrain brain)
    {
        return MathUtil.WeightedDistance(coordinates, brain);
    }

    public float DistanceToRecord(BehaviorRecord other)
    {
        float sum = 0f;
        for (int i = 0; i < coordinates.Count; i++)
        {
            string nodeId = coordinates[i].BehaviorNodeId;
            float myVal = coordinates[i].Value;
            float otherVal = 0f;

            for (int j = 0; j < other.coordinates.Count; j++)
            {
                if (other.coordinates[j].BehaviorNodeId == nodeId)
                {
                    otherVal = other.coordinates[j].Value;
                    break;
                }
            }

            float delta = myVal - otherVal;
            sum += delta * delta * coordinates[i].Weight;
        }
        return Mathf.Sqrt(sum);
    }

    public void MergeWith(BehaviorRecord newExperience, float blendFactor)
    {
        for (int i = 0; i < coordinates.Count; i++)
        {
            string nodeId = coordinates[i].BehaviorNodeId;
            for (int j = 0; j < newExperience.coordinates.Count; j++)
            {
                if (newExperience.coordinates[j].BehaviorNodeId == nodeId)
                {
                    float oldVal = coordinates[i].Value;
                    float newVal = newExperience.coordinates[j].Value;
                    coordinates[i].SetValue(Mathf.Lerp(oldVal, newVal, blendFactor));
                    break;
                }
            }
        }
    }

    public override string ToString()
    {
        return $"{id} -> {payloadId} ({coordinates.Count} coords)";
    }
}