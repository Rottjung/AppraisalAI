using UnityEditor;
using UnityEngine;

public class CloudPreviewWindow : EditorWindow
{
    private PreviewRenderUtility preview;
    private Mesh sphereMesh;
    private Material previewMat;

    private float orbitPitch = 20f;
    private float orbitYaw = 45f;
    private float orbitDist = 15f;
    private Vector2 lastMousePos;
    private bool isDragging;

    [MenuItem("Tools/Cloud Preview")]
    private static void Open()
    {
        var w = GetWindow<CloudPreviewWindow>();
        w.titleContent = new GUIContent("Cloud Preview");
        w.minSize = new Vector2(300, 200);
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

    private void OnGUI()
    {
        if (CloudVisualizer.CloudData == null)
        {
            EditorGUILayout.HelpBox("Load a BehaviorCloudData asset in the Cloud Visualizer window first.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Drag to rotate · Scroll to zoom", EditorStyles.miniLabel);

        Rect previewRect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));
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
        var data = CloudVisualizer.CloudData;
        if (data == null) return;

        preview.BeginPreview(rect, GUIStyle.none);

        Quaternion orbitRot = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
        Vector3 cameraPos = orbitRot * new Vector3(0f, 0f, -orbitDist);
        preview.camera.transform.position = cameraPos;
        preview.camera.transform.LookAt(Vector3.zero, Vector3.up);
        preview.lights[0].intensity = 1f;

        var mpb = new MaterialPropertyBlock();

        foreach (var record in data.Records)
        {
            if (record == null) continue;

            Vector3 pos = CloudVisualizer.GetPosition(record);
            float size = CloudVisualizer.GetSize(record) * CloudVisualizer.PointScale;

            Color color = CloudVisualizer.GetColor(record);
            mpb.SetColor("_Color", color);

            Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * Mathf.Max(size, 0.01f));
            preview.DrawMesh(sphereMesh, matrix, previewMat, 0, mpb);
        }

        preview.camera.Render();
        Texture result = preview.EndPreview();

        GUI.DrawTexture(rect, result, ScaleMode.StretchToFill, false);

        var labelStyle = new GUIStyle(EditorStyles.miniLabel);
        labelStyle.alignment = TextAnchor.LowerCenter;
        labelStyle.fontSize = 11;
        labelStyle.normal.textColor = Color.white;

        foreach (var record in data.Records)
        {
            if (record == null) continue;
            Vector3 worldPos = CloudVisualizer.GetPosition(record);
            Vector3 viewPos = preview.camera.WorldToViewportPoint(worldPos);
            if (viewPos.z < 0f) continue;

            float labelX = rect.x + viewPos.x * rect.width;
            float labelY = rect.y + (1f - viewPos.y) * rect.height;

            var labelRect = new Rect(labelX - 50, labelY + 4, 100, 20);
            GUI.Label(labelRect, record.PayloadId, labelStyle);
        }
    }

    internal void SyncView(float pitch, float yaw, float dist)
    {
        orbitPitch = pitch;
        orbitYaw = yaw;
        orbitDist = dist;
        Repaint();
    }
}
