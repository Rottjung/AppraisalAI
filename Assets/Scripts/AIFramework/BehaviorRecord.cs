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
    public IReadOnlyList<PayloadFilter> Filters => filters;

    public string Id => id;
    public string PayloadId => payloadId;
    public IReadOnlyList<BehaviorCoordinate> Coordinates => coordinates;

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

    public override string ToString()
    {
        return $"{id} -> {payloadId} ({coordinates.Count} coords)";
    }
}