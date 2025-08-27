using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(RadioData))]
public class RadioDataEditor : Editor
{
    private SerializedProperty trackWs;

    private int lastTrackWSize;

    private void OnEnable()
    {
        trackWs = serializedObject.FindProperty("trackWs");
        lastTrackWSize = trackWs.arraySize;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(trackWs);

        if (GUI.changed)
        {
            if (trackWs.arraySize > lastTrackWSize)
            {
                SerializedProperty newElement = trackWs.GetArrayElementAtIndex(trackWs.arraySize - 1);

                SerializedProperty radioTrack = newElement.FindPropertyRelative("track");
                SerializedProperty trackType = newElement.FindPropertyRelative("trackType");

                if (radioTrack.managedReferenceValue != null)
                {
                    radioTrack.managedReferenceValue = RadioTrackWrapper.CreateTrackEditor(trackType.enumValueIndex); 
                }

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