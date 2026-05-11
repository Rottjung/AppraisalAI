using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LearningEpisodeDefinition
{
    [SerializeField] private string episodeTypeId;
    [SerializeField] private List<string> targetBehaviorIds = new();
    [SerializeField] private List<RewardTermDefinition> rewardTerms = new();
    [SerializeField] private float maxScoreMagnitudeForLearning = 3f;

    public string EpisodeTypeId => episodeTypeId;
    public IReadOnlyList<string> TargetBehaviorIds => targetBehaviorIds;
    public IReadOnlyList<RewardTermDefinition> RewardTerms => rewardTerms;
    public float MaxScoreMagnitudeForLearning => maxScoreMagnitudeForLearning;

    public void Initialize(string id, List<string> behaviorIds, List<RewardTermDefinition> terms, float maxScore = 3f)
    {
        episodeTypeId = id;
        targetBehaviorIds = behaviorIds;
        rewardTerms = terms;
        maxScoreMagnitudeForLearning = maxScore;
    }
}
