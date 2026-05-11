using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class Sensors : MonoBehaviour
{
    [SerializeField] private DecisionBrain brain;

    [Header("Signals")]
    [SerializeField] private List<Signal> signals = new();

    [Header("Sensor Modules")]
    [Tooltip("List of sensor configurations. Each sensor writes a value into the signals list.")]
    [SerializeField] private List<Sensor> sensorModules = new();

    [Header("Debug")]
    [SerializeField] private bool logSensors = false;

    private readonly Dictionary<string, int> indexById = new();
    private readonly Dictionary<string, float> valueById = new();

    public DecisionBrain Brain => brain;

    public IReadOnlyList<Signal> Signals => signals;

    public List<Sensor> SensorModules => sensorModules;

    private void Awake()
    {
        RebuildCache();
        InitializeSensors();
    }

    private void OnValidate()
    {
        RebuildCache();
        InitializeSensors();
    }

    private void InitializeSensors()
    {
        for (int i = 0; i < sensorModules.Count; i++)
        {
            Sensor sensor = sensorModules[i];
            if (sensor == null) continue;

            var (signalId, initialValue) = sensor.Initialize();
            EnsureSignal(signalId, initialValue);
        }
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
        Signal s = signals[index];
        s.SetValue(value);
        signals[index] = s;
        valueById[id] = s.Value;
        return true;
    }

    public bool ModifySignal(string id, float value, bool additive)
    {
        if (!indexById.TryGetValue(id, out int index))
        {
            return false;
        }
        Signal s = signals[index];
        if (additive)
        {
            s.AddValue(value);
        }
        else
        {
            s.SetValue(value);
        }
        signals[index] = s;
        valueById[id] = s.Value;
        return true;
    }

    public bool Apply(string id, float value, bool additive = true)
    {
        return ModifySignal(id, value, additive);
    }

    public void ApplyToSignal(string id, float delta)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }
        foreach (var sensor in sensorModules)
        {
            if (sensor != null && sensor.SignalId == id)
            {
                sensor.ApplyStateDelta(delta, this);
                return;
            }
        }
        if (ModifySignal(id, delta, true))
        {
            float current = GetValue(id);
            SetSignal(id, Mathf.Clamp01(current));
        }
    }

    public bool EnsureSignal(string id, float initialValue = 0f)
    {
        if (string.IsNullOrWhiteSpace(id))
            return false;

        if (indexById.ContainsKey(id))
            return true;

        var signal = new Signal(id, SignalType.Float, initialValue);
        signals.Add(signal);

        indexById[id] = signals.Count - 1;

        return true;
    }

    public void Sense()
    {
        float dt = Time.deltaTime;

        for (int i = 0; i < sensorModules.Count; i++)
        {
            Sensor sensor = sensorModules[i];
            if (sensor == null) continue;

            var (id, value) = sensor.Update(dt, transform);
            if (string.IsNullOrWhiteSpace(id)) continue;

            SetSignal(id, value);

            if (brain != null)
            {
                brain.SetInputRaw(id, value);
            }
        }
    
        if (logSensors)
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            for (int i = 0; i < sensorModules.Count; i++)
            {
                Sensor sensor = sensorModules[i];
                if (sensor == null || string.IsNullOrWhiteSpace(sensor.SignalId))
                {
                    continue;
                }
                float val = GetValue(sensor.SignalId);
                if (!first) sb.Append(" | ");
                sb.Append($"{sensor.SignalId}={val:F2}");
                first = false;
            }
            if (sb.Length > 0)
            {
                Debug.Log(sb.ToString(), this);
            }
        }
    }
    public Sensor GetSensor(string signalId)
    {
        if (string.IsNullOrWhiteSpace(signalId))
            return null;

        for (int i = 0; i < sensorModules.Count; i++)
        {
            Sensor sensor = sensorModules[i];
            if (sensor != null && sensor.SignalId == signalId)
                return sensor;
        }

        return null;
    }
}