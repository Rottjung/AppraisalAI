using System.Collections.Generic;
using UnityEngine;

public class LearningState : MonoBehaviour
{
    [SerializeField] private List<Signal> signals = new();

    private readonly Dictionary<string, int> indexById = new();
    private readonly Dictionary<string, float> valueById = new();

    public IReadOnlyList<Signal> Signals => signals;
    public IReadOnlyDictionary<string, float> Values => valueById;

    private void Awake()
    {
        RebuildCache();
    }

    private void OnValidate()
    {
        RebuildCache();
    }

    [ContextMenu("Rebuild Cache")]
    public void RebuildCache()
    {
        indexById.Clear();
        valueById.Clear();

        for (int i = 0; i < signals.Count; i++)
        {
            Signal signal = signals[i];

            if (string.IsNullOrWhiteSpace(signal.Id))
            {
                continue;
            }

            if (indexById.ContainsKey(signal.Id))
            {
                Debug.LogWarning($"Duplicate Signal id '{signal.Id}' on {name}. Keeping last occurrence.", this);
            }

            indexById[signal.Id] = i;
            valueById[signal.Id] = signal.Value;
        }
    }

    public bool HasSignal(string id)
    {
        return !string.IsNullOrWhiteSpace(id) && indexById.ContainsKey(id);
    }

    public float GetValue(string id, float fallback = 0f)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return fallback;
        }

        return valueById.TryGetValue(id, out float value) ? value : fallback;
    }

    public int GetInt(string id, int fallback = 0)
    {
        if (!indexById.TryGetValue(id, out int index))
        {
            return fallback;
        }

        return signals[index].GetInt();
    }

    public bool GetBool(string id, bool fallback = false)
    {
        if (!indexById.TryGetValue(id, out int index))
        {
            return fallback;
        }

        return signals[index].GetBool();
    }

    public bool TryGetSignal(string id, out Signal signal)
    {
        if (!indexById.TryGetValue(id, out int index))
        {
            signal = default;
            return false;
        }

        signal = signals[index];
        return true;
    }

    public bool SetSignal(string id, float value)
    {
        if (!indexById.TryGetValue(id, out int index))
        {
            return false;
        }

        Signal signal = signals[index];
        signal.SetValue(value);
        signals[index] = signal;
        valueById[id] = signal.Value;
        return true;
    }

    public bool ModifySignal(string id, float value, bool additive)
    {
        if (!indexById.TryGetValue(id, out int index))
        {
            return false;
        }

        Signal signal = signals[index];

        if (additive)
        {
            signal.AddValue(value);
        }
        else
        {
            signal.SetValue(value);
        }

        signals[index] = signal;
        valueById[id] = signal.Value;
        return true;
    }

    public bool Apply(string id, float value, bool additive = true)
    {
        return ModifySignal(id, value, additive);
    }

    public bool EnsureSignal(string id, SignalType type, float initialValue = 0f)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        if (indexById.ContainsKey(id))
        {
            return true;
        }

        Signal signal = new Signal(id, type, initialValue);
        signals.Add(signal);

        int index = signals.Count - 1;
        indexById[id] = index;
        valueById[id] = signal.Value;
        return true;
    }

    public void ResetAllToZero()
    {
        for (int i = 0; i < signals.Count; i++)
        {
            Signal signal = signals[i];
            signal.SetValue(0f);
            signals[i] = signal;

            if (!string.IsNullOrWhiteSpace(signal.Id))
            {
                valueById[signal.Id] = signal.Value;
            }
        }
    }
}