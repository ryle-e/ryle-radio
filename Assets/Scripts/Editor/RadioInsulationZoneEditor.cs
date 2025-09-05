using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;

[CustomEditor(typeof(RadioInsulator))]
public class RadioInsulationZoneEditor : Editor
{
    private readonly BoxBoundsHandle outerBoundsHandle = new();
    private readonly BoxBoundsHandle innerBoundsHandle = new();

    private RadioInsulator zone = null;

    private void OnEnable()
    {
        if (zone == null)
            zone = (RadioInsulator)target;

        outerBoundsHandle.SetColor(zone.Data.GizmoColor);
        innerBoundsHandle.SetColor(zone.Data.GizmoColorSecondary);
    }

    private void OnSceneGUI()
    {
        if (zone == null)
            zone = (RadioInsulator)target;

        Bounds outerBounds = zone.outerBox;
        Bounds innerBounds = zone.innerBox;

        outerBoundsHandle.center = outerBounds.center + zone.transform.position;
        outerBoundsHandle.size = outerBounds.size;

        innerBoundsHandle.center = innerBounds.center + zone.transform.position;
        innerBoundsHandle.size = innerBounds.size;

        EditorGUI.BeginChangeCheck();
        outerBoundsHandle.DrawHandle();
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(zone, "Change Outer Bounds");
            zone.outerBox = new(outerBoundsHandle.center - zone.transform.position, outerBoundsHandle.size);
        }

        EditorGUI.BeginChangeCheck();
        innerBoundsHandle.DrawHandle();
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(zone, "Change Inner Bounds");
            zone.innerBox = new(innerBoundsHandle.center - zone.transform.position, innerBoundsHandle.size);
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}
#endif