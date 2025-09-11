using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

// the editor for a RadioData object
// the main purpose for this is to reset tracks when new ones are added- if we don't do this, they link- changing one track changes all of them

// see RadioData.cs for more info about variables
[CustomEditor(typeof(RadioData))]
public class RadioDataEditor : Editor
{
    private SerializedProperty trackWs; // the tracks in this radio
    private int lastTrackWSize; // the last recorded number of tracks

    private SerializedProperty gizmoColor; // colour of gizmos associated with the data
    private SerializedProperty gizmoColorSecondary;

    // for a foldout hiding advanced vars
    private bool showAdvanced = false;


    // on inspector init
    private void OnEnable()
    {
        // store gizmo colours
        gizmoColor = serializedObject.FindProperty("gizmoColor");
        gizmoColorSecondary = serializedObject.FindProperty("gizmoColorSecondary");

        // store tracks
        trackWs = serializedObject.FindProperty("trackWs");
        lastTrackWSize = trackWs.arraySize;

        // for every track,
        for (int i = 0; i < trackWs.arraySize; i++)
        {
            // cache it
            SerializedProperty track = trackWs.GetArrayElementAtIndex(i);

            // get the internal track class and the string type
            SerializedProperty radioTrack = track.FindPropertyRelative("track");
            SerializedProperty trackType = track.FindPropertyRelative("trackType");

            // set the track type to whatever the internal track class is
            trackType.stringValue = RadioTrackWrapper.GetTrackType(radioTrack.managedReferenceValue.ToString());
        }
    }

    public override void OnInspectorGUI()
    {
        // update the rest of the inspector
        serializedObject.Update();

        // show the foldout that hides advanced vars
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, new GUIContent("Advanced Settings"));

        // if the foldout is open,
        if (showAdvanced)
        {
            // display the gizmo colour vars
            EditorGUILayout.PropertyField(gizmoColor, new GUIContent("Gizmo Colour"));
            EditorGUILayout.PropertyField(gizmoColorSecondary, new GUIContent("Secondary Gizmo Colour"));
        }

        // display the tracks
        EditorGUILayout.PropertyField(trackWs, new GUIContent("Tracks", "These are actually RadioTrackWrappers, not RadioTracks"));

        // if something has been changed
        if (GUI.changed)
        {
            // if a track has been added,
            if (trackWs.arraySize > lastTrackWSize)
            {
                // cache it
                SerializedProperty newElement = trackWs.GetArrayElementAtIndex(trackWs.arraySize - 1);

                // get the internal class and type
                SerializedProperty radioTrack = newElement.FindPropertyRelative("track");
                SerializedProperty trackType = newElement.FindPropertyRelative("trackType");

                // get the gain
                SerializedProperty gain = newElement.FindPropertyRelative("gain");

                // reset the internal class and gain
                radioTrack.managedReferenceValue = RadioTrackWrapper.CreateTrackEditor(trackType.stringValue);
                gain.floatValue = 100;

                // store the new track list size
                lastTrackWSize++;
            }
            // if a track has been removed
            else if (trackWs.arraySize < lastTrackWSize)
            {
                // store the new track list size
                lastTrackWSize--; 
            }
        }

        // apply inspector changes
        serializedObject.ApplyModifiedProperties();
    }
}
#endif