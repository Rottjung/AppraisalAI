using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FeatureNode : NodeBase
{
    [Header("Feature Settings")]
    [SerializeField] private float bias = 0f;
    [SerializeField] private ActivationType activationType = ActivationType.Clamped01;

    [Header("Runtime")]
    [SerializeField] private float rawValue;
    [SerializeField] private float value;

    [Header("Incoming Links")]
    [SerializeField] private List<Connection> connections = new();

    private readonly List<MathUtil.Contribution> debugContributions = new();

    public float Bias => bias;
    public ActivationType ActivationType => activationType;
    public float RawValue => rawValue;
    public float Value => value;
    public IReadOnlyList<Connection> Connections => connections;

    public override float OutputValue => value;

    public FeatureNode(string id, string displayName) : base(id, displayName) { }

    public void SetBias(float newBias)
    {
        bias = newBias;
    }

    public void SetActivationType(ActivationType type)
    {
        activationType = type;
    }

    public void AddConnection(Connection connection)
    {
        if (connection != null)
            connections.Add(connection);
    }

    public void ClearConnections()
    {
        connections.Clear();
    }

    public void Evaluate(DecisionBrain brain, bool debug = false)
    {
        if (!IsEnabled)
        {
            rawValue = 0;
            value = 0;
            return;
        }

        rawValue = MathUtil.Summation(
            brain,
            connections,
            bias,
            debug ? debugContributions : null
        );

        value = MathUtil.ApplyActivation(rawValue, activationType);
    }

    public string GetDebugBreakdown()
    {
        System.Text.StringBuilder sb = new();

        sb.AppendLine(DisplayName);

        foreach (var c in debugContributions)
        {
            sb.AppendLine(c.ToString());
        }

        sb.AppendLine($"Bias = {bias:F3}");
        sb.AppendLine($"Raw = {rawValue:F3}");
        sb.AppendLine($"Activated = {value:F3}");

        return sb.ToString();
    }
}
