using System;
using UnityEngine;

[Serializable]
public sealed class RetrievalCandidate
{
    [SerializeField] private BehaviorRecord record;
    [SerializeField] private float distance;

    public BehaviorRecord Record => record;
    public float Distance => distance;

    public RetrievalCandidate(BehaviorRecord record, float distance)
    {
        this.record = record;
        this.distance = distance;
    }

    public override string ToString()
    {
        if (record == null)
        {
            return "Null candidate";
        }

        return $"{record.Id} -> {record.PayloadId}, Distance={distance:F3}";
    }
}