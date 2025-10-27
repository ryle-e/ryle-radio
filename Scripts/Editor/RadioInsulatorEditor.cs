using UnityEngine;

namespace RyleRadio.Editor
{
    using RyleRadio.Components;

#if UNITY_EDITOR
    using UnityEditor;
    using UnityEditor.IMGUI.Controls; // we have to use imgui handles rather than unity handles in order to have draggable boxes

    /// <summary>
    /// Custom inspector for a \ref RadioInsulator
    /// <br><br>This is mainly so that we can use `Handles` (draggable gizmos).
    /// 
    /// <b>See: </b> \ref RadioInsulator
    /// </summary>
    [CustomEditor(typeof(RadioInsulator))]
    public class RadioInsulatorEditor : Editor
    {
        /// <summary>
        /// The insulator this is linked to
        /// </summary>
        private RadioInsulator insulator = null;

        /// The draggable gizmo for the outer bounds of the insulator
        private readonly BoxBoundsHandle outerBoundsHandle = new();
        /// The draggable gizmo for the inner bounds of the insulator
        private readonly BoxBoundsHandle innerBoundsHandle = new();


        /// <summary>
        /// Initializes the insulator's editor
        /// </summary>
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

        /// <summary>
        /// Displays the handles for \ref RadioInsulator.innerBoxSize and \ref RadioInsulator.outerBoxSize in the scene
        /// </summary>
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

        /// <summary>
        /// Draw the actual inspector as default
        /// </summary>
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }

#endif
}