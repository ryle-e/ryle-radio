using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "new test", menuName = "ryle radio/radio data")]
public class RadioData : ScriptableObject
{
    public const float LOW_TUNE = 0;
    public const float HIGH_TUNE = 1000;


    [SerializeField]
    private List<RadioTrackWrapper> trackWs = new() { new() };

    public List<RadioTrackWrapper> TrackWrappers => trackWs;

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
        if (trackWs.Count <= 0)
            return;

        trackNames = new List<string>();
        trackIDs = new List<string>();

        for (int i = TrackWrappers.Count - 1; i >= 0; i--)
        {
            RadioTrackWrapper track = TrackWrappers[i];

            var othersWithID = trackWs.Where(t => t.id == track.id);

            if (othersWithID.Count() > 1)
            {
                track.id += othersWithID.Count();
                //Debug.LogWarning("A RadioTrack has the same ID as a previous one! Changed ID to " + track.id);
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
        foreach (RadioTrackWrapper trackW in TrackWrappers)
            trackW.Init();

        RadioBroadcaster.InitBroadcasters();
    }

    public bool TryGetTrack(string _nameOrID, out RadioTrackWrapper _trackW, bool _useID = false)
    {
        string id = "";

        if (_useID)
            id = _nameOrID;
        else
            id = NameToID(_nameOrID);

        var found = TrackWrappers.Find(t => t.id == id);

        if (found != null)
        {
            _trackW = found;
            return true;
        }
        else
        {
            _trackW = null;
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

        var found = TrackWrappers.Find(t => t.id == id);

        if (found != null)
        {
            _index = TrackWrappers.IndexOf(found);
            return true;
        }
        else
        {
            _index = -1;
            return false;
        }
    }
}
