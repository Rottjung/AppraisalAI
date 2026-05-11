using System.Collections.Generic;
using UnityEngine;

public class WorldSenseVisualizer : MonoBehaviour
{
    //[SerializeField] private Sensors sensors;
    //[SerializeField] private Material baseLineMaterial;

    //[Header("Circle Settings")]
    //[SerializeField] private int segments = 64;
    //[SerializeField] private float lineWidth = 0.03f;
    //[SerializeField] private float yOffset = 0.03f;

    //[Header("Colors")]
    //[SerializeField] private Color foodColor = Color.green;
    //[SerializeField] private Color enemyColor = Color.red;

    //private readonly Dictionary<WorldTarget, GameObject> visualObjects = new();

    //private void OnEnable()
    //{
    //    WorldTargetRegistry.TargetRegistered += HandleTargetRegistered;
    //    WorldTargetRegistry.TargetUnregistered += HandleTargetUnregistered;
    //}

    //private void Start()
    //{
    //    BuildAll();
    //}

    //private void OnDisable()
    //{
    //    WorldTargetRegistry.TargetRegistered -= HandleTargetRegistered;
    //    WorldTargetRegistry.TargetUnregistered -= HandleTargetUnregistered;
    //}

    //private void OnDestroy()
    //{
    //    ClearAll();
    //}

    //[ContextMenu("Rebuild All Circles")]
    //public void RebuildAll()
    //{
    //    ClearAll();
    //    BuildAll();
    //}

    //private void BuildAll()
    //{
    //    if (sensors == null || baseLineMaterial == null)
    //    {
    //        return;
    //    }

    //    for (int i = 0; i < WorldTargetRegistry.FoodTargets.Count; i++)
    //    {
    //        CreateVisual(WorldTargetRegistry.FoodTargets[i]);
    //    }

    //    for (int i = 0; i < WorldTargetRegistry.EnemyTargets.Count; i++)
    //    {
    //        CreateVisual(WorldTargetRegistry.EnemyTargets[i]);
    //    }
    //}

    //private void ClearAll()
    //{
    //    foreach (KeyValuePair<WorldTarget, GameObject> pair in visualObjects)
    //    {
    //        if (pair.Value != null)
    //        {
    //            Destroy(pair.Value);
    //        }
    //    }

    //    visualObjects.Clear();
    //}

    //private void HandleTargetRegistered(WorldTarget target)
    //{
    //    CreateVisual(target);
    //}

    //private void HandleTargetUnregistered(WorldTarget target)
    //{
    //    RemoveVisual(target);
    //}

    //private void CreateVisual(WorldTarget target)
    //{
    //    if (target == null || sensors == null || baseLineMaterial == null)
    //    {
    //        return;
    //    }

    //    if (visualObjects.ContainsKey(target))
    //    {
    //        return;
    //    }

    //    float radius = GetRadiusFor(target.TargetType);
    //    Color color = GetColorFor(target.TargetType);

    //    GameObject go = new GameObject($"{target.name}_SenseCircle");
    //    go.transform.SetParent(transform, true);

    //    LineRenderer lr = go.AddComponent<LineRenderer>();
    //    lr.useWorldSpace = true;
    //    lr.loop = true;
    //    lr.positionCount = segments;
    //    lr.startWidth = lineWidth;
    //    lr.endWidth = lineWidth;
    //    lr.alignment = LineAlignment.View;
    //    lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    //    lr.receiveShadows = false;

    //    Material instanceMaterial = new Material(baseLineMaterial);
    //    instanceMaterial.color = color;
    //    lr.material = instanceMaterial;
    //    lr.startColor = color;
    //    lr.endColor = color;

    //    BuildCircleWorldSpace(
    //        lr,
    //        target.transform.position + Vector3.up * yOffset,
    //        radius);

    //    visualObjects[target] = go;
    //}

    //private void RemoveVisual(WorldTarget target)
    //{
    //    if (target == null)
    //    {
    //        return;
    //    }

    //    if (!visualObjects.TryGetValue(target, out GameObject go))
    //    {
    //        return;
    //    }

    //    if (go != null)
    //    {
    //        Destroy(go);
    //    }

    //    visualObjects.Remove(target);
    //}

    //private float GetRadiusFor(WorldTargetType type)
    //{
    //    return type == WorldTargetType.Food
    //        ? sensors.foodSenseRadius
    //        : sensors.enemySenseRadius;
    //}

    //private Color GetColorFor(WorldTargetType type)
    //{
    //    return type == WorldTargetType.Food
    //        ? foodColor
    //        : enemyColor;
    //}

    //private void BuildCircleWorldSpace(LineRenderer lr, Vector3 center, float radius)
    //{
    //    if (lr == null)
    //    {
    //        return;
    //    }

    //    if (segments < 3)
    //    {
    //        segments = 3;
    //    }

    //    lr.positionCount = segments;

    //    float step = Mathf.PI * 2f / segments;

    //    for (int i = 0; i < segments; i++)
    //    {
    //        float angle = i * step;
    //        float x = Mathf.Cos(angle) * radius;
    //        float z = Mathf.Sin(angle) * radius;

    //        lr.SetPosition(i, center + new Vector3(x, 0f, z));
    //    }
    //}
}