using System;
using System.Collections.Generic;
using UnityEngine;

public static class DecisionFilter
{
    public static RetrievalCandidate SelectFirstValid(
        List<RetrievalCandidate> candidates,
        Sensors sensors)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            RetrievalCandidate candidate = candidates[i];
            if (candidate == null || candidate.Record == null)
            {
                continue;
            }

            if (IsValid(candidate.Record, sensors))
            {
                return candidate;
            }
        }

        return null;
    }

    public static RetrievalCandidate SelectRandomValid(
        List<RetrievalCandidate> candidates,
        Sensors sensors)
    {
        if (candidates == null || candidates.Count == 0)
            return null;

        var valid = new List<RetrievalCandidate>();
        for (int i = 0; i < candidates.Count; i++)
        {
            RetrievalCandidate candidate = candidates[i];
            if (candidate != null && candidate.Record != null && IsValid(candidate.Record, sensors))
                valid.Add(candidate);
        }

        if (valid.Count == 0)
            return null;

        return valid[UnityEngine.Random.Range(0, valid.Count)];
    }

    public static bool IsValid(BehaviorRecord record, Sensors sensors)
    {
        if (record == null || sensors == null)
            return true;

        IReadOnlyList<PayloadFilter> filters = record.Filters;
        if (filters == null || filters.Count == 0)
            return true;

        for (int i = 0; i < filters.Count; i++)
        {
            PayloadFilter filter = filters[i];

            if (string.IsNullOrWhiteSpace(filter.signalId))
                continue;

            float currentValue = sensors.GetValue(filter.signalId);

            if (!PassesFilter(currentValue, filter))
                return false;
        }

        return true;
    }

    private static bool PassesFilter(float currentValue, PayloadFilter filter)
    {
        switch (filter.comparison)
        {
            case ComparisonType.Greater:
                return currentValue > filter.value;

            case ComparisonType.GreaterOrEqual:
                return currentValue >= filter.value;

            case ComparisonType.Less:
                return currentValue < filter.value;

            case ComparisonType.LessOrEqual:
                return currentValue <= filter.value;

            case ComparisonType.Equal:
                return Mathf.Approximately(currentValue, filter.value);

            case ComparisonType.NotEqual:
                return !Mathf.Approximately(currentValue, filter.value);

            default:
                return true;
        }
    }
}
