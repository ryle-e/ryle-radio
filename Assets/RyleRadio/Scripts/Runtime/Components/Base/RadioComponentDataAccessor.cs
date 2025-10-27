using RyleRadio.Tracks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyleRadio.Components.Base
{

    /// <summary>
    /// An extension of \ref RadioComponent that accesses specific tracks on the stored \ref RadioData
    /// </summary>
    public abstract class RadioComponentDataAccessor : RadioComponent
    {
        /// <summary>
        /// The tracks that this component affects. This is displayed with a \ref MultiselectAttribute
        /// </summary>
        [SerializeField, Multiselect("TrackNames")]
        private int affectedTracks;

        /// <summary>
        /// The tracks that were affected previously- only matters if affected tracks are changed at runtime (which currently is not possible)
        /// </summary>
        private int lastAffectedTracks;

        /// <summary>
        /// The list of tracks on the \ref RadioData that this component can choose from
        /// </summary>
        protected List<string> TrackNames => data != null
            ? data.TrackNames
            : new() { "Data not assigned!" };

        /// <summary>
        /// Event called when the component is initialized
        /// </summary>
        public Action<RadioComponent> OnInit { get; set; } = new(_ => { });


        /// <summary>
        /// Initialises this component and links its affected tracks
        /// </summary>
        public override void Init()
        {
            // apply this component to selected tracks
            AssignToTracksGeneric();

            // initialize the rest of this component
            OnInit(this);
            AccessorInit();
        }

        /// <summary>
        /// Generally applicable method that converts \ref affectedTracks to a list of tracks, then calls \ref AssignToTrack() to link this component to each of them
        /// </summary>
        private void AssignToTracksGeneric()
        {
            // if the tracks have been changed since last initialize,
            if (lastAffectedTracks != affectedTracks)
            {
                // find the different tracks as flags
                int removedTracks = lastAffectedTracks ^ affectedTracks;

                // convert those flags to indexes
                int[] oldIndexes = MultiselectAttribute.ToInt(removedTracks);

                // for each index,
                for (int i = 0; i < oldIndexes.Length; i++)
                {
                    // remove this component from the track
                    RadioTrackWrapper track = data.TrackWrappers[oldIndexes[i]];
                    RemoveFromTrack(track);
                }
            }

            // convert the affected tracks from a flag int to indexes
            int[] affectedIndexes = MultiselectAttribute.ToInt(affectedTracks);
            lastAffectedTracks = 0;

            // for each index,
            for (int i = 0; i < affectedIndexes.Length; i++)
            {
                // assign this component to the indexed track
                RadioTrackWrapper track = data.TrackWrappers[affectedIndexes[i]];
                AssignToTrack(track);
            }

            // save the last affected tracks
            lastAffectedTracks = affectedTracks;
        }

        /// Links this component to a track
        protected abstract void AssignToTrack(RadioTrackWrapper _track);
        /// Unlinks this component from a track
        protected abstract void RemoveFromTrack(RadioTrackWrapper _track);

        /// <summary>
        /// Allows extra code for initialization so that \ref Init() can still be called
        /// </summary>
        protected virtual void AccessorInit() { }

    }

}