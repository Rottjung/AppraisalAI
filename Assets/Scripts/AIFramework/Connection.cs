using System;
using UnityEngine;

[Serializable]
public sealed class Connection
{
    [SerializeField] private string sourceNodeId;
    [SerializeField] private float weight = 1f;
    [SerializeField] private bool isEnabled = true;

    [Header("Adaptive Weight")]
    [SerializeField] private bool isLearnable = true;
    [SerializeField] private float learnedOffset = 0f;
    [SerializeField] private float minEffectiveWeight = -2f;
    [SerializeField] private float maxEffectiveWeight = 2f;

    public string SourceNodeId => sourceNodeId;
    public float Weight => weight;
    public bool IsEnabled => isEnabled;

    public bool IsLearnable => isLearnable;
    public float LearnedOffset => learnedOffset;
    public float MinEffectiveWeight => minEffectiveWeight;
    public float MaxEffectiveWeight => maxEffectiveWeight;

    public float EffectiveWeight => Mathf.Clamp(weight + learnedOffset, minEffectiveWeight, maxEffectiveWeight);

    public Connection(string sourceNodeId, float weight = 1f)
    {
        this.sourceNodeId = sourceNodeId;
        this.weight = weight;
    }

    public void SetWeight(float newWeight)
    {
        weight = newWeight;
        learnedOffset = GetClampedOffset(learnedOffset);
    }

    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
    }

    public void SetLearnable(bool learnable)
    {
        isLearnable = learnable;
    }

    public void SetLearnedOffset(float newOffset)
    {
        learnedOffset = GetClampedOffset(newOffset);
    }

    public void AddToLearnedOffset(float delta)
    {
        if (!isLearnable)
        {
            return;
        }

        learnedOffset = GetClampedOffset(learnedOffset + delta);
    }

    public void ResetLearnedOffset()
    {
        learnedOffset = 0f;
    }

    public void SetEffectiveWeightLimits(float minWeight, float maxWeight)
    {
        if (maxWeight < minWeight)
        {
            float temp = minWeight;
            minWeight = maxWeight;
            maxWeight = temp;
        }

        minEffectiveWeight = minWeight;
        maxEffectiveWeight = maxWeight;
        learnedOffset = GetClampedOffset(learnedOffset);
    }

    public float Evaluate(NodeBase sourceNode)
    {
        if (!isEnabled || sourceNode == null || !sourceNode.IsEnabled)
        {
            return 0f;
        }

        return sourceNode.OutputValue * EffectiveWeight;
    }

    private float GetClampedOffset(float candidateOffset)
    {
        float minOffset = minEffectiveWeight - weight;
        float maxOffset = maxEffectiveWeight - weight;
        return Mathf.Clamp(candidateOffset, minOffset, maxOffset);
    }

    public override string ToString()
    {
        return $"Source={sourceNodeId}, Base={weight:F3}, LearnedOffset={learnedOffset:F3}, Effective={EffectiveWeight:F3}, Enabled={isEnabled}, Learnable={isLearnable}";
    }
}