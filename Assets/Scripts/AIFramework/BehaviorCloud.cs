using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class BehaviorCloud
{
    [SerializeField] private List<BehaviorRecord> records = new();

    public IReadOnlyList<BehaviorRecord> Records => records;

    public void AddRecord(BehaviorRecord record)
    {
        if (record == null)
        {
            return;
        }

        records.Add(record);
    }

    public void Clear()
    {
        records.Clear();
    }

    public RetrievalCandidate GetNearest(DecisionBrain brain)
    {
        List<RetrievalCandidate> candidates = GetCandidates(brain);
        return candidates.Count > 0 ? candidates[0] : null;
    }

    public List<RetrievalCandidate> GetCandidates(DecisionBrain brain)
    {
        List<RetrievalCandidate> candidates = new();

        for (int i = 0; i < records.Count; i++)
        {
            BehaviorRecord record = records[i];
            if (record == null)
            {
                continue;
            }

            float distance = record.DistanceTo(brain);
            candidates.Add(new RetrievalCandidate(record, distance));
        }

        candidates.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        return candidates;
    }
}