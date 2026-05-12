using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AppraisalAI/Behavior Cloud Data")]
public class BehaviorCloudData : ScriptableObject
{
    [SerializeField] private List<BehaviorRecord> records = new();
    [SerializeField] private float[] learnedOffsets = new float[0];

    public IReadOnlyList<BehaviorRecord> Records => records;
    public float[] LearnedOffsets => learnedOffsets;

    public void CopyFrom(BehaviorCloud cloud)
    {
        records.Clear();
        foreach (var record in cloud.Records)
        {
            var copy = new BehaviorRecord(record.Id, record.PayloadId);
            foreach (var coord in record.Coordinates)
                copy.AddCoordinate(new BehaviorCoordinate(coord.BehaviorNodeId, coord.Value, coord.Weight));
            foreach (var filter in record.Filters)
                copy.AddFilter(filter);
            records.Add(copy);
        }
    }

    public void CopyTo(BehaviorCloud cloud)
    {
        cloud.Clear();
        foreach (var record in records)
        {
            var copy = new BehaviorRecord(record.Id, record.PayloadId);
            foreach (var coord in record.Coordinates)
                copy.AddCoordinate(new BehaviorCoordinate(coord.BehaviorNodeId, coord.Value, coord.Weight));
            foreach (var filter in record.Filters)
                copy.AddFilter(filter);
            cloud.AddRecord(copy);
        }
    }

    public void CopyFromSO(BehaviorCloudData other)
    {
        records.Clear();
        learnedOffsets = new float[0];
        if (other == null) return;

        foreach (var record in other.records)
        {
            if (record == null) continue;
            var copy = new BehaviorRecord(record.Id, record.PayloadId);
            foreach (var coord in record.Coordinates)
                copy.AddCoordinate(new BehaviorCoordinate(coord.BehaviorNodeId, coord.Value, coord.Weight));
            foreach (var filter in record.Filters)
                copy.AddFilter(filter);
            records.Add(copy);
        }

        if (other.learnedOffsets.Length > 0)
        {
            learnedOffsets = new float[other.learnedOffsets.Length];
            System.Array.Copy(other.learnedOffsets, learnedOffsets, other.learnedOffsets.Length);
        }
    }

    public void CrossbreedFrom(BehaviorCloudData a, BehaviorCloudData b, float mutationRate, float mutationStrength)
    {
        records.Clear();
        learnedOffsets = new float[0];

        // Merge all records from both parents
        var seenIds = new HashSet<string>();
        for (int p = 0; p < 2; p++)
        {
            var source = p == 0 ? a : b;
            if (source == null) continue;
            foreach (var record in source.records)
            {
                if (record == null) continue;
                string key = $"{record.PayloadId}_{record.Id}";
                if (!seenIds.Add(key)) continue;
                var copy = new BehaviorRecord(record.Id, record.PayloadId);
                foreach (var coord in record.Coordinates)
                    copy.AddCoordinate(new BehaviorCoordinate(coord.BehaviorNodeId, coord.Value, coord.Weight));
                foreach (var filter in record.Filters)
                    copy.AddFilter(filter);
                records.Add(copy);
            }
        }

        // Blend offsets: average parents, then mutate
        int len = Mathf.Max(a?.learnedOffsets.Length ?? 0, b?.learnedOffsets.Length ?? 0);
        if (len > 0)
        {
            learnedOffsets = new float[len];
            for (int i = 0; i < len; i++)
            {
                float va = a != null && i < a.learnedOffsets.Length ? a.learnedOffsets[i] : 0f;
                float vb = b != null && i < b.learnedOffsets.Length ? b.learnedOffsets[i] : 0f;
                learnedOffsets[i] = (va + vb) * 0.5f;
                if (Random.value < mutationRate)
                    learnedOffsets[i] += Random.Range(-mutationStrength, mutationStrength);
            }
        }
    }

    public void SetOffsets(float[] offsets)
    {
        if (offsets == null || offsets.Length == 0)
        {
            learnedOffsets = new float[0];
            return;
        }
        learnedOffsets = new float[offsets.Length];
        System.Array.Copy(offsets, learnedOffsets, offsets.Length);
    }
}
