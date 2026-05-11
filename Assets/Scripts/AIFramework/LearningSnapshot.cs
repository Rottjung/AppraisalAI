using System.Collections.Generic;

public sealed class LearningSnapshot
{
    private readonly Dictionary<string, float> values = new();

    public void Set(string id, float value)
    {
        values[id] = value;
    }

    public void Set(Signal signal)
    {
        values[signal.Id] = signal.Value;
    }

    public float Get(string id)
    {
        return values.TryGetValue(id, out float v) ? v : 0f;
    }

    public IReadOnlyDictionary<string, float> Values => values;
}

public interface ILearningSignalProvider
{
    void CollectSignals(List<Signal> signals);
}