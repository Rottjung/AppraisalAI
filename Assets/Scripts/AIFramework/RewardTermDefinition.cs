using System;
using UnityEngine;

public enum RewardEvaluationMode
{
    StartValue,
    EndValue,
    Delta,
    AbsoluteDelta
}

[Serializable]
public sealed class RewardTermDefinition
{
    [SerializeField] private string signalId;
    [SerializeField] private RewardEvaluationMode mode;
    [SerializeField] private float weight = 1f;

    public string SignalId => signalId;
    public RewardEvaluationMode Mode => mode;
    public float Weight => weight;

    public RewardTermDefinition() { }

    public RewardTermDefinition(string signalId, RewardEvaluationMode mode, float weight = 1f)
    {
        this.signalId = signalId;
        this.mode = mode;
        this.weight = weight;
    }
}