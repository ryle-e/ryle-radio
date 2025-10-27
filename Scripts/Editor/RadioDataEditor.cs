using UnityEngine;

namespace RyleRadio.Editor
{
    using RyleRadio.Tracks;

#if UNITY_EDITOR
    using UnityEditor;

    /// <summary>
    /// A custom editor for \ref RadioData
    /// <br><br> This mainly exists so that we can reset tracks when they're newly added to the RadioData and prevent them from being linked- I don't think it actually does its job for that, though
    /// <br><br><b>See: </b>\ref RadioData
    /// </summary>
    [CustomEditor(typeof(RadioData))]
    public class RadioDataEditor : Editor
    {
        /// The tracks contained in this radio
        private SerializedProperty trackWs;
        /// The last recorded number of tracks contained in this radio
        private int lastTrackWSize;

        /// The primary colour of gizmos on components referencing this radio
        private SerializedProperty gizmoColor;
        /// The secondary colour of gizmos on components referencing this radio
        private SerializedProperty gizmoColorSecondary;

        /// Toggles whether or not we should force the sample rate on AudioClips used in this radio
        private SerializedProperty forceClipSampleRate;
        /// The sample rate we're forcing on AudioClips used in this radio- if left at 0, picks the project's default sample rate
        private SerializedProperty forcedSampleRate;

        /// Toggle drawing of advanced settings
        private bool showAdvanced = false;


        /// <summary>
        /// Initializes this object on inspector init
        /// </summary>
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

                // get the internal track class and the string eventType
                SerializedProperty radioTrack = track.FindPropertyRelative("track");
                SerializedProperty trackType = track.FindPropertyRelative("trackType");

                // if the track itself is null somehow, (e.g it's the first track added when the data is created)
                if (radioTrack.managedReferenceValue == null)
                {
                    // set a default track class based on the listed track eventType string
                    radioTrack.managedReferenceValue = RadioTrackWrapper.CreateTrackEditor(trackType.stringValue);
                }
                else
                {
                    // set the track eventType string to whatever the internal track class is
                    trackType.stringValue = RadioTrackWrapper.GetTrackType(radioTrack.managedReferenceValue.ToString());
                }
            }

            // apply the changes
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws variables, buttons, etc
        /// </summary>
        public override void OnInspectorGUI()
        {
            // update the rest of the inspector
            serializedObject.Update();

            // show the foldout that hides advanced vars
            showAdvanced = EditorGUILayout.Foldout(showAdvanced, new GUIContent("Advanced Settings"));

            // if the foldout is open,
            if (showAdvanced)
            {
                DrawAdvancedOptions();
            }

            // display the tracks
            EditorGUILayout.PropertyField(trackWs, new GUIContent("Tracks", "These are actually RadioTrackWrappers, not RadioTracks"));

            // if something has been changed
            if (GUI.changed)
            {
                // if a track has been added,
                if (trackWs.arraySize > lastTrackWSize)
                {
                    InitNewTrack();
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

        /// <summary>
        /// Draws the advanced options (gizmo colours, sample rate forcing, cache clear)
        /// </summary>
        private void DrawAdvancedOptions()
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

        /// <summary>
        /// Initializes a newly created track
        /// </summary>
        private void InitNewTrack()
        {
            // cache it
            SerializedProperty newElement = trackWs.GetArrayElementAtIndex(trackWs.arraySize - 1);

            // get the internal class and eventType
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

        /// <summary>
        /// Get all AudioClips used in this radio, and override their sample rates all at once. This is done to reduce distortion when converting between sample rates at runtime- if you're okay with the distortion there's no issue
        /// </summary>
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

        /// <summary>
        /// Override the sample rate of a specific AudioClip
        /// </summary>
        /// <param name="_clip">The clip to override the sample rate on</param>
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