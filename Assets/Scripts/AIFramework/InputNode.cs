using System;
using System.Collections.Generic;
using UnityEngine;

public enum InputValueType
{
    Float,
    Bool,
    Int
}

[Serializable]
public sealed class InputNode : NodeBase
{
    [SerializeField] private InputValueType valueType = InputValueType.Float;

    [Header("Normalization")]
    [SerializeField] private float minValue = 0f;
    [SerializeField] private float maxValue = 1f;
    [SerializeField] private bool clampNormalized = true;

    [Header("Runtime")]
    [SerializeField] private float rawValue;
    [SerializeField] private float normalizedValue;

    public InputValueType ValueType => valueType;
    public float RawValue => rawValue;
    public float NormalizedValue => normalizedValue;
    public float MinValue => minValue;
    public float MaxValue => maxValue;

    public override float OutputValue => normalizedValue;

    public InputNode(
        string id,
        string displayName,
        InputValueType valueType = InputValueType.Float,
        float minValue = 0f,
        float maxValue = 1f) : base(id, displayName)
    {
        this.valueType = valueType;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }

    public void SetRawValue(float value)
    {
        rawValue = value;
        normalizedValue = Normalize(value);
    }

    public void SetBoolValue(bool value)
    {
        rawValue = value ? 1f : 0f;
        normalizedValue = rawValue;
    }

    public void SetIntValue(int value)
    {
        rawValue = value;
        normalizedValue = Normalize(value);
    }

    public void Recalculate()
    {
        normalizedValue = Normalize(rawValue);
    }

    private float Normalize(float value)
    {
        return MathUtil.Normalize(value, minValue, maxValue, clampNormalized);
    }

    public override string ToString()
    {
        return $"{base.ToString()} Raw={rawValue:F3}, Normalized={normalizedValue:F3}";
    }
}
