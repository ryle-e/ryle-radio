using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.IMGUI.Controls;

[CustomEditor(typeof(RadioInsulator))]
public class RadioInsulatorEditor : Editor
{
    private readonly BoxBoundsHandle outerBoundsHandle = new();
    private readonly BoxBoundsHandle innerBoundsHandle = new();

    private RadioInsulator insulator = null;

    private void OnEnable()
    {
        if (insulator == null)
            insulator = (RadioInsulator)target;

        if (insulator.Data != null)
        {
            outerBoundsHandle.SetColor(insulator.Data.GizmoColor);
            innerBoundsHandle.SetColor(insulator.Data.GizmoColorSecondary);
        }
    }

    private void OnSceneGUI()
    {
        if (insulator == null)
            insulator = (RadioInsulator)target;

        Bounds outerBounds = new(insulator.transform.position, insulator.outerBoxSize);
        Bounds innerBounds = new(insulator.transform.position, insulator.innerBoxSize);

        outerBoundsHandle.center = outerBounds.center;
        outerBoundsHandle.size = insulator.transform.localToWorldMatrix * outerBounds.size;

        innerBoundsHandle.center = innerBounds.center;
        innerBoundsHandle.size = insulator.transform.localToWorldMatrix * innerBounds.size;

        EditorGUI.BeginChangeCheck();
        outerBoundsHandle.DrawHandle();
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(insulator, "Change Outer Bounds");
            insulator.outerBoxSize = insulator.transform.worldToLocalMatrix * outerBoundsHandle.size;
        }

        EditorGUI.BeginChangeCheck();
        innerBoundsHandle.DrawHandle();
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(insulator, "Change Inner Bounds");
            insulator.innerBoxSize = insulator.transform.worldToLocalMatrix * innerBoundsHandle.size;
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}
#endif