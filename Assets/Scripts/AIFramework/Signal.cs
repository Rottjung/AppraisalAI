using System;
using UnityEngine;

public enum SignalType
{
    Float,
    Int,
    Bool
}

[Serializable]
public struct Signal
{
    [SerializeField] private string id;
    [SerializeField] private SignalType type;
    [SerializeField] private float value;

    public string Id => id;
    public SignalType Type => type;
    public float Value => value;

    public Signal(string id, SignalType type, float value = 0f)
    {
        this.id = id;
        this.type = type;
        this.value = NormalizeValue(type, value);
    }

    public void SetValue(float newValue)
    {
        value = NormalizeValue(type, newValue);
    }

    public void AddValue(float delta)
    {
        switch (type)
        {
            case SignalType.Bool:
                value = NormalizeValue(type, delta);
                break;

            case SignalType.Int:
            case SignalType.Float:
            default:
                value = NormalizeValue(type, value + delta);
                break;
        }
    }

    public int GetInt()
    {
        return Mathf.RoundToInt(value);
    }

    public bool GetBool()
    {
        return value >= 0.5f;
    }

    public static float NormalizeValue(SignalType type, float raw)
    {
        switch (type)
        {
            case SignalType.Bool:
                return raw > 0f ? 1f : 0f;

            case SignalType.Int:
                return Mathf.Round(raw);

            case SignalType.Float:
            default:
                return raw;
        }
    }
}