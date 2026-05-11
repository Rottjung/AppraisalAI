using System;
using UnityEngine;

[Serializable]
public sealed class BehaviorCoordinate
{
    [SerializeField] private string behaviorNodeId;
    [SerializeField] private float value = 0f;
    [SerializeField] private float weight = 1f;

    public string BehaviorNodeId => behaviorNodeId;
    public float Value => value;
    public float Weight => weight;

    public BehaviorCoordinate(string behaviorNodeId, float value, float weight = 1f)
    {
        this.behaviorNodeId = behaviorNodeId;
        this.value = value;
        this.weight = weight;
    }

    public void SetValue(float newValue)
    {
        value = newValue;
    }

    public void SetWeight(float newWeight)
    {
        weight = newWeight;
    }

    public override string ToString()
    {
        return $"{behaviorNodeId}={value:F3} (w={weight:F3})";
    }
}