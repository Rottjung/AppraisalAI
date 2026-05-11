using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LearningController : MonoBehaviour
{
    [SerializeField] private DecisionBrain brain;

    [Header("Signal Providers")]
    [SerializeField] private List<MonoBehaviour> signalProviderBehaviours = new();

    private readonly List<ILearningSignalProvider> signalProviders = new();

    [Header("Episode Definitions")]
    [SerializeField] private List<LearningEpisodeDefinition> episodeDefinitions = new();

    [Header("Learning")]
    [SerializeField] private float behaviorLearningRate = 0.2f;
    [SerializeField] private bool logLearning = true;
    [SerializeField] private TMP_Text Log;
    private readonly Dictionary<string, LearningEpisodeDefinition> episodeLookup = new();

    private bool episodeActive;
    private LearningEpisodeDefinition activeEpisode;
    private LearningSnapshot startSnapshot;

    private readonly Dictionary<string, Dictionary<string, float>> behaviorTraces = new();
    private readonly Dictionary<string, int> behaviorSampleCounts = new();

    private readonly List<Signal> signalBuffer = new();

    private void Awake()
    {
        RebuildProviders();
        RebuildLookup();
    }

    private void OnValidate()
    {
        RebuildProviders();
        RebuildLookup();
    }

    private void RebuildProviders()
    {
        signalProviders.Clear();

        for (int i = 0; i < signalProviderBehaviours.Count; i++)
        {
            if (signalProviderBehaviours[i] is ILearningSignalProvider provider)
            {
                signalProviders.Add(provider);
            }
        }
    }

    private void RebuildLookup()
    {
        episodeLookup.Clear();

        for (int i = 0; i < episodeDefinitions.Count; i++)
        {
            LearningEpisodeDefinition def = episodeDefinitions[i];
            if (def != null && !string.IsNullOrWhiteSpace(def.EpisodeTypeId))
            {
                episodeLookup[def.EpisodeTypeId] = def;
            }
        }
    }

    private LearningSnapshot CaptureSnapshot()
    {
        LearningSnapshot snapshot = new LearningSnapshot();

        signalBuffer.Clear();

        for (int i = 0; i < signalProviders.Count; i++)
        {
            signalProviders[i].CollectSignals(signalBuffer);
        }

        for (int i = 0; i < signalBuffer.Count; i++)
        {
            snapshot.Set(signalBuffer[i]);
        }

        return snapshot;
    }

    public void BeginEpisode(string episodeTypeId)
    {
        if (!episodeLookup.TryGetValue(episodeTypeId, out LearningEpisodeDefinition def))
        {
            Debug.LogWarning($"No episode definition: {episodeTypeId}", this);
            return;
        }

        episodeActive = true;
        activeEpisode = def;
        startSnapshot = CaptureSnapshot();

        behaviorTraces.Clear();
        behaviorSampleCounts.Clear();

        if (logLearning)
        {
           Log.text += $"\nBegin Episode: {episodeTypeId}";
        }
    }

    public void RecordStep()
    {
        if (!episodeActive || brain == null || activeEpisode == null)
        {
            return;
        }

        IReadOnlyList<string> targetBehaviorIds = activeEpisode.TargetBehaviorIds;
        for (int i = 0; i < targetBehaviorIds.Count; i++)
        {
            string behaviorId = targetBehaviorIds[i];
            BehaviorNode node = brain.GetBehaviorNode(behaviorId);
            if (node == null || !node.IsEnabled)
            {
                continue;
            }

            RecordBehaviorTrace(node);
        }
    }

    public void EndEpisode()
    {
        if (!episodeActive || activeEpisode == null)
        {
            return;
        }

        LearningSnapshot endSnapshot = CaptureSnapshot();

        float rawScore = EvaluateReward(startSnapshot, endSnapshot, activeEpisode);
        float normalized = Mathf.Clamp(
            rawScore / Mathf.Max(0.001f, activeEpisode.MaxScoreMagnitudeForLearning),
            -1f,
            1f
        );

        IReadOnlyList<string> targetBehaviorIds = activeEpisode.TargetBehaviorIds;
        for (int i = 0; i < targetBehaviorIds.Count; i++)
        {
            string behaviorId = targetBehaviorIds[i];
            BehaviorNode node = brain.GetBehaviorNode(behaviorId);
            if (node == null || !node.IsEnabled)
            {
                continue;
            }

            ApplyTrace(node, normalized);
        }

        if (logLearning)
        {
            Log.text += $"\nEnd Episode: {activeEpisode.EpisodeTypeId} score={rawScore:F2}";
        }

        episodeActive = false;
        activeEpisode = null;
        behaviorTraces.Clear();
        behaviorSampleCounts.Clear();
    }

    private float EvaluateReward(LearningSnapshot start, LearningSnapshot end, LearningEpisodeDefinition def)
    {
        float score = 0f;

        IReadOnlyList<RewardTermDefinition> rewardTerms = def.RewardTerms;
        for (int i = 0; i < rewardTerms.Count; i++)
        {
            RewardTermDefinition term = rewardTerms[i];
            if (term == null || string.IsNullOrWhiteSpace(term.SignalId))
            {
                continue;
            }

            float startVal = start.Get(term.SignalId);
            float endVal = end.Get(term.SignalId);

            float value = term.Mode switch
            {
                RewardEvaluationMode.StartValue => startVal,
                RewardEvaluationMode.EndValue => endVal,
                RewardEvaluationMode.Delta => endVal - startVal,
                RewardEvaluationMode.AbsoluteDelta => Mathf.Abs(endVal - startVal),
                _ => 0f
            };

            score += value * term.Weight;
        }

        return score;
    }

    private void RecordBehaviorTrace(BehaviorNode node)
    {
        if (!behaviorTraces.TryGetValue(node.Id, out Dictionary<string, float> trace))
        {
            trace = new Dictionary<string, float>();
            behaviorTraces[node.Id] = trace;
        }

        IReadOnlyList<Connection> connections = node.Connections;
        for (int i = 0; i < connections.Count; i++)
        {
            Connection connection = connections[i];
            if (connection == null || !connection.IsEnabled || !connection.IsLearnable)
            {
                continue;
            }

            NodeBase source = brain.GetNode(connection.SourceNodeId);
            if (source == null || !source.IsEnabled)
            {
                continue;
            }

            float value = Mathf.Clamp01(source.OutputValue);

            if (trace.ContainsKey(connection.SourceNodeId))
            {
                trace[connection.SourceNodeId] += value;
            }
            else
            {
                trace[connection.SourceNodeId] = value;
            }
        }

        behaviorSampleCounts[node.Id] = behaviorSampleCounts.TryGetValue(node.Id, out int c) ? c + 1 : 1;
    }

    private void ApplyTrace(BehaviorNode node, float normalizedScore)
    {
        if (!behaviorTraces.TryGetValue(node.Id, out Dictionary<string, float> trace))
        {
            return;
        }

        if (!behaviorSampleCounts.TryGetValue(node.Id, out int count) || count <= 0)
        {
            return;
        }

        IReadOnlyList<Connection> connections = node.Connections;
        for (int i = 0; i < connections.Count; i++)
        {
            Connection connection = connections[i];
            if (connection == null || !connection.IsEnabled || !connection.IsLearnable)
            {
                continue;
            }

            if (!trace.TryGetValue(connection.SourceNodeId, out float sum))
            {
                continue;
            }

            float avg = sum / count;
            float delta = behaviorLearningRate * normalizedScore * avg;

            connection.AddToLearnedOffset(delta);
        }
    }

    internal void ToggleLog()
    {
        logLearning = !logLearning;
    }
}