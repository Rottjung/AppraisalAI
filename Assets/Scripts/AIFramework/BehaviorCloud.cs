using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class BehaviorCloud
{
    [SerializeField] private List<BehaviorRecord> records = new();
    [SerializeField] private int maxRecords = 50;

    public IReadOnlyList<BehaviorRecord> Records => records;

    private readonly List<RetrievalCandidate> candidateCache = new();

    public void AddRecord(BehaviorRecord record)
    {
        if (record == null)
            return;

        records.Add(record);
        TrimExcess();
    }

    public void AddOrMergeRecord(BehaviorRecord candidate, float mergeThreshold)
    {
        if (candidate == null)
            return;

        BehaviorRecord nearest = null;
        float nearestDist = float.MaxValue;

        for (int i = 0; i < records.Count; i++)
        {
            if (records[i] == null || records[i].PayloadId != candidate.PayloadId)
                continue;

            float dist = records[i].DistanceToRecord(candidate);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = records[i];
            }
        }

        if (nearest != null && nearestDist <= mergeThreshold)
        {
            nearest.MergeWith(candidate, Mathf.Clamp01(1f - nearestDist / mergeThreshold));
        }
        else
        {
            records.Add(candidate);
            TrimExcess();
        }
    }

    private void TrimExcess()
    {
        while (records.Count > maxRecords)
            records.RemoveAt(0);
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
        candidateCache.Clear();

        for (int i = 0; i < records.Count; i++)
        {
            BehaviorRecord record = records[i];
            if (record == null)
                continue;

            float distance = record.DistanceTo(brain);
            candidateCache.Add(new RetrievalCandidate(record, distance));
        }

        candidateCache.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        return candidateCache;
    }
}
