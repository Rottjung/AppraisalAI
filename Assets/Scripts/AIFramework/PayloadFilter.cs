using System;

public enum ComparisonType
{
    Equal,
    NotEqual,
    Greater,
    GreaterOrEqual,
    Less,
    LessOrEqual
}
[Serializable]
public struct PayloadFilter
{
    public string signalId;
    public ComparisonType comparison;
    public float value;
}