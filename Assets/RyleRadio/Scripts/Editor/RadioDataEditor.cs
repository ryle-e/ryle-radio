using UnityEngine;

namespace RyleRadio.Editor
{
    using RyleRadio.Tracks;

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

                // if the track itself is null somehow, (e.g it's the first track added when the data is created)
                if (radioTrack.managedReferenceValue == null)
                {
                    // set a default track class based on the listed track type string
                    radioTrack.managedReferenceValue = RadioTrackWrapper.CreateTrackEditor(trackType.stringValue);
                }
                else
                {
                    // set the track type string to whatever the internal track class is
                    trackType.stringValue = RadioTrackWrapper.GetTrackType(radioTrack.managedReferenceValue.ToString());
                }
            }

            // apply the changes
            serializedObject.ApplyModifiedProperties();
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

                if (GUILayout.Button(new GUIContent("Clear Cache")))
                    ((RadioData) serializedObject.targetObject).ClearCache();
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

                    // get the gain curve
                    SerializedProperty gainCurve = newElement.FindPropertyRelative("gainCurve");

                    // reset the internal track class
                    radioTrack.managedReferenceValue = RadioTrackWrapper.CreateTrackEditor(trackType.stringValue);

                    // reset the gain and the gain curve
                    gain.floatValue = 100;
                    gainCurve.animationCurveValue = new(RadioTrackWrapper.DefaultGainCurve.keys);

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

}