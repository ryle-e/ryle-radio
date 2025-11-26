using RyleRadio.Tracks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#pragma warning disable 0414 // we do this as the force clip sample rate variables are technically not used in code- but they are in the inspector

namespace RyleRadio
{
    /// <summary>
    /// The central data object defining the radio. Contains the tracks and information required to play the radio at runtime.
    /// <br><br>Has a custom editor at \ref RadioDataEditor
    /// </summary>
    [CreateAssetMenu(fileName = "New Radio Data", menuName = "Ryle Radio/Radio Data")]
    public class RadioData : ScriptableObject
    {
        // i wanted these to be editable in the inspector too, but for now it's internally hardcoded- looking into changing this at some point
        /// The lower limit for tune on this radio. This may become non-const at some point
        public const float LOW_TUNE = 0;
        /// The upper limit for tune on this radio. This may become non-const at some point
        public const float HIGH_TUNE = 1000;

        /// The primary colour of gizmos relating to this radio
        [SerializeField] private Color gizmoColor = new Color32(200, 180, 255, 255);
        /// The secondary colour of gizmos relating to this radio
        [SerializeField] private Color gizmoColorSecondary = new Color32(175, 105, 205, 255);

        /// Whether or not this radio forces the sample rate on AudioClips it references
        [SerializeField] private bool forceClipSampleRate = true;
        /// The sample rate this radio can force on AudioClips it references. If left at 0, it chooses the project's default sample rate
        [SerializeField] private int forcedSampleRate = 0;

        /// <summary>
        /// The tracks contained in this radio, editable in the inspector
        /// </summary>
        [SerializeField] private List<RadioTrackWrapper> trackWs = new() { new() };


        /// Alias for \ref gizmoColor for safety
        public Color GizmoColor => gizmoColor;
        /// Alias for \ref gizmoColorSecondary for safety
        public Color GizmoColorSecondary => gizmoColorSecondary;

        /// Alias for \ref trackWs for safety- in documentation we usually call them the tracks, but for code clarity we explicitly call them wrappers in this object
        public List<RadioTrackWrapper> TrackWrappers => trackWs;


        /// Event invoked when \ref Init() is called
        public Action<RadioData> OnInit { get; set; } = new(_ => { });
        /// Event invoked when \ref Init() is called, but at the beginning before anything happens
        public Action<RadioData> BeforeInit { get; set; } = new(_ => { });


#if !SKIP_IN_DOXYGEN 
        // the names of all tracks contained in this radio
        private List<string> trackNames;
#endif
        /// <summary>
        /// The names of all tracks stored in this radio, used when selecting them in the inspector
        /// </summary>
        public List<string> TrackNames
        {
            get
            {
                // if the names haven't been generated, generate them
                if (trackNames == null || trackNames.Count <= 0)
                    PopulateTrackIDs();

                return trackNames;
            }
        }

        /// <summary>
        /// The IDs of all tracks stored in this radio
        /// </summary>
        private List<string> trackIDs;
        public List<string> TrackIDs
        {
            get
            {
                // if the ids haven't been generated, generate them
                if (trackIDs == null || trackIDs.Count <= 0)
                    PopulateTrackIDs();

                return trackIDs;
            }
        }


        /// <summary>
        /// Converts a track's name to ID format
        /// </summary>
        /// <param name="_name">The name to convert</param>
        /// <returns>The name transformed into ID format</returns>
        public static string NameToID(string _name)
        {
            return _name.Split(", ")[0];
        }

        /// <summary>
        /// Fills \ref TrackNames and \ref TrackIDs to match the current content of \ref trackWs
        /// </summary>
        private void PopulateTrackIDs()
        {
            // if there are no tracks, don't try to get the names
            if (trackWs.Count <= 0)
                return;

            // reset the track info lists
            trackNames = new List<string>();
            trackIDs = new List<string>();

            // for every track in the radio
            for (int i = 0; i < TrackWrappers.Count; i++)
            {
                // cache the track
                RadioTrackWrapper track = TrackWrappers[i];

                // find any other tracks with the same id as this one
                var othersWithID = trackIDs.Where(t => t == track.id);

                // if there are others, change the id of this track to match
                if (othersWithID.Count() > 0)
                    track.id += othersWithID.Count(); // you can only add one new track at a time, so we just append the count to the end
                                                      // e.g a adding a track with the last one's id "music" will make the id of the new track "music1"

                // combine some track info into a display id
                string name = $"{track.id}, {track.range.x} - {track.range.y}";

                // store the id and id
                trackNames.Add(name);
                trackIDs.Add(track.id);

                // assign the id
                track.name = name;
            }
        }

        /// <summary>
        /// Updates the track names and IDs when this object is changed
        /// </summary>
        private void OnValidate()
        {
            PopulateTrackIDs();
        }

        /// <summary>
        /// Initialise this radio, its tracks, and referenced components
        /// </summary>
        public void Init()
        {
            BeforeInit(this);

            // initialize all the tracks
            foreach (RadioTrackWrapper trackW in TrackWrappers)
                trackW.Init();

            OnInit(this);
        }

        /// <summary>
        /// Clears track names and IDs
        /// </summary>
        public void ClearCache()
        {
            TrackNames.Clear();
            TrackIDs.Clear();
        }

        /// <summary>
        /// Attempts to find a track in this radio using either an ID or a name
        /// </summary>
        /// <param name="_idOrName">The ID or name of the track to find</param>
        /// <param name="_trackW">The track that has been found, or null if none was found</param>
        /// <param name="_useID">If true, this method searches for a matching ID. If false, it searches for a matching name</param>
        /// <returns>True if a track was found, false if not</returns>
        public bool TryGetTrack(string _idOrName, out RadioTrackWrapper _trackW, bool _useID = true)
        {
            // either an id or id can be supplied here, but we always convert it to an id for this
            string id = "";

            if (_useID) // if an id was provided, use it
                id = _idOrName;
            else // if a id was provided, convert it to the id
                id = NameToID(_idOrName);

            // find any track with that id
            var found = TrackWrappers.Find(t => t.id == id);

            // if a track is found,
            if (found != null)
            {
                // output it and return true
                _trackW = found;
                return true;
            }
            else // otherwise
            {
                // output it and return false
                _trackW = null;
                return false;
            }
        }
    }

}