using UnityEngine;
using System.Collections.Generic;

public class FoodSpawner : MonoBehaviour
{
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private LevelBounds levelBounds;

    [Header("Spawn Timing")]
    [SerializeField] private float spawnInterval = 4f;

    [Header("Spawn Rules")]
    [SerializeField] private int maxFood = 25;
    [SerializeField] private float spawnRadius = 6f;
    [SerializeField] private float minDistance = 2f;

    [Header("Scatter")]
    [SerializeField] private bool chainFromPrevious = true;

    private float timer;
    private Vector3? previousSpawnPoint;

    private readonly List<GameObject> activeFood = new();

    private void Update()
    {
        if (foodPrefab == null || levelBounds == null)
            return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            timer += spawnInterval;

            CleanupList();

            if (activeFood.Count < maxFood)
            {
                SpawnFood();
            }
        }
    }

    private void SpawnFood()
    {
        Bounds bounds = levelBounds.GetWorldBounds();

        Vector3 spawnPoint;

        if (chainFromPrevious && previousSpawnPoint.HasValue)
        {
            spawnPoint = MathUtil.RandomPointInBounds(
                bounds,
                previousSpawnPoint.Value,
                spawnRadius,
                minDistance
            );
        }
        else
        {
            spawnPoint = MathUtil.RandomPointInBounds(bounds);
        }

        spawnPoint = levelBounds.ClampPointInside(spawnPoint);

        GameObject food = Instantiate(foodPrefab, spawnPoint, Quaternion.identity);
        activeFood.Add(food);

        previousSpawnPoint = spawnPoint;
    }

    private void CleanupList()
    {
        for (int i = activeFood.Count - 1; i >= 0; i--)
        {
            if (activeFood[i] == null)
            {
                activeFood.RemoveAt(i);
            }
        }
    }
}