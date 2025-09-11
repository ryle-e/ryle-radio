using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

// custom inspector for a RadioBroadcaster
// mainly to have handles (draggable gizmos)
// check RadioBroadcaster.cs for more info on variables, etc
[CustomEditor(typeof(RadioBroadcaster))]
public class RadioBroadcasterEditor : Editor
{
    // the broadcaster itself
    private RadioBroadcaster broadcaster;

    // the colours of the broadcaster gizmos
    private Color innerColor;
    private Color outerColor;

    private void OnEnable()
    {
        // if the broadcaster's not set, set it
        if (broadcaster == null)
            broadcaster = (RadioBroadcaster) target;

        // if the broadcaster has a RadioData assigned, get gizmo colours from it
        if (broadcaster.Data != null)
        {
            innerColor = broadcaster.Data.GizmoColorSecondary;
            outerColor = broadcaster.Data.GizmoColor;
        }
        // otherwise make them white
        else
        {
            innerColor = Color.white;
            outerColor = Color.white;
        }
    }

    private void OnSceneGUI()
    {
        // get the radii
        Vector2 o = broadcaster.broadcastRadius;

        // display the inner radius
        Handles.color = innerColor;
        o.x = Mathf.Min(o.y, Handles.RadiusHandle(Quaternion.identity, broadcaster.transform.position, o.x));

        // display the outer radius
        Handles.color = outerColor;
        o.y = Mathf.Max(o.x, Handles.RadiusHandle(Quaternion.identity, broadcaster.transform.position, o.y));

        // set the radii
        broadcaster.broadcastRadius = o;
    }

    // the rest of the inspector is default, so draw it
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}
#endif