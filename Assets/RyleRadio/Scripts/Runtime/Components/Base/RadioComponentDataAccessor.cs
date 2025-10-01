using RyleRadio.Tracks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyleRadio.Components.Base
{

    // an expansion of a RadioComponent that can access specific tracks
    public abstract class RadioComponentDataAccessor : RadioComponent
    {
        [SerializeField, Multiselect("TrackNames")]
        private int affectedTracks; // the tracks to affect
        private int lastAffectedTracks; // the tracks that were previously affected- used if the affected tracks are changed at runtime

        // the tracks to choose from on the RadioData
        protected List<string> TrackNames => data != null
            ? data.TrackNames
            : new() { "Data not assigned!" };

        // called when the component is initialized
        public Action<RadioComponent> OnInit { get; set; } = new(_ => { });


        public override void Init()
        {
            // apply this component to selected tracks
            AssignToTracksGeneric();

            // initialize the rest of this component
            OnInit(this);
            AccessorInit();
        }

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

        // methods to link and unlink this component to a track
        protected abstract void AssignToTrack(RadioTrackWrapper _track);
        protected abstract void RemoveFromTrack(RadioTrackWrapper _track);

        // initialize this component if needed
        protected virtual void AccessorInit() { }

    }

}