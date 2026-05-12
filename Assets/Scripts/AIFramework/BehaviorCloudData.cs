using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AppraisalAI/Behavior Cloud Data")]
public class BehaviorCloudData : ScriptableObject
{
    [SerializeField] private List<BehaviorRecord> records = new();

    public IReadOnlyList<BehaviorRecord> Records => records;

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
}
