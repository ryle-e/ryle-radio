using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "new test", menuName = "ryle radio/radio data")]
public class RadioData : ScriptableObject
{
    public const float LOW_INDEX = 0;
    public const float HIGH_INDEX = 1000;

    private const float DEFAULT_INDEX_DIFF = 0.1f;

    [SerializeField] private List<RadioTrack> tracks = new() { 
        new() { gainCurve = RadioTrack.DefaultGainCurve, gain = 100, loop = true, playOnInit = true } 
    };

    public List<RadioTrack> Tracks => tracks;
    public List<string> TrackIDs { get; private set; }


    public void Init()
    {
        foreach (RadioTrack track in tracks)
        { 
            track.Init();

            var othersWithID = tracks.Where(t => t.id == track.id);

            if (othersWithID.Count() > 1)
            {
                track.id += othersWithID.Count();
                Debug.LogWarning("A RadioTrack has the same ID as a previous one! Changed ID to " + track.id);
            }

            TrackIDs.Add(track.id);
        }
    }

    public bool TryGetTrack(string _id, out RadioTrack _track)
    {
        var found = tracks.Find(t => t.id == _id);

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
}
