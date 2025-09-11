using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

// the editor for a station radio track
// this mainly exists so that we can reset any newly added track in the station, same as RadioDataEditor
//
// please check StationRadioTrack for info about any variables here
[CustomPropertyDrawer(typeof(StationRadioTrack))]
public class StationRadioTrackEditor : PropertyDrawer
{
    private int lastTrackWSize = -1; // the last number of tracks in this station
    private bool isShowingThreshold = false; // whether or not the random threshold is currently visible in the inspector


    public override void OnGUI(Rect _position, SerializedProperty _property, GUIContent _label)
    {
        SerializedProperty trackWs = _property.FindPropertyRelative("stationTrackWs"); // the tracks

        SerializedProperty isRandom = _property.FindPropertyRelative("randomSequence"); // whether or not the station is random
        SerializedProperty threshold = _property.FindPropertyRelative("thresholdBeforeRepeats"); // the random threshold if it is

        // visuals for randomization settings
        Rect isRandomRect = new(_position.position, new Vector2(24, 20));
        Rect thresholdSliderRect = isRandomRect;

        // put the threshold slider under the bool
        thresholdSliderRect.position += new Vector2(0, 20);
        thresholdSliderRect.width = _position.width;

        // if the station editor was just loaded, assign the default track count
        if (lastTrackWSize < 0)
            lastTrackWSize = trackWs.arraySize;

        // if the station is in a random order,
        if (EditorGUI.Toggle(isRandomRect, new GUIContent("Random Sequence"), isRandom.boolValue))
        {
            isRandom.boolValue = true;
            isShowingThreshold = true;

            // show the threshold for that order
            EditorGUI.Slider(thresholdSliderRect, threshold, 0, 1, new GUIContent("Threshold Before Repeats"));
        }
        else
        {
            isRandom.boolValue = false;
            isShowingThreshold = false;
        }

        // add the height of the random settings to the property height
        _position.position = new Vector2(
            _position.position.x, 
            _position.position.y + isRandomRect.height + (
                isShowingThreshold 
                ? thresholdSliderRect.height + 4 
                : 0
            )
        );

        // show the list of tracks
        EditorGUI.PropertyField(_position, trackWs, _label, true);

        // if the station was changed
        if (GUI.changed)
        {
            // and a new track was added
            if (trackWs.arraySize > lastTrackWSize)
            {
                // get the new element
                SerializedProperty newElement = trackWs.GetArrayElementAtIndex(trackWs.arraySize - 1);

                // get the stored track and track type
                SerializedProperty track = newElement.FindPropertyRelative("track");
                SerializedProperty trackType = newElement.FindPropertyRelative("trackType");

                // get the stored gain
                SerializedProperty gain = newElement.FindPropertyRelative("gain");

                // create an empty track for the station
                track.managedReferenceValue = StationRadioTrackWrapper.CreateTrackEditor(trackType.stringValue);

                // reset the track's gain
                gain.floatValue = 100;

                // store the new track list size
                lastTrackWSize++;
            }
            // if a track was removed
            else if (trackWs.arraySize < lastTrackWSize)
            { 
                // store the new track list size
                lastTrackWSize--; 
            }
        }

        // apply any inspector changes
        _property.serializedObject.ApplyModifiedProperties();

    }

    // get the height of the track in the inspector
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // if the random options are shown, add height accordingly
        float randomOptionsHeight = 20 + (isShowingThreshold ? 20 : 0);

        // get the height of the contained track list
        float listHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("stationTrackWs"));

        // combine the heights
        return randomOptionsHeight + listHeight;
    }
}
#endif