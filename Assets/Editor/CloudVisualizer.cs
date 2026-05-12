using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class CloudVisualizer : EditorWindow
{
    private BehaviorCloudData cloudData;

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

    // Preview
    private PreviewRenderUtility preview;
    private Mesh sphereMesh;
    private Material previewMat;

    // Orbit
    private float orbitPitch = 20f;
    private float orbitYaw = 45f;
    private float orbitDist = 15f;
    private Vector2 lastMousePos;
    private bool isDragging;

    // Splitter
    private float splitRatio = 0.7f;
    private bool isDraggingSplitter;
    private const float minPanelWidth = 120f;
    private const float splitterWidth = 5f;

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
        w.minSize = new Vector2(500, 350);
        w.Show();
    }

    private void OnEnable()
    {
        instance = this;
        preview = new PreviewRenderUtility();
        preview.camera.fieldOfView = 30f;
        preview.camera.nearClipPlane = 0.1f;
        preview.camera.farClipPlane = 100f;
        preview.camera.clearFlags = CameraClearFlags.SolidColor;
        preview.camera.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);

        sphereMesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
        previewMat = new Material(Shader.Find("Unlit/Color"));
        previewMat.hideFlags = HideFlags.HideAndDontSave;
    }

    private void OnDisable()
    {
        if (instance == this) instance = null;
        if (preview != null) { preview.Cleanup(); preview = null; }
        if (previewMat != null) DestroyImmediate(previewMat);
    }

    private void RefreshNodeList()
    {
        nodeIds.Clear();
        if (cloudData == null) { nodeOptions = null; return; }

        var seen = new HashSet<string>();
        foreach (var record in cloudData.Records)
        {
            if (record == null) continue;
            foreach (var coord in record.Coordinates)
                if (coord != null && !string.IsNullOrWhiteSpace(coord.BehaviorNodeId) && seen.Add(coord.BehaviorNodeId))
                    nodeIds.Add(coord.BehaviorNodeId);
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
        if (preview == null) return;

        Rect totalRect = new Rect(0, 0, position.width, position.height);

        // Compute panel widths
        float splitX = Mathf.Clamp(totalRect.width * splitRatio, minPanelWidth, totalRect.width - minPanelWidth - splitterWidth);
        float settingsWidth = totalRect.width - splitX - splitterWidth;

        Rect previewRect = new Rect(0, 0, splitX, totalRect.height);
        Rect splitterRect = new Rect(splitX, 0, splitterWidth, totalRect.height);
        Rect settingsRect = new Rect(splitX + splitterWidth, 0, settingsWidth, totalRect.height);

        // Preview
        if (cloudData != null && nodeIds.Count > 0)
        {
            HandlePreviewEvents(previewRect);
            if (Event.current.type == EventType.Repaint)
                RenderPreview(previewRect);
        }
        else
        {
            EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.15f, 1f));
            if (cloudData == null)
            {
                GUI.Label(previewRect, "Load a BehaviorCloudData asset", new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                { fontSize = 14, alignment = TextAnchor.MiddleCenter });
            }
        }

        // Splitter
        EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
        if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
        {
            isDraggingSplitter = true;
            Event.current.Use();
        }
        if (isDraggingSplitter)
        {
            splitRatio = Event.current.mousePosition.x / totalRect.width;
            if (Event.current.type == EventType.MouseUp) isDraggingSplitter = false;
            Repaint();
        }
        EditorGUI.DrawRect(splitterRect, new Color(0.3f, 0.3f, 0.3f, 1f));

        // Settings
        GUILayout.BeginArea(settingsRect);
        DrawSettings();
        GUILayout.EndArea();

        // Sync preview window
        if (previewWindow != null && Event.current.type == EventType.Repaint)
            previewWindow.Repaint();
    }

    private void DrawSettings()
    {
        var newData = (BehaviorCloudData)EditorGUILayout.ObjectField("Cloud Data", cloudData, typeof(BehaviorCloudData), false);
        if (newData != cloudData)
        {
            cloudData = newData;
            RefreshNodeList();
            AutoFit();
        }

        if (cloudData == null)
        {
            EditorGUILayout.HelpBox("Assign a BehaviorCloudData asset.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Records: {cloudData.Records.Count}", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (nodeOptions == null || nodeOptions.Length == 0)
        {
            EditorGUILayout.HelpBox("No behavior nodes found.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Coordinate Mapping", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        int xIdx = EditorGUILayout.Popup("X", NodeIndex(mapX), nodeOptions);
        mapX = MakeMapping(xIdx, mapX.constantValue, mapX.constantColor);

        int yIdx = EditorGUILayout.Popup("Y", NodeIndex(mapY), nodeOptions);
        mapY = MakeMapping(yIdx, mapY.constantValue, mapY.constantColor);

        int zIdx = EditorGUILayout.Popup("Z", NodeIndex(mapZ), nodeOptions);
        mapZ = MakeMapping(zIdx, mapZ.constantValue, mapZ.constantColor);

        EditorGUILayout.Space();
        spacingMultiplier = EditorGUILayout.FloatField("Spacing", spacingMultiplier);

        EditorGUILayout.Space();
        int sizeIdx = EditorGUILayout.Popup("Size", NodeIndex(mapSize), nodeOptions);
        mapSize.constantValue = EditorGUILayout.FloatField("Size Val", mapSize.constantValue);
        mapSize = MakeMapping(sizeIdx, mapSize.constantValue, mapSize.constantColor);

        int colorIdx = EditorGUILayout.Popup("Color", NodeIndex(mapColor), nodeOptions);
        mapColor.constantColor = EditorGUILayout.ColorField("Color", mapColor.constantColor);
        mapColor = MakeMapping(colorIdx, mapColor.constantValue, mapColor.constantColor);

        if (colorIdx > 0)
            EditorGUILayout.HelpBox("lerp(white → color, val)", MessageType.None);

        EditorGUILayout.Space();
        pointScale = EditorGUILayout.Slider("Scale", pointScale, 0.01f, 5f);

        EditorGUILayout.Space();
        if (GUILayout.Button("Fit View")) AutoFit();

        EditorGUILayout.Space();
        if (GUILayout.Button("Open Preview Window"))
        {
            previewWindow = EditorWindow.GetWindow<CloudPreviewWindow>();
            previewWindow.titleContent = new GUIContent("Cloud Preview");
            previewWindow.Show();
            previewWindow.SyncView(orbitPitch, orbitYaw, orbitDist);
        }
    }

    private void HandlePreviewEvents(Rect rect)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition) && e.button == 0)
        {
            isDragging = true;
            lastMousePos = e.mousePosition;
            e.Use();
        }
        if (e.type == EventType.MouseUp && e.button == 0)
        {
            isDragging = false;
            e.Use();
        }
        if (e.type == EventType.MouseDrag && isDragging)
        {
            Vector2 delta = e.mousePosition - lastMousePos;
            orbitYaw += delta.x * 0.5f;
            orbitPitch = Mathf.Clamp(orbitPitch - delta.y * 0.5f, -80f, 80f);
            lastMousePos = e.mousePosition;
            e.Use();
            Repaint();
        }
        if (e.type == EventType.ScrollWheel && rect.Contains(e.mousePosition))
        {
            orbitDist = Mathf.Clamp(orbitDist + e.delta.y * 0.5f, 2f, 100f);
            e.Use();
            Repaint();
        }
    }

    private void RenderPreview(Rect rect)
    {
        preview.BeginPreview(rect, GUIStyle.none);

        Quaternion orbitRot = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
        Vector3 cameraPos = orbitRot * new Vector3(0f, 0f, -orbitDist);
        preview.camera.transform.position = cameraPos;
        preview.camera.transform.LookAt(Vector3.zero, Vector3.up);
        preview.lights[0].intensity = 1f;

        var mpb = new MaterialPropertyBlock();

        foreach (var record in cloudData.Records)
        {
            if (record == null) continue;
            Vector3 pos = GetPosition(record);
            float size = GetSize(record) * pointScale;
            Color color = GetColor(record);
            mpb.SetColor("_Color", color);
            preview.DrawMesh(sphereMesh, Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * Mathf.Max(size, 0.01f)), previewMat, 0, mpb);
        }

        preview.camera.Render();
        Texture result = preview.EndPreview();
        GUI.DrawTexture(rect, result, ScaleMode.StretchToFill, false);

        var labelStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 11, alignment = TextAnchor.LowerCenter };
        labelStyle.normal.textColor = Color.white;

        foreach (var record in cloudData.Records)
        {
            if (record == null) continue;
            Vector3 worldPos = GetPosition(record);
            Vector3 viewPos = preview.camera.WorldToViewportPoint(worldPos);
            if (viewPos.z < 0f) continue;

            float labelX = rect.x + viewPos.x * rect.width;
            float labelY = rect.y + (1f - viewPos.y) * rect.height;
            GUI.Label(new Rect(labelX - 50, labelY + 4, 100, 20), record.PayloadId, labelStyle);
        }
    }

    private float GetChannelValue(BehaviorRecord record, Mapping m)
    {
        if (m.source == ChannelSource.Constant) return m.constantValue;
        foreach (var coord in record.Coordinates)
            if (coord != null && coord.BehaviorNodeId == m.nodeId)
                return coord.Value;
        return 0f;
    }

    private void AutoFit()
    {
        if (cloudData == null || cloudData.Records.Count == 0) return;

        Bounds bounds = new Bounds(GetPosition(cloudData.Records[0]), Vector3.one);
        foreach (var record in cloudData.Records)
        {
            if (record == null) continue;
            bounds.Encapsulate(GetPosition(record));
        }

        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 1f);
        orbitDist = maxSize * 1.5f + 2f;
        orbitPitch = 20f;
        orbitYaw = 45f;
        Repaint();
    }
}
