using UnityEngine;
using System.Collections.Generic;

public class Spawner : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private LevelBounds levelBounds;

    [Header("Spawn Timing")]
    [SerializeField] private float spawnInterval = 4f;

    [Header("Spawn Rules")]
    [SerializeField] private int maxAmount = 25;
    [SerializeField] private float spawnRadius = 6f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float YOffset = 0.5f;


    [Header("Scatter")]
    [SerializeField] private bool chainFromPrevious = true;

    private float timer;
    private Vector3? previousSpawnPoint;

    private readonly List<GameObject> activeObjects = new();

    private void Update()
    {
        if (prefab == null || levelBounds == null)
            return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            timer += spawnInterval;

            CleanupList();

            if (activeObjects.Count < maxAmount)
            {
                Spawn();
            }
        }
    }

    private void Spawn()
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

        spawnPoint = levelBounds.ClampPointInside(spawnPoint) + new Vector3(0, YOffset, 0);

        GameObject food = Instantiate(prefab, spawnPoint, Quaternion.identity);
        activeObjects.Add(food);

        previousSpawnPoint = spawnPoint;
    }

    private void CleanupList()
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            if (activeObjects[i] == null)
            {
                activeObjects.RemoveAt(i);
            }
        }
    }
}