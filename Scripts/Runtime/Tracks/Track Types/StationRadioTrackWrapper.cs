using NaughtyAttributes;
using System;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RyleRadio.Tracks
{
    /// <summary>
    /// A smaller, separate version of \ref RadioTrackWrapper for use in a \ref StationRadioTrack
    ///
    /// This is how child tracks are contained inside stations- very similar to how normal tracks themselves are stored in \ref RadioData
    ///  
    /// We could probably use an interface in future for both this and RadioTrackWrapper, but I fear we won't ever use that interface again and it would just obfuscate functionality
    /// If it'd be useful, though, I'm happy to implement
    /// </summary>
    [System.Serializable]
    public class StationRadioTrackWrapper
    {
        /// <summary>
        /// The ID of this track
        /// </summary>
        [SerializeField] private string id;

        /// <summary>
        /// The additional volume of this track. See \ref RadioTrackWrapper.gain
        /// </summary>
        [Range(0, 500)] public float gain = 100;

        /// <summary>
        /// The amount of time at the start and end of the track for which there's silence. This allows you to spread tracks apart a little bit
        /// </summary>
        public Vector2 startAndEndRests = Vector2.zero;

        /// <summary>
        /// The current eventType of this track as chosen in the editor. Displayed as a dropdown of \ref RadioTrack `DISPLAY_NAME`s
        /// </summary>
        [SerializeField, AllowNesting, OnValueChanged("CreateTrackLocal"), Dropdown("TrackNames")]
        private string trackType = "None";

        /// <summary>
        /// The track contained in this wrapper.<br>
        /// Note that this is an \ref IStationTrack and not an \ref IRadioTrack like in the normal \ref RadioTrackWrapper
        /// Some custom track types might not be usable in a station (e.g a station within a station)- hence we need to separate those that can be in one from those that can't
        /// </summary>
        [SerializeReference]
        protected IStationTrack track;

#if !SKIP_IN_DOXYGEN
        // the ID of this track
        public string ID => id;

        // the sample rate of this track
        public float SampleRate => track.SampleRate;

        // the number of samples in this track
        public int SampleCount
        {
            get
            {
                // adds the rests to the number of samples in this track
                return track.SampleCount + (int)((startAndEndRests.x + startAndEndRests.y) * track.SampleRate);
            }
        }
#endif

        /// <summary>
        /// The gain value scaled down to ones- e.g \ref gain at 200 is \ref Gain at 2
        /// </summary>
        public float Gain => gain / 100f;

#if !SKIP_IN_DOXYGEN
        private static Type[] trackTypes;
#endif
        /// <summary>
        /// A list of each eventType of track that this wrapper can contain- this is anything that inherits from \ref IStationTrack
        /// <br><br><b>See also: </b> \ref RadioUtils.FindDerivedTypes()
        /// </summary>
        private static Type[] TrackTypes
        {
            get
            {
                // just like RadioTrackWrapper, get all available track types dynamically
                trackTypes ??= RadioUtils.FindDerivedTypes(typeof(IStationTrack));

                return trackTypes;
            }
        }

#if !SKIP_IN_DOXYGEN
        private static string[] trackNames;
#endif
        /// <summary>
        /// A list of the names of each eventType in \ref TrackTypes
        /// This is what's displayed in the inspector for the user to choose from
        /// </summary>
        private static string[] TrackNames
        {
            get
            {
                // convert available track types to their class names, as usual
                trackNames ??= TrackTypes
                    .Select(t => (string)t.GetField("DISPLAY_NAME").GetValue(null))
                    .ToArray();

                return trackNames;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// If this wrapper contains a \ref ClipRadioTrack , this becomes a reference to the track's clip. Used when forcing sample rates in \ref RadioDataEditor.ForceSampleRate()
        /// </summary>
        public AudioClip EditorChildClip => (track is ClipRadioTrack clipTrack) ? clipTrack.clip : null;
#endif

        /// <summary>
        /// Creates an empty wrapper for a station
        /// </summary>
        /// <param name="_track">The default track eventType to use</param>
        public StationRadioTrackWrapper(IStationTrack _track)
        {
            track = _track;
            gain = 100;

            // ensure this wrapper is not empty
            CreateTrackLocal();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Updates track types and names on script reload to detect any new types
        /// </summary>
        [InitializeOnLoadMethod]
        public static void OnReload()
        {
            trackTypes = null;
            trackNames = null;
        }
#endif

        /// <summary>
        /// Gets a new track to be used in a wrapper- note that this is marked as `static`<br>
        /// This is mainly used in the editor to update the track eventType when it's selected on \ref trackType
        /// </summary>
        /// <param name="_name">The name of the track eventType to create</param>
        /// <returns>A new track with the given eventType</returns>
        public static IStationTrack CreateTrackEditor(string _name)
        {
            // get the index of the chosen track eventType
            int index = Array.IndexOf(TrackNames, _name);

            // if you somehow have an invalid track eventType, don't create anything
            if (index < 0)
                return null;

            // create the track generically
            // more info in RadioTrackWrapper.CreateTrackEditor
            IStationTrack outTrack = (IStationTrack)Activator.CreateInstance(TrackTypes[index]);
            outTrack.IsInStation = true;

            // return the track
            return outTrack;
        }

        /// <summary>
        /// Initialize the track stored in this wrapper
        /// </summary>
        public void Init()
        {
            track.Init();
        }

        /// <summary>
        /// Creates a new track in this wrapper. This is called when \ref trackType is updated
        /// </summary>
        public void CreateTrackLocal()
        {
            track = CreateTrackEditor(trackType);
        }

        /// <summary>
        /// Gets a sample from the contained track.
        /// </summary>
        /// <param name="_sampleIndex">Index of the sample to get</param>
        /// <returns>A sample from the contained track</returns>
        public float GetSample(int _sampleIndex)
        {
            // if the track has a rest at the beginning at that sample index, return a silent sample
            if (_sampleIndex < startAndEndRests.x)
                return 0;
            // same thing but for a rest at the end of the track
            else if (_sampleIndex > (track.SampleCount + startAndEndRests.x))
                return 0;

            // otherwise, return the sample from the track
            return track.GetSample(_sampleIndex);
        }
    }

}