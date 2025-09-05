using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[CustomEditor(typeof(RadioBroadcaster))]
public class RadioBroadcasterEditor : Editor
{
    private RadioBroadcaster broadcaster;

    private Color innerColor;
    private Color outerColor;

    private void OnEnable()
    {
        if (broadcaster == null)
            broadcaster = (RadioBroadcaster) target;

        if (broadcaster.Data != null)
        {
            innerColor = broadcaster.Data.GizmoColorSecondary;
            outerColor = broadcaster.Data.GizmoColor;
        }
        else
        {
            innerColor = Color.white;
            outerColor = Color.white;
        }
    }

    private void OnSceneGUI()
    {
        Vector2 o = broadcaster.broadcastRadius;

        Handles.color = innerColor;
        o.x = Mathf.Min(o.y, Handles.RadiusHandle(Quaternion.identity, broadcaster.transform.position, o.x));

        Handles.color = outerColor;
        o.y = Mathf.Max(o.x, Handles.RadiusHandle(Quaternion.identity, broadcaster.transform.position, o.y));

        broadcaster.broadcastRadius = o;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}
#endif