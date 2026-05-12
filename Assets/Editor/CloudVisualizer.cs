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
    private float pointScale = 1f;
    private bool drawLabels = true;

    private List<string> nodeIds = new();
    private string[] nodeOptions;
    private bool subscribed;

    [MenuItem("Tools/Cloud Visualizer")]
    private static void Open()
    {
        var w = GetWindow<CloudVisualizer>();
        w.titleContent = new GUIContent("Cloud Viz");
        w.Show();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (!subscribed)
        {
            SceneView.duringSceneGui += OnSceneGUI;
            subscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if (subscribed)
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            subscribed = false;
        }
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

        var newData = (BehaviorCloudData)EditorGUILayout.ObjectField("Cloud Data", cloudData, typeof(BehaviorCloudData), false);
        if (newData != cloudData)
        {
            cloudData = newData;
            RefreshNodeList();
        }

        if (cloudData == null)
        {
            EditorGUILayout.HelpBox("Assign a BehaviorCloudData asset to visualize.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        EditorGUILayout.LabelField($"Records: {cloudData.Records.Count}", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (nodeOptions == null || nodeOptions.Length == 0)
        {
            EditorGUILayout.HelpBox("No behavior nodes found in cloud records.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
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

        int sizeIdx = EditorGUILayout.Popup("Size", NodeIndex(mapSize), nodeOptions);
        if (sizeIdx == 0)
            mapSize.constantValue = EditorGUILayout.FloatField("Constant Size", mapSize.constantValue);
        mapSize = MakeMapping(sizeIdx, mapSize.constantValue, mapSize.constantColor);

        int colorIdx = EditorGUILayout.Popup("Color", NodeIndex(mapColor), nodeOptions);
        if (colorIdx == 0)
            mapColor.constantColor = EditorGUILayout.ColorField("Constant Color", mapColor.constantColor);
        mapColor = MakeMapping(colorIdx, mapColor.constantValue, mapColor.constantColor);

        EditorGUILayout.Space();
        pointScale = EditorGUILayout.Slider("Point Scale", pointScale, 0.1f, 10f);
        drawLabels = EditorGUILayout.Toggle("Draw Labels", drawLabels);

        EditorGUILayout.Space();

        if (GUILayout.Button("Fit View to Points"))
        {
            FramePoints();
        }

        EditorGUILayout.EndScrollView();
    }

    private void OnSceneGUI(SceneView sv)
    {
        if (cloudData == null || nodeIds.Count == 0)
            return;

        Handles.BeginGUI();
        Handles.EndGUI();

        foreach (var record in cloudData.Records)
        {
            if (record == null) continue;

            Vector3 pos = GetPosition(record);
            float size = GetSize(record);
            Color color = GetColor(record);

            Handles.color = color;
            Handles.SphereHandleCap(0, pos, Quaternion.identity, size * pointScale, EventType.Repaint);

            if (drawLabels)
            {
                var style = new GUIStyle { fontSize = 10, normal = new GUIStyleState { textColor = color } };
                Handles.Label(pos + Vector3.up * size * pointScale, record.PayloadId, style);
            }
        }

        sv.Repaint();
    }

    private Vector3 GetPosition(BehaviorRecord record)
    {
        float x = GetChannelValue(record, mapX);
        float y = GetChannelValue(record, mapY);
        float z = GetChannelValue(record, mapZ);
        return new Vector3(x, y, z);
    }

    private float GetSize(BehaviorRecord record)
    {
        return GetChannelValue(record, mapSize);
    }

    private Color GetColor(BehaviorRecord record)
    {
        float val = GetChannelValue(record, mapColor);
        return mapColor.source == ChannelSource.Constant ? mapColor.constantColor : Color.HSVToRGB(val, 0.8f, 0.9f);
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

    private void FramePoints()
    {
        if (cloudData == null || cloudData.Records.Count == 0) return;

        Bounds bounds = new Bounds(GetPosition(cloudData.Records[0]), Vector3.one);
        foreach (var record in cloudData.Records)
        {
            if (record == null) continue;
            bounds.Encapsulate(GetPosition(record));
        }

        SceneView.lastActiveSceneView.Frame(bounds, false);
    }
}
