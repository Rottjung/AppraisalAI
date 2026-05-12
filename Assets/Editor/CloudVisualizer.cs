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

    private PreviewRenderUtility preview;
    private Mesh sphereMesh;
    private Material previewMat;

    // Orbit camera
    private float orbitPitch = 20f;
    private float orbitYaw = 45f;
    private float orbitDist = 15f;
    private Vector2 lastMousePos;
    private bool isDragging;

    [MenuItem("Tools/Cloud Visualizer")]
    private static void Open()
    {
        var w = GetWindow<CloudVisualizer>();
        w.titleContent = new GUIContent("Cloud Viz");
        w.minSize = new Vector2(400, 500);
        w.Show();
    }

    private void OnEnable()
    {
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
        if (preview != null)
        {
            preview.Cleanup();
            preview = null;
        }
        if (previewMat != null)
            DestroyImmediate(previewMat);
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
        DrawPreview();

        EditorGUILayout.EndScrollView();
    }

    private void DrawControls()
    {
        var newData = (BehaviorCloudData)EditorGUILayout.ObjectField("Cloud Data", cloudData, typeof(BehaviorCloudData), false);
        if (newData != cloudData)
        {
            cloudData = newData;
            RefreshNodeList();
            FramePoints();
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

        EditorGUILayout.Space();

        if (GUILayout.Button("Fit View to Points"))
            FramePoints();
    }

    private void DrawPreview()
    {
        if (cloudData == null || nodeIds.Count == 0)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("3D Preview", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Drag to rotate · Scroll to zoom", EditorStyles.miniLabel);

        Rect previewRect = EditorGUILayout.GetControlRect(GUILayout.Height(350));

        HandlePreviewEvents(previewRect);

        if (Event.current.type == EventType.Repaint)
            RenderPreview(previewRect);
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

        // Position camera
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

            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * Mathf.Max(size, 0.01f));
            preview.DrawMesh(sphereMesh, matrix, previewMat, 0, mpb);
        }

        preview.camera.Render();
        Texture result = preview.EndPreview();

        GUI.DrawTexture(rect, result, ScaleMode.StretchToFill, false);

        // Draw label text on top of the preview
        var labelStyle = new GUIStyle(EditorStyles.miniLabel);
        labelStyle.alignment = TextAnchor.LowerCenter;
        labelStyle.fontSize = 11;
        labelStyle.normal.textColor = Color.white;

        foreach (var record in cloudData.Records)
        {
            if (record == null) continue;
            Vector3 worldPos = GetPosition(record);
            Vector3 viewPos = preview.camera.WorldToViewportPoint(worldPos);
            if (viewPos.z < 0f) continue;

            float labelX = rect.x + viewPos.x * rect.width;
            float labelY = rect.y + (1f - viewPos.y) * rect.height;

            var labelRect = new Rect(labelX - 50, labelY + 4, 100, 20);
            GUI.Label(labelRect, record.PayloadId, labelStyle);
        }
    }

    private Vector3 GetPosition(BehaviorRecord record)
    {
        return new Vector3(
            GetChannelValue(record, mapX) * spacingMultiplier,
            GetChannelValue(record, mapY) * spacingMultiplier,
            GetChannelValue(record, mapZ) * spacingMultiplier
        );
    }

    private float GetSize(BehaviorRecord record)
    {
        return GetChannelValue(record, mapSize);
    }

    private Color GetColor(BehaviorRecord record)
    {
        float val = GetChannelValue(record, mapColor);
        if (mapColor.source == ChannelSource.Constant)
            return mapColor.constantColor;
        return Color.Lerp(Color.white, mapColor.constantColor, val);
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
        bool hasBounds = false;
        foreach (var record in cloudData.Records)
        {
            if (record == null) continue;
            if (!hasBounds)
            {
                bounds = new Bounds(GetPosition(record), Vector3.one);
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(GetPosition(record));
            }
        }

        float maxSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z, 1f);
        orbitDist = maxSize * 1.5f + 2f;
        orbitPitch = 20f;
        orbitYaw = 45f;
        Repaint();
    }
}
