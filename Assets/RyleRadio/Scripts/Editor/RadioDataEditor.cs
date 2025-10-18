using UnityEngine;

namespace RyleRadio.Editor
{
    using RyleRadio.Tracks;

#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine.UIElements;

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

        private SerializedProperty forceClipSampleRate; // whether or not all AudioClips are overriden to a specific sample rate
        private SerializedProperty forcedSampleRate; // the specific sample rate

        // for a foldout hiding advanced vars
        private bool showAdvanced = false;


        // on inspector init
        private void OnEnable()
        {
            // store gizmo colours
            gizmoColor = serializedObject.FindProperty("gizmoColor");
            gizmoColorSecondary = serializedObject.FindProperty("gizmoColorSecondary");

            forceClipSampleRate = serializedObject.FindProperty("forceClipSampleRate");
            forcedSampleRate = serializedObject.FindProperty("forcedSampleRate");

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


                // toggle for whether or not the user wants to force a sample rate on clips referenced in this radio
                forceClipSampleRate.boolValue = EditorGUILayout.BeginToggleGroup("Force sample rate on Clips", forceClipSampleRate.boolValue);
                
                // the sample rate to force on the clips
                EditorGUILayout.PropertyField(forcedSampleRate, new GUIContent("Forced sample rate"));

                // a button to manually perform the sample rate forcing
                if (GUILayout.Button(new GUIContent("Force Clips to sample rate")))
                    ForceClipsToSampleRate();

                // finish the toggled area
                EditorGUILayout.EndToggleGroup();


                // button to manually clear the name and ID caches of the radio
                if (GUILayout.Button(new GUIContent("Clear Cache")))
                    ((RadioData)serializedObject.targetObject).ClearCache();

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
                    SerializedProperty rangeCurve = newElement.FindPropertyRelative("rangeCurve");

                    // reset the internal track class
                    radioTrack.managedReferenceValue = RadioTrackWrapper.CreateTrackEditor(trackType.stringValue);

                    // reset the gain and the gain curve
                    gain.floatValue = 100;
                    rangeCurve.animationCurveValue = new(RadioTrackWrapper.DefaultRangeCurve.keys);

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

        // get all AudioClips referenced in this radio, and override their sample rates in one go
        // we do this to stop distortion in RadioOutput when converting between sample rates- optional if you're okay with a little distortion
        private void ForceClipsToSampleRate()
        {
            // for every track in this radio,
            for (int i = 0; i < trackWs.arraySize; i++)
            {
                // get its wrapper
                SerializedProperty wrapper = trackWs.GetArrayElementAtIndex(i);

                // if the wrapper is a clip,
                if (wrapper.FindPropertyRelative("track").managedReferenceValue is ClipRadioTrack clipTrack)
                {
                    // force the clip's sample rate
                    ForceSingleClipToSampleRate(clipTrack.clip);
                }
                // otherwise if the wrapper is a station (and therefore could contain a clip)
                else if (wrapper.FindPropertyRelative("track").managedReferenceValue is StationRadioTrack stationTrack)
                {
                    // for every track in this station
                    foreach (StationRadioTrackWrapper sTrackW in stationTrack.stationTrackWs)
                    {
                        // if the track has a referenced clip (is a ClipRadioTrack)
                        if (sTrackW.EditorChildClip != null)
                            ForceSingleClipToSampleRate(sTrackW.EditorChildClip); // force its sample rate
                    }
                }
            }
        }

        // force the sample rate on a specific clip
        private void ForceSingleClipToSampleRate(AudioClip _clip)
        {
            // get the import settings for the clip
            AudioImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(_clip)) as AudioImporter;

            // get the current build target of the game
            string buildTarget = BuildPipeline.GetBuildTargetName(EditorUserBuildSettings.activeBuildTarget);

            // get the custom sample settings of the clip
            var sampleSettings = importer.GetOverrideSampleSettings(buildTarget);

            // enable the sample rate override
            sampleSettings.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;

            // save whatever the sample rate is now
            uint lastSampleRate = sampleSettings.sampleRateOverride;

            // if a chosen sample rate has been provided,
            if (forceClipSampleRate.intValue > 0) 
                sampleSettings.sampleRateOverride = (uint)forcedSampleRate.intValue; // set it to that
            else // otherwise if it's set to 0 or less,
                sampleSettings.sampleRateOverride = (uint)AudioSettings.outputSampleRate; // set it to the game's default

            // if the sample rate was changed, let the user know
            if (sampleSettings.sampleRateOverride !=  lastSampleRate)
                Debug.Log("Changed clip {_clip} sample rate to {sampleSettings.sampleRateOverride} for platform {buildTarget}.");

            // apply the sample rate override
            importer.SetOverrideSampleSettings(buildTarget, sampleSettings);
        }
    }
#endif

}