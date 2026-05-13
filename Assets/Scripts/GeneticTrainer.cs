using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GeneticTrainer : MonoBehaviour
{
    [Header("Population")]
    [SerializeField] private int populationSize = 10;
    [SerializeField] private int topN = 3;
    [SerializeField] private int generations = 50;
    [SerializeField] private float timePerGeneration = 30f;
    [SerializeField, Range(0f, 1f)] private float mutationRate = 0.15f;
    [SerializeField] private float mutationStrength = 0.3f;

    [Header("References")]
    [SerializeField] private GameObject creaturePrefab;
    [SerializeField] private LevelBounds levelBounds;

    [Header("Cloud Setup")]
    [SerializeField] private int initialRecordCount = 18;
    [SerializeField] private string[] cloudPayloads = new[] { "Wander", "SeekFood", "FleeEnemy", "Attack", "Idle" };

    [Header("Save Session")]
    [SerializeField] private bool saveSessionOnComplete = true;
    [SerializeField] private string sessionName = "";

    [Header("Fitness Weights")]
    [SerializeField] private float fitnessTimeAlive = 1f;
    [SerializeField] private float fitnessDistanceTraveled = 2f;
    [SerializeField] private float fitnessFoodEaten = 2f;
    [SerializeField] private float fitnessEnemyKilled = 3f;
    [SerializeField] private float fitnessAvgEnergy = 1f;
    [SerializeField] private float fitnessAvgHealth = 1f;

    private List<Genome> genomes = new();
    private List<CreatureBrainController> creatures = new();
    private int currentGeneration;
    private float generationTimer;
    private bool isRunning;
    private int weightCount;
    private float spawnY;
    private BehaviorCloudData seedSO;
    private List<BehaviorCloudData> nextSOs = new();
    private string[] behaviorNodeIds;

    private class Genome
    {
        public float[] weights;
        public float fitness;

        public Genome(int count) { weights = new float[count]; }

        public void Randomize(float range)
        {
            for (int i = 0; i < weights.Length; i++)
                weights[i] = Random.Range(-range, range);
        }

        public Genome Clone()
        {
            var g = new Genome(weights.Length);
            System.Array.Copy(weights, g.weights, weights.Length);
            return g;
        }

        public void Mutate(float rate, float strength)
        {
            for (int i = 0; i < weights.Length; i++)
            {
                if (Random.value < rate)
                    weights[i] += Random.Range(-strength, strength);
            }
        }

        public static Genome Crossover(Genome a, Genome b)
        {
            var child = new Genome(a.weights.Length);
            for (int i = 0; i < a.weights.Length; i++)
                child.weights[i] = (a.weights[i] + b.weights[i]) * 0.5f;
            return child;
        }
    }

    private void Start()
    {
        if (creaturePrefab == null || levelBounds == null)
        {
            Debug.LogError("GeneticTrainer: assign creaturePrefab and levelBounds");
            return;
        }

        DecisionBrain templateBrain = creaturePrefab.GetComponentInChildren<DecisionBrain>();
        if (templateBrain == null)
        {
            Debug.LogError("GeneticTrainer: creaturePrefab has no DecisionBrain");
            return;
        }

        weightCount = templateBrain.GetLearnableWeightCount();
        if (weightCount == 0)
        {
            Debug.LogError("GeneticTrainer: no learnable weights found in brain");
            return;
        }

        spawnY = creaturePrefab.transform.position.y;
        if (spawnY == 0f) spawnY = 0.5f;

        // Collect behavior node IDs for generating random records
        var nodeList = new List<string>();
        foreach (var node in templateBrain.BehaviorNodes)
            if (node != null && node.IsEnabled)
                nodeList.Add(node.Id);
        behaviorNodeIds = nodeList.ToArray();

        // Seed SO comes from the prefab — either empty or a pre-trained SO the user assigned
        seedSO = templateBrain.SaveTarget;

        isRunning = true;
        currentGeneration = 0;
        StartGeneration();
    }

    private void Update()
    {
        if (!isRunning) return;
        generationTimer -= Time.deltaTime;
        if (generationTimer <= 0f)
            EndGeneration();
    }

    private void StartGeneration()
    {
        if (currentGeneration >= generations)
        {
            Debug.Log($"GeneticTrainer: completed {generations} generations");
            SaveSessionAssets();
            isRunning = false;
            return;
        }

        if (genomes.Count == 0)
        {
            for (int i = 0; i < populationSize; i++)
            {
                var g = new Genome(weightCount);
                g.Randomize(1f);
                genomes.Add(g);
            }

        }

        SpawnCreatures();
        generationTimer = timePerGeneration;
        Debug.Log($"Gen {currentGeneration + 1}/{generations} starting");
    }

    private void SpawnCreatures()
    {
        ClearCreatures();

        for (int i = 0; i < populationSize; i++)
        {
            Vector3 pos = levelBounds.GetRandomPointInside();
            pos.y = spawnY;

            GameObject go = Instantiate(creaturePrefab, pos, Quaternion.identity);
            go.name = $"Creature_{i}";

            var brain = go.GetComponentInChildren<DecisionBrain>();
            if (brain != null)
            {
                brain.SetLearnableWeights(genomes[i].weights);

                var creatureSO = ScriptableObject.CreateInstance<BehaviorCloudData>();
                creatureSO.name = $"Creature_{i}_Brain";

                if (i < nextSOs.Count && nextSOs[i] != null)
                {
                    creatureSO.CopyFromSO(nextSOs[i]);
                }
                else
                {
                    // Gen 1: each creature gets unique random records for diversity
                    int recsPerPayload = Mathf.Max(1, initialRecordCount / cloudPayloads.Length);
                    for (int p = 0; p < cloudPayloads.Length; p++)
                    {
                        for (int r = 0; r < recsPerPayload; r++)
                        {
                            var rec = new BehaviorRecord($"random_{cloudPayloads[p]}_{i}_{r}", cloudPayloads[p]);
                            foreach (var nodeId in behaviorNodeIds)
                                rec.AddCoordinate(new BehaviorCoordinate(nodeId, Random.Range(0f, 1f), 1f));
                            creatureSO.CopyRecord(rec);
                        }
                    }
                }

                brain.SetSaveTarget(creatureSO);
                brain.SetLoadSource(creatureSO);
                brain.LoadCloud(); // Reload cloud from the new loadSource (overrides Awake's template load)
            }

            var controller = go.GetComponentInChildren<CreatureBrainController>();
            if (controller != null)
            {
                controller.ignoreMaxLifetimes = true;
                controller.SetLevelBounds(levelBounds);
                creatures.Add(controller);
            }
        }
    }

    private void ClearCreatures()
    {
        for (int i = 0; i < creatures.Count; i++)
        {
            if (creatures[i] != null)
                Destroy(creatures[i].gameObject);
        }
        creatures.Clear();
    }

    private void EndGeneration()
    {
        for (int i = 0; i < creatures.Count; i++)
        {
            if (creatures[i] == null) continue;
            var c = creatures[i];
            genomes[i].fitness =
                c.totalTimeAlive * fitnessTimeAlive +
                c.totalDistanceTraveled * fitnessDistanceTraveled +
                c.totalFoodConsumed * fitnessFoodEaten +
                c.totalEnemyKilled * fitnessEnemyKilled +
                c.avgEnergy * fitnessAvgEnergy +
                c.avgHealth * fitnessAvgHealth;
        }

        genomes.Sort((a, b) => b.fitness.CompareTo(a.fitness));

        // Save all top creatures to their private SOs
        var topBrains = new List<DecisionBrain>();
        for (int i = 0; i < Mathf.Min(topN, creatures.Count); i++)
        {
            int idx = genomes.IndexOf(genomes[i]);
            if (idx >= 0 && idx < creatures.Count && creatures[idx] != null)
            {
                var brain = creatures[idx].GetComponentInChildren<DecisionBrain>();
                if (brain != null)
                {
                    brain.SaveToTarget();
                    topBrains.Add(brain);
                }
            }
        }

        Debug.Log($"Gen {currentGeneration + 1}: best={genomes[0].fitness:F2} top={genomes[0].fitness:F2},{genomes[Mathf.Min(1, genomes.Count-1)].fitness:F2},{genomes[Mathf.Min(2, genomes.Count-1)].fitness:F2}");

        currentGeneration++;
        BreedNextGeneration(topBrains);
        ClearCreatures();
        StartGeneration();
    }

    private void BreedNextGeneration(List<DecisionBrain> topBrains)
    {
        // Collect all records from top creatures, sorted by score
        var allRecords = new List<BehaviorRecord>();
        foreach (var brain in topBrains)
        {
            if (brain?.SaveTarget == null) continue;
            foreach (var rec in brain.SaveTarget.Records)
                if (rec != null) allRecords.Add(rec);
        }

        allRecords.Sort((a, b) => b.Score.CompareTo(a.Score));

        // Breed genomes
        var nextGen = new List<Genome>();
        for (int i = 0; i < topN && i < genomes.Count; i++)
            nextGen.Add(genomes[i].Clone());

        while (nextGen.Count < populationSize)
        {
            var parentA = genomes[Random.Range(0, topN)];
            var parentB = genomes[Random.Range(0, topN)];
            var child = Genome.Crossover(parentA, parentB);
            child.Mutate(mutationRate, mutationStrength);
            nextGen.Add(child);
        }
        genomes = nextGen;

        // Collect only positive-scored records from top brains
        var positivePool = new List<BehaviorRecord>();
        int totalRecs = 0;
        int brainsWithRecs = 0;
        foreach (var brain in topBrains)
        {
            if (brain?.SaveTarget == null) continue;
            int count = 0;
            foreach (var rec in brain.SaveTarget.Records)
            {
                if (rec != null && rec.Score > 0f)
                {
                    positivePool.Add(rec);
                    count++;
                }
            }
            if (count > 0) { totalRecs += count; brainsWithRecs++; }
        }

        if (positivePool.Count == 0)
        {
            // Fallback: use all records regardless of score
            foreach (var brain in topBrains)
            {
                if (brain?.SaveTarget == null) continue;
                foreach (var rec in brain.SaveTarget.Records)
                    if (rec != null) positivePool.Add(rec);
            }
        }

        int avgCount = brainsWithRecs > 0 ? totalRecs / brainsWithRecs : initialRecordCount;
        avgCount = Mathf.Clamp(avgCount, 1, initialRecordCount);

        // Build each creature's cloud by randomly sampling + mutating
        nextSOs.Clear();
        for (int c = 0; c < populationSize; c++)
        {
            var so = ScriptableObject.CreateInstance<BehaviorCloudData>();
            so.name = $"GenCloud_{c}";

            for (int r = 0; r < avgCount && positivePool.Count > 0; r++)
            {
                var source = positivePool[Random.Range(0, positivePool.Count)];
                var rec = new BehaviorRecord($"bred_{source.PayloadId}_{c}_{r}", source.PayloadId);
                foreach (var coord in source.Coordinates)
                {
                    float val = coord.Value + Random.Range(-mutationStrength, mutationStrength);
                    rec.AddCoordinate(new BehaviorCoordinate(coord.BehaviorNodeId, Mathf.Clamp01(val), coord.Weight));
                }
                rec.Score = source.Score;
                so.CopyRecord(rec);
            }

            // Ensure minimum coverage: seed missing payloads if cloud is empty
            if (so.RecordCount == 0 && positivePool.Count > 0)
            {
                var any = positivePool[Random.Range(0, positivePool.Count)];
                var fallback = new BehaviorRecord($"seed_{c}_0", any.PayloadId);
                foreach (var coord in any.Coordinates)
                    fallback.AddCoordinate(new BehaviorCoordinate(coord.BehaviorNodeId, coord.Value, coord.Weight));
                fallback.Score = any.Score;
                so.CopyRecord(fallback);
            }

            nextSOs.Add(so);
        }

        // Update seedSO from a random top creature for the next generation
        if (positivePool.Count > 0)
        {
            var bestCloud = new BehaviorCloud();
            int take = Mathf.Min(avgCount, positivePool.Count);
            for (int i = 0; i < take; i++)
            {
                var rec = positivePool[i];
                var copy = new BehaviorRecord(rec.Id, rec.PayloadId);
                foreach (var coord in rec.Coordinates)
                    copy.AddCoordinate(new BehaviorCoordinate(coord.BehaviorNodeId, coord.Value, coord.Weight));
                foreach (var filter in rec.Filters)
                    copy.AddFilter(filter);
                copy.Score = rec.Score;
                bestCloud.AddRecord(copy);
            }
            if (seedSO != null) seedSO.CopyFrom(bestCloud);
        }
    }

    private void SaveSessionAssets()
    {
#if UNITY_EDITOR
        if (!saveSessionOnComplete) return;

        string session = string.IsNullOrWhiteSpace(sessionName)
            ? $"GA_{System.DateTime.Now:yyyyMMdd_HHmmss}"
            : sessionName;
        string folderPath = Path.Combine("Assets", "Brains", session);
        Directory.CreateDirectory(folderPath);

        // Sort creatures by totalTimeAlive as a fitness proxy
        var ranked = new List<(CreatureBrainController c, float score)>();
        for (int i = 0; i < creatures.Count; i++)
        {
            if (creatures[i] != null)
                ranked.Add((creatures[i], creatures[i].totalTimeAlive));
        }
        ranked.Sort((a, b) => b.score.CompareTo(a.score));

        int saveCount = Mathf.Min(topN, ranked.Count);
        for (int i = 0; i < saveCount; i++)
        {
            var brain = ranked[i].c.GetComponentInChildren<DecisionBrain>();
            if (brain?.SaveTarget == null) continue;

            brain.SaveToTarget();
            string path = Path.Combine(folderPath, $"{ranked[i].c.name}_cloud.asset");
            UnityEditor.AssetDatabase.CreateAsset(brain.SaveTarget, path);
        }

        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        Debug.Log($"Saved {saveCount} SO assets to {folderPath}");
#endif
    }
}
