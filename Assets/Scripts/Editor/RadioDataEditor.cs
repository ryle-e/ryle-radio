using UnityEngine;
using System.Security.Policy;


#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(RadioData))]
public class RadioDataEditor : Editor
{
    private SerializedProperty trackWs;
    private int lastTrackWSize;

    private SerializedProperty gizmoColor;
    private SerializedProperty gizmoColorSecondary;
    private bool showAdvanced = false;

    private void OnEnable()
    {
        gizmoColor = serializedObject.FindProperty("gizmoColor");
        gizmoColorSecondary = serializedObject.FindProperty("gizmoColorSecondary");

        trackWs = serializedObject.FindProperty("trackWs");
        lastTrackWSize = trackWs.arraySize;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        showAdvanced = EditorGUILayout.Foldout(showAdvanced, new GUIContent("Advanced Settings"));

        if (showAdvanced)
        {
            EditorGUILayout.PropertyField(gizmoColor, new GUIContent("Gizmo Colour"));
            EditorGUILayout.PropertyField(gizmoColorSecondary, new GUIContent("Secondary Gizmo Colour"));
        }

        EditorGUILayout.PropertyField(trackWs, new GUIContent("Tracks", "These are actually RadioTrackWrappers, not RadioTracks"));

        if (GUI.changed)
        {
            if (trackWs.arraySize > lastTrackWSize)
            {
                SerializedProperty newElement = trackWs.GetArrayElementAtIndex(trackWs.arraySize - 1);

                SerializedProperty radioTrack = newElement.FindPropertyRelative("track");
                SerializedProperty trackType = newElement.FindPropertyRelative("trackType");

                SerializedProperty gain = newElement.FindPropertyRelative("gain");

                radioTrack.managedReferenceValue = RadioTrackWrapper.CreateTrackEditor(trackType.stringValue);
                gain.floatValue = 100;

                lastTrackWSize++;
            }
            else if (trackWs.arraySize < lastTrackWSize)
            { 
                lastTrackWSize--; 
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif