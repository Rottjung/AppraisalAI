using System.Collections.Generic;
using UnityEngine;

public sealed class DecisionBrain : MonoBehaviour
{
    [Header("Layer Sizes")]
    [SerializeField] private int inputCount = 3;
    [SerializeField] private int featureCount = 0;
    [SerializeField] private int behaviorCount = 3;

    [Header("Layers")]
    [SerializeField] private List<InputNode> inputNodes = new();
    [SerializeField] private List<FeatureNode> featureNodes = new();
    [SerializeField] private List<BehaviorNode> behaviorNodes = new();
    [SerializeField] private BehaviorCloud behaviorCloud = new();

    [Header("Cloud Persistence")]
    [SerializeField] private bool saveOnQuit = true;
    [SerializeField] private BehaviorCloudData saveTarget;
    [SerializeField] private BehaviorCloudData loadSource;

    private readonly Dictionary<string, NodeBase> nodeLookup = new();
    private readonly Dictionary<string, BehaviorNode> behaviorLookup = new();
    private readonly Dictionary<string, InputNode> inputLookup = new();
    private readonly Dictionary<string, FeatureNode> featureLookup = new();

    public IReadOnlyList<InputNode> InputNodes => inputNodes;
    public IReadOnlyList<FeatureNode> FeatureNodes => featureNodes;
    public IReadOnlyList<BehaviorNode> BehaviorNodes => behaviorNodes;
    public BehaviorCloud Cloud => behaviorCloud;
    public BehaviorCloudData SaveTarget => saveTarget;

    private void Awake()
    {
        RebuildCaches();
        if (loadSource != null)
            LoadFromSource();
    }

    private void OnValidate()
    {
        RebuildCaches();
    }

    private void Reset()
    {
        BuildEmptyFramework();
    }

    [ContextMenu("Build Empty Framework")]
    public void BuildEmptyFramework()
    {
        inputNodes.Clear();
        featureNodes.Clear();
        behaviorNodes.Clear();

        for (int i = 0; i < inputCount; i++)
        {
            inputNodes.Add(new InputNode(
                id: $"input_{i}",
                displayName: $"Input {i}",
                valueType: InputValueType.Float,
                minValue: 0f,
                maxValue: 1f));
        }

        for (int i = 0; i < featureCount; i++)
        {
            featureNodes.Add(new FeatureNode(
                id: $"feature_{i}",
                displayName: $"Feature {i}"));
        }

        for (int i = 0; i < behaviorCount; i++)
        {
            behaviorNodes.Add(new BehaviorNode(
                id: $"behavior_{i}",
                displayName: $"Behavior {i}"));
        }

        if (behaviorCloud == null)
        {
            behaviorCloud = new BehaviorCloud();
        }
        else
        {
            behaviorCloud.Clear();
        }

        RebuildCaches();
    }

    public void AddInputNode(InputNode node)
    {
        if (node == null) return;
        inputNodes.Add(node);
        RebuildCaches();
    }

    public int GetLearnableWeightCount()
    {
        int count = 0;
        for (int b = 0; b < behaviorNodes.Count; b++)
        {
            var conns = behaviorNodes[b].Connections;
            for (int c = 0; c < conns.Count; c++)
            {
                if (conns[c] != null && conns[c].IsLearnable)
                    count++;
            }
        }
        return count;
    }

    public void GetLearnableWeights(float[] buffer)
    {
        int idx = 0;
        for (int b = 0; b < behaviorNodes.Count; b++)
        {
            var conns = behaviorNodes[b].Connections;
            for (int c = 0; c < conns.Count; c++)
            {
                var conn = conns[c];
                if (conn != null && conn.IsLearnable)
                    buffer[idx++] = conn.Weight;
            }
        }
    }

    public void SetLearnableWeights(float[] weights)
    {
        int idx = 0;
        for (int b = 0; b < behaviorNodes.Count; b++)
        {
            var conns = behaviorNodes[b].Connections;
            for (int c = 0; c < conns.Count; c++)
            {
                var conn = conns[c];
                if (conn != null && conn.IsLearnable)
                    conn.SetWeight(weights[idx++]);
            }
        }
    }

    public void GetLearnableOffsets(float[] buffer)
    {
        int idx = 0;
        for (int b = 0; b < behaviorNodes.Count; b++)
        {
            var conns = behaviorNodes[b].Connections;
            for (int c = 0; c < conns.Count; c++)
            {
                var conn = conns[c];
                if (conn != null && conn.IsLearnable)
                    buffer[idx++] = conn.LearnedOffset;
            }
        }
    }

    public void SetLearnableOffsets(float[] offsets)
    {
        int idx = 0;
        for (int b = 0; b < behaviorNodes.Count; b++)
        {
            var conns = behaviorNodes[b].Connections;
            for (int c = 0; c < conns.Count; c++)
            {
                var conn = conns[c];
                if (conn != null && conn.IsLearnable)
                    conn.SetLearnedOffset(offsets[idx++]);
            }
        }
    }

    public void AddBehaviorNode(BehaviorNode node)
    {
        if (node == null) return;
        behaviorNodes.Add(node);
        RebuildCaches();
    }

