using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "New Radio Data", menuName = "Ryle Radio/Radio Data")]
public class RadioData : ScriptableObject
{
    public const float LOW_TUNE = 0;
    public const float HIGH_TUNE = 1000;

    [SerializeField] private Color gizmoColor = new Color32(200, 180, 255, 255);
    [SerializeField] private Color gizmoColorSecondary = new Color32(175, 105, 205, 255);

    [SerializeField]
    private List<RadioTrackWrapper> trackWs = new() { new() };

    public Color GizmoColor => gizmoColor;
    public Color GizmoColorSecondary => gizmoColorSecondary;

    public List<RadioTrackWrapper> TrackWrappers => trackWs;

    public Action<RadioData> OnInit { get; set; } = new(_ => { });
    public Action<RadioData> BeforeInit { get; set; } = new(_ => { });

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

        for (int i = 0; i < TrackWrappers.Count; i++)
        {
            RadioTrackWrapper track = TrackWrappers[i];

            var othersWithID = trackWs.Where(t => t.id == track.id);

            if (othersWithID.Count() > 1)
                track.id += othersWithID.Count();

            string name = $"{track.id}, {track.range.x} - {track.range.y}";

            trackNames.Add(name);
            trackIDs.Add(track.id);

            track.name = name;
        }
    }

    public void OnValidate()
    {
        PopulateTrackIDs();
    }

    public void Init()
    {
        BeforeInit(this);

        foreach (RadioTrackWrapper trackW in TrackWrappers)
            trackW.Init();

        RadioComponent.InitAllComponents();

        OnInit(this);
    }

    public bool TryGetTrack(string _idOrName, out RadioTrackWrapper _trackW, bool _useID = true)
    {
        string id = "";

        if (_useID)
            id = _idOrName;
        else
            id = NameToID(_idOrName);

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

    public bool TryGetTrackIndex(string _idOrName, out int _index, bool _useID = true)
    {
        string id = "";

        if (_useID)
            id = _idOrName;
        else
            id = NameToID(_idOrName);

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
