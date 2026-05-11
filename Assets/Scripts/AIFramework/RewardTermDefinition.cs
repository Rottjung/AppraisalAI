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
}