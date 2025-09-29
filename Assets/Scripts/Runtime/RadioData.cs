using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// the central data object containing the tracks and information for a radio
// has a custom editor in RadioDataEditor.cs
[CreateAssetMenu(fileName = "New Radio Data", menuName = "Ryle Radio/Radio Data")]
public class RadioData : ScriptableObject
{
    // i wanted this to be editable in the inspector too, but for now it's internally hardcoded- looking into changing this at some point
    public const float LOW_TUNE = 0; // the lower limit for tune on this radio
    public const float HIGH_TUNE = 1000; // the upper limit for tune on this radio

    // the colours of scene gizmos
    [SerializeField] private Color gizmoColor = new Color32(200, 180, 255, 255);
    [SerializeField] private Color gizmoColorSecondary = new Color32(175, 105, 205, 255);

    // all of the tracks in this radio- these make up the bulk of the radio itself
    [SerializeField] private List<RadioTrackWrapper> trackWs = new() { new() };

    public Color GizmoColor => gizmoColor; // aliases for gizmo colours, for safety
    public Color GizmoColorSecondary => gizmoColorSecondary;

    public List<RadioTrackWrapper> TrackWrappers => trackWs;

    public Action<RadioData> OnInit { get; set; } = new(_ => { }); // invoked when Init() is called
    public Action<RadioData> BeforeInit { get; set; } = new(_ => { }); // invoked when Init() is called, but before anything happens

    // the names of all tracks contained in this radio
    private List<string> trackNames;
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

    // the ids of all tracks contained in this radio
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


    // slices a given id to get the id
    public static string NameToID(string _name)
    {
        return _name.Split(", ")[0];
    }

    // fill the track id and id lists to match the current tracks
    private void PopulateTrackIDs()
    {
        // if there are no tracks, don't try to get the names
        if (trackWs.Count <= 0)
            return;

        // reset the track info lists
        trackNames = new List<string>();
        trackIDs = new List<string>();

        // for every track in the radio
        for (int i = TrackWrappers.Count - 1; i >= 0; i--)
        {
            // cache the track
            RadioTrackWrapper track = TrackWrappers[i];

            // find any other tracks with the same id as this one
            var othersWithID = trackWs.Where(t => t.id == track.id);

            // if there are others, change the id of this track to match
            // this only works as we are technically iterating backwards thanks to how inspector lists are working in this editor
            if (othersWithID.Count() > 1)
                track.id += othersWithID.Count(); // you can only add one new track at a time, so we just append the count to the end
                                                  // e.g a adding a track with the last one's id "music" will make the id of the new track "music2"

            // combine some track info into a display id
            string name = $"{track.id}, {track.range.x} - {track.range.y}";

            // store the id and id
            trackNames.Add(name);
            trackIDs.Add(track.id);

            // assign the id
            track.name = name;
        }
    }

    // when tracks are changed, update their info
    private void OnValidate()
    {
        PopulateTrackIDs();
    }

    // initialize this radio and all related components
    public void Init()
    {
        BeforeInit(this);

        // initialize all the tracks
        foreach (RadioTrackWrapper trackW in TrackWrappers)
            trackW.Init();

        // initialize all components, e.g broadcasters, insulators, outputs
        RadioComponent.InitAllComponents();

        OnInit(this);
    }

    // attempt to get a track from this radio using an id or id
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