    [ContextMenu("Rebuild Caches")]
    public void RebuildCaches()
    {
        nodeLookup.Clear();
        behaviorLookup.Clear();
        inputLookup.Clear();
        featureLookup.Clear();

        for (int i = 0; i < inputNodes.Count; i++)
        {
            InputNode node = inputNodes[i];
            if (node == null || string.IsNullOrEmpty(node.Id))
            {
                continue;
            }

            inputLookup[node.Id] = node;
            nodeLookup[node.Id] = node;
        }

        for (int i = 0; i < featureNodes.Count; i++)
        {
            FeatureNode node = featureNodes[i];
            if (node == null || string.IsNullOrEmpty(node.Id))
            {
                continue;
            }

            featureLookup[node.Id] = node;
            nodeLookup[node.Id] = node;
        }

        for (int i = 0; i < behaviorNodes.Count; i++)
        {
            BehaviorNode node = behaviorNodes[i];
            if (node == null || string.IsNullOrEmpty(node.Id))
            {
                continue;
            }

            behaviorLookup[node.Id] = node;
            nodeLookup[node.Id] = node;
        }
    }

    public NodeBase GetNode(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        nodeLookup.TryGetValue(id, out NodeBase node);
        return node;
    }

    public InputNode GetInputNode(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        inputLookup.TryGetValue(id, out InputNode node);
        return node;
    }

    public FeatureNode GetFeatureNode(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        featureLookup.TryGetValue(id, out FeatureNode node);
        return node;
    }

    public BehaviorNode GetBehaviorNode(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        behaviorLookup.TryGetValue(id, out BehaviorNode node);
        return node;
    }

    public float GetBehaviorValue(string id)
    {
        BehaviorNode node = GetBehaviorNode(id);
        return node != null ? node.Value : 0f;
    }

    public RetrievalCandidate QueryCloud()
    {
        if (behaviorCloud == null)
        {
            return null;
        }

        return behaviorCloud.GetNearest(this);
    }

    public List<RetrievalCandidate> QueryCloudCandidates()
    {
        if (behaviorCloud == null)
        {
            return new List<RetrievalCandidate>();
        }

        return behaviorCloud.GetCandidates(this);
    }

    public void SetInputRaw(string id, float value)
    {
        InputNode node = GetInputNode(id);
        if (node == null)
        {
            Debug.LogWarning($"Input node not found: {id}", this);
            return;
        }

        node.SetRawValue(value);
    }

    [ContextMenu("Evaluate Features")]
    public void EvaluateFeatures()
    {
        for (int i = 0; i < featureNodes.Count; i++)
        {
            featureNodes[i].Evaluate(this);
        }
    }

    [ContextMenu("Evaluate Behaviors")]
    public void EvaluateBehaviors()
    {
        for (int i = 0; i < behaviorNodes.Count; i++)
        {
            behaviorNodes[i].Evaluate(this);
        }
    }

    [ContextMenu("Evaluate All")]
    public void EvaluateAll()
    {
        EvaluateFeatures();
        EvaluateBehaviors();
    }

    [ContextMenu("Debug Print Inputs")]
    public void DebugPrintInputs()
    {
        for (int i = 0; i < inputNodes.Count; i++)
        {
            Debug.Log(inputNodes[i].ToString(), this);
        }
    }

    [ContextMenu("Debug Print Features")]
    public void DebugPrintFeatures()
    {
        for (int i = 0; i < featureNodes.Count; i++)
        {
            Debug.Log(featureNodes[i].ToString(), this);
        }
    }

    [ContextMenu("Debug Print Behaviors")]
    public void DebugPrintBehaviors()
    {
        for (int i = 0; i < behaviorNodes.Count; i++)
        {
            Debug.Log(behaviorNodes[i].ToString(), this);
        }
    }

    [ContextMenu("Debug Query Cloud")]
    public void DebugQueryCloud()
    {
        RetrievalCandidate candidate = QueryCloud();

        if (candidate == null)
        {
            Debug.Log("Cloud returned no candidate.", this);
            return;
        }

        Debug.Log(candidate.ToString(), this);
    }

    public void SaveToTarget()
    {
        if (saveTarget == null) return;
        saveTarget.CopyFrom(behaviorCloud);

        float[] offsets = new float[GetLearnableWeightCount()];
        GetLearnableOffsets(offsets);
        saveTarget.SetOffsets(offsets);

        Debug.Log($"Saved to {saveTarget.name}: {behaviorCloud.Records.Count} records, {offsets.Length} offsets", this);
    }

    private void LoadFromSource()
    {
        if (loadSource == null) return;
        loadSource.CopyTo(behaviorCloud);

        var offsets = loadSource.LearnedOffsets;
        if (offsets.Length > 0)
            SetLearnableOffsets(offsets);

        Debug.Log($"Loaded from {loadSource.name}: {loadSource.Records.Count} records, {offsets.Length} offsets", this);
    }

    private void OnApplicationQuit()
    {
        if (saveOnQuit && saveTarget != null)
            SaveToTarget();
    }

    [ContextMenu("Save to Target")]
    public void SaveCloud() { SaveToTarget(); }

    [ContextMenu("Load from Source")]
    public void LoadCloud() { LoadFromSource(); }
}