using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "new test", menuName = "ryle radio/radio data")]
public class RadioData : ScriptableObject
{
    public const float LOW_TUNE = 0;
    public const float HIGH_TUNE = 1000;


    [SerializeField]
    private List<RadioTrackWrapper> tracks = new() {
        new( new ClipRadioTrack() )
    };

    public List<RadioTrack> Tracks => tracks.Select(t => t.track).ToList();
    public List<RadioTrackWrapper> TrackWrappers => tracks;

    public List<string> TrackNames {
        get
        {
            if (trackNames == null || trackNames.Count <= 0)
                PopulateTrackIDs();

            return trackNames;
        }
    }
    private List<string> trackNames;

    public List<string> TrackIDs {
        get
        {
            if (trackIDs == null || trackIDs.Count <= 0)
                PopulateTrackIDs();

            return trackIDs;
        }
    }
    private List<string> trackIDs;


    public static string NameToID(string _name)
    {
        return _name.Split(", ")[0];
    }

    public void PopulateTrackIDs()
    {
        if (tracks.Count <= 0)
            return;

        trackNames = new List<string>();
        trackIDs = new List<string>();

        foreach (RadioTrackWrapper track in tracks)
        {
            var othersWithID = tracks.Where(t => t.id == track.id);

            if (othersWithID.Count() > 1)
            {
                track.id += othersWithID.Count();
                Debug.LogWarning("A RadioTrack has the same ID as a previous one! Changed ID to " + track.id);
            }

            trackNames.Add($"{track.id}, {track.range.x} - {track.range.y}");
            trackIDs.Add(track.id);
        }
    }

    public void OnValidate()
    {
        PopulateTrackIDs();
    }

    public void Init()
    {
        foreach (RadioTrack track in tracks)
        {
            track.Init();
        }

        RadioBroadcaster.InitBroadcasters();
    }

    public bool TryGetTrack(string _nameOrID, out RadioTrack _track, bool _useID = false)
    {
        string id = "";

        if (_useID)
            id = _nameOrID;
        else
            id = NameToID(_nameOrID);

        var found = tracks.Find(t => t.id == id);

        if (found != null)
        {
            _track = found;
            return true;
        }
        else
        {
            _track = null;
            return false;
        }
    }

    public bool TryGetTrackIndex(string _nameOrID, out int _index, bool _useID = false)
    {
        string id = "";

        if (_useID)
            id = _nameOrID;
        else
            id = NameToID(_nameOrID);

        var found = tracks.Find(t => t.id == id);

        if (found != null)
        {
            _index = tracks.IndexOf(found);
            return true;
        }
        else
        {
            _index = -1;
            return false;
        }
    }
}
