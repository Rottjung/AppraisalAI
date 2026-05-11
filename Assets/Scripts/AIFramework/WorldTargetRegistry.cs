using System;
using System.Collections.Generic;

public static class WorldTargetRegistry
{
    private static readonly Dictionary<string, List<WorldTarget>> targetsByTag = new();

    private static readonly List<WorldTarget> allTargets = new();

    public static IReadOnlyList<WorldTarget> GetTargetsByTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return Array.Empty<WorldTarget>();
        }
        return targetsByTag.TryGetValue(tag, out List<WorldTarget> list) ? list : Array.Empty<WorldTarget>();
    }

    public static IReadOnlyList<WorldTarget> AllTargets => allTargets;

    public static void Register(WorldTarget target)
    {
        if (target == null)
        {
            return;
        }
        string tag = target.gameObject.tag;
        if (!allTargets.Contains(target))
        {
            allTargets.Add(target);
            TargetRegistered?.Invoke(target);
        }

        if (!string.IsNullOrWhiteSpace(tag))
        {
            if (!targetsByTag.TryGetValue(tag, out List<WorldTarget> list))
            {
                list = new List<WorldTarget>();
                targetsByTag[tag] = list;
            }
            if (!list.Contains(target))
            {
                list.Add(target);
            }
        }
    }

    public static void Unregister(WorldTarget target)
    {
        if (target == null)
        {
            return;
        }
        string tag = target.gameObject.tag;
        if (allTargets.Remove(target))
        {
            TargetUnregistered?.Invoke(target);
        }
        if (!string.IsNullOrWhiteSpace(tag) && targetsByTag.TryGetValue(tag, out List<WorldTarget> list))
        {
            list.Remove(target);
            if (list.Count == 0)
            {
                targetsByTag.Remove(tag);
            }
        }
    }

    public static event Action<WorldTarget> TargetRegistered;

    public static event Action<WorldTarget> TargetUnregistered;

}