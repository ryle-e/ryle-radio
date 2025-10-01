using UnityEngine;

namespace RyleRadio.Editor
{
    using RyleRadio.Components;

#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.IMGUI.Controls; // we have to use imgui handles rather than unity handles in order to have draggable boxes

    // the editor for an insulator
    // mainly to have handles (draggable gizmos)
    // check RadioInsulator.cs for more info on variables, etc
    [CustomEditor(typeof(RadioInsulator))]
    public class RadioInsulatorEditor : Editor
    {
        // draggable box gizmos (handles) for the outer and inner bounds
        private readonly BoxBoundsHandle outerBoundsHandle = new();
        private readonly BoxBoundsHandle innerBoundsHandle = new();

        // the insulator class itself
        private RadioInsulator insulator = null;


        // when the inspector is initialized
        private void OnEnable()
        {
            // get the insulator itself
            if (insulator == null)
                insulator = (RadioInsulator)target;

            if (insulator.Data != null)
            {
                // assign any changed colours from the radio data
                outerBoundsHandle.SetColor(insulator.Data.GizmoColor);
                innerBoundsHandle.SetColor(insulator.Data.GizmoColorSecondary);
            }
        }

        private void OnSceneGUI()
        {
            // if this doesn't have the insulator for some reason, get it
            if (insulator == null)
                insulator = (RadioInsulator)target;

            // get the boundaries of the insulator
            Bounds outerBounds = new(insulator.transform.position, insulator.outerBoxSize);
            Bounds innerBounds = new(insulator.transform.position, insulator.innerBoxSize);

            // apply the outer boundary info to one of the handles
            outerBoundsHandle.center = outerBounds.center;
            outerBoundsHandle.size = insulator.transform.localToWorldMatrix * outerBounds.size;

            // apply the inner boundary info to one of the handles
            innerBoundsHandle.center = innerBounds.center;
            innerBoundsHandle.size = insulator.transform.localToWorldMatrix * innerBounds.size;

            // capture any changes made to the handle
            EditorGUI.BeginChangeCheck();
            outerBoundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                // if changes were detected, save them to the undo record and apply the changes
                Undo.RecordObject(insulator, "Change Outer Bounds");
                insulator.outerBoxSize = insulator.transform.worldToLocalMatrix * outerBoundsHandle.size;
            }

            // capture any changes made to the handle
            EditorGUI.BeginChangeCheck();
            innerBoundsHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                // if changes were detected, save them to the undo record and apply the changes
                Undo.RecordObject(insulator, "Change Inner Bounds");
                insulator.innerBoxSize = insulator.transform.worldToLocalMatrix * innerBoundsHandle.size;
            }
        }

        // the inspector itself is normal, so draw it as such
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }

#endif
}