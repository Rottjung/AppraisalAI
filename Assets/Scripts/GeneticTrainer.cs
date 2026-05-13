using System.Collections.Generic;
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

    [Header("Fitness Weights")]
    [SerializeField] private float fitnessTimeAlive = 1f;
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

            // Gen 1: seed the cloud with random records covering all payloads
            GenerateRandomCloud();
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

                // Every creature gets its own private SO — a deep copy of the seed SO
                var creatureSO = ScriptableObject.CreateInstance<BehaviorCloudData>();
                creatureSO.name = $"Creature_{i}_Brain";

                if (i < nextSOs.Count && nextSOs[i] != null)
                    creatureSO.CopyFromSO(nextSOs[i]);
                else if (seedSO != null)
                    creatureSO.CopyFromSO(seedSO);

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

    private void GenerateRandomCloud()
    {
        if (behaviorNodeIds.Length == 0 || cloudPayloads.Length == 0) return;

        // Create a seed SO with random records covering all payloads
        var cloud = new BehaviorCloud();
        int recsPerPayload = Mathf.Max(1, initialRecordCount / cloudPayloads.Length);

        for (int p = 0; p < cloudPayloads.Length; p++)
        {
            for (int r = 0; r < recsPerPayload; r++)
            {
                var rec = new BehaviorRecord($"random_{cloudPayloads[p]}_{r}", cloudPayloads[p]);
                foreach (var nodeId in behaviorNodeIds)
                    rec.AddCoordinate(new BehaviorCoordinate(nodeId, Random.Range(0f, 1f), 1f));
                cloud.AddRecord(rec);
            }
        }

        // Copy to seedSO
        if (seedSO != null)
            seedSO.CopyFrom(cloud);

        Debug.Log($"Generated {cloud.Records.Count} random records across {cloudPayloads.Length} payloads");
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

        // Build nextSOs: each creature gets a cloud from the highest-scored records
        nextSOs.Clear();
        for (int c = 0; c < populationSize; c++)
        {
            var so = ScriptableObject.CreateInstance<BehaviorCloudData>();
            so.name = $"GenCloud_{c}";

            // Take top records directly
            int takeCount = Mathf.Min(initialRecordCount / 2, allRecords.Count);
            for (int i = 0; i < takeCount; i++)
                so.CopyRecord(allRecords[i]);

            // Fill rest with crossbred records from random pairs
            while (so.RecordCount < initialRecordCount && allRecords.Count >= 2)
            {
                var a = allRecords[Random.Range(0, Mathf.Min(topN * 3, allRecords.Count))];
                var b = allRecords[Random.Range(0, Mathf.Min(topN * 3, allRecords.Count))];

                // Crossbreed by taking majority of coordinates from higher-scored parent
                var parentBetter = a.Score >= b.Score ? a : b;
                var parentOther = a.Score >= b.Score ? b : a;

                var childRec = new BehaviorRecord($"bred_{c}_{so.RecordCount}", parentBetter.PayloadId);
                foreach (var coord in parentBetter.Coordinates)
                {
                    float otherVal = 0f;
                    foreach (var oc in parentOther.Coordinates)
                    {
                        if (oc.BehaviorNodeId == coord.BehaviorNodeId)
                        { otherVal = oc.Value; break; }
                    }
                    float val = (coord.Value + otherVal) * 0.5f + Random.Range(-0.1f, 0.1f);
                    childRec.AddCoordinate(new BehaviorCoordinate(coord.BehaviorNodeId, Mathf.Clamp01(val), coord.Weight));
                }
                childRec.Score = (parentBetter.Score + parentOther.Score) * 0.5f;
                so.CopyRecord(childRec);
            }

            nextSOs.Add(so);
        }

        // Update seedSO from best records for the GenerateRandomCloud fallback
        if (allRecords.Count > 0)
        {
            var bestCloud = new BehaviorCloud();
            int take = Mathf.Min(initialRecordCount, allRecords.Count);
            for (int i = 0; i < take; i++)
            {
                var rec = allRecords[i];
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
}
