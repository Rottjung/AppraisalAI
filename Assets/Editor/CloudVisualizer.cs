using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class CloudVisualizer : EditorWindow
{
    private BehaviorCloudData cloudData;
    private Vector2 scrollPos;

    private enum ChannelSource { Constant, Node }

    private struct Mapping
    {
        public ChannelSource source;
        public string nodeId;
        public float constantValue;
        public Color constantColor;
    }

    private Mapping mapX, mapY, mapZ, mapSize, mapColor;
    private float spacingMultiplier = 10f;
    private float pointScale = 0.5f;

    private List<string> nodeIds = new();
    private string[] nodeOptions;

    // Shared state for preview window
    private static CloudVisualizer instance;
    private static CloudPreviewWindow previewWindow;

    public static BehaviorCloudData CloudData => instance != null ? instance.cloudData : null;
    public static float PointScale => instance != null ? instance.pointScale : 0.5f;

    public static Vector3 GetPosition(BehaviorRecord record)
    {
        if (instance == null) return Vector3.zero;
        return new Vector3(
            instance.GetChannelValue(record, instance.mapX) * instance.spacingMultiplier,
            instance.GetChannelValue(record, instance.mapY) * instance.spacingMultiplier,
            instance.GetChannelValue(record, instance.mapZ) * instance.spacingMultiplier
        );
    }

    public static float GetSize(BehaviorRecord record)
    {
        if (instance == null) return 0.5f;
        return instance.GetChannelValue(record, instance.mapSize);
    }

    public static Color GetColor(BehaviorRecord record)
    {
        if (instance == null) return Color.white;
        float val = instance.GetChannelValue(record, instance.mapColor);
        if (instance.mapColor.source == ChannelSource.Constant)
            return instance.mapColor.constantColor;
        return Color.Lerp(Color.white, instance.mapColor.constantColor, val);
    }

    [MenuItem("Tools/Cloud Visualizer")]
    private static void Open()
    {
        var w = GetWindow<CloudVisualizer>();
        w.titleContent = new GUIContent("Cloud Viz");
        w.minSize = new Vector2(350, 400);
        w.Show();
    }

    private void OnEnable()
    {
        instance = this;
    }

    private void OnDisable()
    {
        if (instance == this)
            instance = null;
    }

    private void RefreshNodeList()
    {
        nodeIds.Clear();
        if (cloudData == null)
        {
            nodeOptions = null;
            return;
        }

        var seen = new HashSet<string>();
        foreach (var record in cloudData.Records)
        {
            if (record == null) continue;
            foreach (var coord in record.Coordinates)
            {
                if (coord != null && !string.IsNullOrWhiteSpace(coord.BehaviorNodeId) && seen.Add(coord.BehaviorNodeId))
                    nodeIds.Add(coord.BehaviorNodeId);
            }
        }

        nodeOptions = new string[nodeIds.Count + 1];
        nodeOptions[0] = "Constant";
        for (int i = 0; i < nodeIds.Count; i++)
            nodeOptions[i + 1] = nodeIds[i];
    }

    private int NodeIndex(Mapping m)
    {
        if (m.source == ChannelSource.Constant) return 0;
        int idx = nodeIds.IndexOf(m.nodeId);
        return idx >= 0 ? idx + 1 : 0;
    }

    private Mapping MakeMapping(int popupIdx, float constVal, Color constColor)
    {
        if (popupIdx <= 0)
            return new Mapping { source = ChannelSource.Constant, constantValue = constVal, constantColor = constColor };
        return new Mapping { source = ChannelSource.Node, nodeId = nodeIds[popupIdx - 1], constantValue = constVal, constantColor = constColor };
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        DrawControls();

        EditorGUILayout.Space();
        if (GUILayout.Button("Open Cloud Preview Window", GUILayout.Height(30)))
        {
            previewWindow = EditorWindow.GetWindow<CloudPreviewWindow>();
            previewWindow.titleContent = new GUIContent("Cloud Preview");
            previewWindow.Show();
            previewWindow.SyncView(20f, 45f, GetAutoDist());
        }

        EditorGUILayout.EndScrollView();

        // Repaint the preview window whenever controls change
        if (previewWindow != null && Event.current.type == EventType.Repaint)
            previewWindow.Repaint();
    }

    private void DrawControls()
    {
        var newData = (BehaviorCloudData)EditorGUILayout.ObjectField("Cloud Data", cloudData, typeof(BehaviorCloudData), false);
        if (newData != cloudData)
        {
            cloudData = newData;
            RefreshNodeList();
        }

        if (cloudData == null)
        {
            EditorGUILayout.HelpBox("Assign a BehaviorCloudData asset to visualize.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Records: {cloudData.Records.Count}", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (nodeOptions == null || nodeOptions.Length == 0)
        {
            EditorGUILayout.HelpBox("No behavior nodes found in cloud records.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Coordinate Mapping", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        int xIdx = EditorGUILayout.Popup("X Position", NodeIndex(mapX), nodeOptions);
        mapX = MakeMapping(xIdx, mapX.constantValue, mapX.constantColor);

        int yIdx = EditorGUILayout.Popup("Y Position", NodeIndex(mapY), nodeOptions);
        mapY = MakeMapping(yIdx, mapY.constantValue, mapY.constantColor);

        int zIdx = EditorGUILayout.Popup("Z Position", NodeIndex(mapZ), nodeOptions);
        mapZ = MakeMapping(zIdx, mapZ.constantValue, mapZ.constantColor);

        EditorGUILayout.Space();
        spacingMultiplier = EditorGUILayout.FloatField("Spacing Multiplier", spacingMultiplier);

        EditorGUILayout.Space();

        int sizeIdx = EditorGUILayout.Popup("Size", NodeIndex(mapSize), nodeOptions);
        mapSize.constantValue = EditorGUILayout.FloatField("Constant Size", mapSize.constantValue);
        mapSize = MakeMapping(sizeIdx, mapSize.constantValue, mapSize.constantColor);

        int colorIdx = EditorGUILayout.Popup("Color", NodeIndex(mapColor), nodeOptions);
        mapColor.constantColor = EditorGUILayout.ColorField("Constant Color", mapColor.constantColor);
        mapColor = MakeMapping(colorIdx, mapColor.constantValue, mapColor.constantColor);

        if (colorIdx > 0)
            EditorGUILayout.HelpBox("Color = lerp(white, constant color, coord value)", MessageType.Info);

        EditorGUILayout.Space();
        pointScale = EditorGUILayout.Slider("Point Scale", pointScale, 0.01f, 5f);
    }

    private float GetChannelValue(BehaviorRecord record, Mapping m)
    {
        if (m.source == ChannelSource.Constant)
            return m.constantValue;

        foreach (var coord in record.Coordinates)
        {
            if (coord != null && coord.BehaviorNodeId == m.nodeId)
                return coord.Value;
        }
        return 0f;
    }

    private float GetAutoDist()
    {
        if (cloudData == null || cloudData.Records.Count == 0) return 15f;

        Bounds bounds = new Bounds(GetPosition(cloudData.Records[0]), Vector3.one);
        foreach (var record in cloudData.Records)
        {
            if (record == null) continue;
            bounds.Encapsulate(GetPosition(record));
        }

        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 1f);
        return maxSize * 1.5f + 2f;
    }
}
