using System;
using UnityEngine;

[Serializable]
public abstract class NodeBase
{
    [SerializeField] protected string id;
    [SerializeField] protected string displayName;
    [SerializeField] protected bool isEnabled = true;

    public string Id => id;
    public string DisplayName => displayName;
    public bool IsEnabled => isEnabled;

    public abstract float OutputValue { get; }

    protected NodeBase(string id, string displayName)
    {
        this.id = id;
        this.displayName = displayName;
    }

    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;
    }

    public override string ToString()
    {
        return $"{displayName} ({id})";
    }
}
