using UnityEngine;
using Codice.Client.BaseCommands.BranchExplorer;


#if UNITY_EDITOR
using UnityEditor;

[CustomPropertyDrawer(typeof(StationRadioTrack))]
public class StationRadioTrackEditor : PropertyDrawer
{
    private int lastTrackWSize = -1;
    private bool isShowingThreshold = false;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty trackWs = property.FindPropertyRelative("stationTrackWs");

        SerializedProperty isRandom = property.FindPropertyRelative("randomSequence");
        SerializedProperty threshold = property.FindPropertyRelative("thresholdBeforeRepeats");

        // random options
        Rect isRandomRect = new(position.position, new Vector2(24, 20));
        Rect thresholdSliderRect = isRandomRect;

        thresholdSliderRect.position += new Vector2(0, 20);
        thresholdSliderRect.width = position.width;

        if (lastTrackWSize < 0)
            lastTrackWSize = trackWs.arraySize;

        if (EditorGUI.Toggle(isRandomRect, new GUIContent("Random Sequence"), isRandom.boolValue))
        {
            isRandom.boolValue = true;
            isShowingThreshold = true;

            EditorGUI.Slider(thresholdSliderRect, threshold, 0, 1, new GUIContent("Threshold Before Repeats"));
        }
        else
        {
            isRandom.boolValue = false;
            isShowingThreshold = false;
        }

        position.position = new Vector2(position.position.x, position.position.y + isRandomRect.height + (isShowingThreshold ? thresholdSliderRect.height + 4 : 0));

        //EditorGUILayout.EndHorizontal();

        // list of tracks
        EditorGUI.PropertyField(position, trackWs, label, true);

        if (GUI.changed)
        {
            if (trackWs.arraySize > lastTrackWSize)
            {
                SerializedProperty newElement = trackWs.GetArrayElementAtIndex(trackWs.arraySize - 1);

                SerializedProperty track = newElement.FindPropertyRelative("track");
                SerializedProperty trackType = newElement.FindPropertyRelative("trackType");

                SerializedProperty gain = newElement.FindPropertyRelative("gain");

                track.managedReferenceValue = StationRadioTrackWrapper.CreateTrackEditor(trackType.enumValueIndex);
                gain.floatValue = 100;

                lastTrackWSize++;
            }
            else if (trackWs.arraySize < lastTrackWSize)
            { 
                lastTrackWSize--; 
            }

            property.serializedObject.ApplyModifiedProperties();
        }

    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float randomOptionsHeight = 20 + (isShowingThreshold ? 20 : 0);
        float listHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("stationTrackWs"));

        return randomOptionsHeight + listHeight;
    }
}
#endif