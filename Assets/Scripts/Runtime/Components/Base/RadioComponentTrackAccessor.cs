
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public abstract class RadioComponentTrackAccessor : RadioComponent
{
    [SerializeField, Multiselect("TrackNames")]
    private int affectedTracks;
    private int lastAffectedTracks;


    protected List<string> TrackNames => data != null 
        ? data.TrackNames 
        : new() { "Data not assigned!" };

    public Action<RadioComponent> OnInit { get; set; } = new(_ => { });


    protected override void Init()
    {
        AssignToTracksGeneric();

        OnInit(this);
        AccessorInit();
    }

    private void AssignToTracksGeneric()
    {
        if (lastAffectedTracks != affectedTracks)
        {
            int removedTracks = lastAffectedTracks ^ affectedTracks;
            int[] oldIndexes = MultiselectAttribute.ToInt(removedTracks);

            for (int i = 0; i < oldIndexes.Length; i++)
            {
                RadioTrackWrapper track = data.TrackWrappers[oldIndexes[i]];
                RemoveFromTrack(track);
            }
        }

        int[] affectedIndexes = MultiselectAttribute.ToInt(affectedTracks);
        lastAffectedTracks = 0;

        for (int i = 0; i < affectedIndexes.Length; i++)
        {
            RadioTrackWrapper track = data.TrackWrappers[affectedIndexes[i]];
            AssignToTrack(track);

            lastAffectedTracks |= affectedIndexes[i];
        }
    }

    protected abstract void AssignToTrack(RadioTrackWrapper _track);
    protected abstract void RemoveFromTrack(RadioTrackWrapper _track);

    protected virtual void AccessorInit() { }

}