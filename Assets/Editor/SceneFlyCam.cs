using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SceneFlyCam
{
    private static bool enabled = true;

    static SceneFlyCam()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sv)
    {
        if (!enabled) return;

        Event e = Event.current;
        if (e.type != EventType.KeyDown && e.type != EventType.KeyUp)
            return;

        bool w = e.keyCode == KeyCode.W;
        bool a = e.keyCode == KeyCode.A;
        bool s = e.keyCode == KeyCode.S;
        bool d = e.keyCode == KeyCode.D;
        bool q = e.keyCode == KeyCode.Q;
        bool eKey = e.keyCode == KeyCode.E;

        if (!w && !a && !s && !d && !q && !eKey)
            return;

        if (e.type == EventType.KeyUp)
        {
            e.Use();
            return;
        }

        float speed = 15f;
        if (e.shift) speed *= 3f;

        var cam = sv.camera;
        Vector3 fwd = cam.transform.forward;
        Vector3 right = cam.transform.right;
        fwd.y = 0f; fwd.Normalize();
        right.y = 0f; right.Normalize();

        Vector3 move = Vector3.zero;
        if (w) move += fwd * speed * 0.1f;
        if (s) move -= fwd * speed * 0.1f;
        if (d) move += right * speed * 0.1f;
        if (a) move -= right * speed * 0.1f;
        if (eKey) move += Vector3.up * speed * 0.1f;
        if (q) move -= Vector3.up * speed * 0.1f;

        sv.pivot += move;
        sv.Repaint();
        e.Use();
    }
}
