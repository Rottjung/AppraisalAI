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
