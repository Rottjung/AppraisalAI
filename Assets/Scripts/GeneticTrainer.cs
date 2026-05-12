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
            Debug.LogError("GeneticTrainer: creaturePrefab has no DecisionBrain component");
            return;
        }

        weightCount = templateBrain.GetLearnableWeightCount();
        if (weightCount == 0)
        {
            Debug.LogError("GeneticTrainer: no learnable weights found in brain");
            return;
        }

        spawnY = creaturePrefab.transform.position.y;
        if (spawnY == 0f)
            spawnY = 0.5f;

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
                brain.SetLearnableWeights(genomes[i].weights);

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
                c.totalFoodConsumed * fitnessFoodEaten +
                c.totalEnemyKilled * fitnessEnemyKilled +
                c.avgEnergy * fitnessAvgEnergy +
                c.avgHealth * fitnessAvgHealth;
        }

        genomes.Sort((a, b) => b.fitness.CompareTo(a.fitness));

        Debug.Log($"Gen {currentGeneration + 1}: best={genomes[0].fitness:F2} top={string.Join(", ", genomes.GetRange(0, Mathf.Min(topN, genomes.Count)).ConvertAll(g => g.fitness.ToString("F2")))}");

        currentGeneration++;
        BreedNextGeneration();
        ClearCreatures();
        StartGeneration();
    }

    private void BreedNextGeneration()
    {
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
    }
}
