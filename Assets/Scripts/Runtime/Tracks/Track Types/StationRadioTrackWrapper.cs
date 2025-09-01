using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.Callbacks;
#endif

[System.Serializable]
public class StationRadioTrackWrapper
{
    [SerializeField] 
    private string id;

    [Range(0, 500)]
    public float gain = 100; // the volume of the track

    public Vector2 startAndEndRests = Vector2.zero;

    [SerializeField, AllowNesting, OnValueChanged("CreateTrack"), Dropdown("TrackNames")]
    private string trackType = "None";

    [SerializeReference]
    protected IStationTrack track; // the track itself

    public float SampleRate => track.SampleRate;

    public int SampleCount
    {
        get
        {
            return track.SampleCount + (int)((startAndEndRests.x + startAndEndRests.y) * track.SampleRate);
        }
    }

    public string ID => id;

    private static Type[] trackTypes;
    private static Type[] TrackTypes
    {
        get
        {
            trackTypes ??= RadioUtils.FindDerivedTypes(typeof(IStationTrack));

            return trackTypes;
        }
    }

    private static string[] trackNames;
    private static string[] TrackNames { 
        get 
        {
            trackNames ??= TrackTypes
                .Select(t => (string)t.GetField("DISPLAY_NAME").GetValue(null))
                .ToArray();

            return trackNames;
        } 
    }

    public static void OnScriptReload()
    {
        trackTypes = null;
        trackNames = null;
    }

    public StationRadioTrackWrapper(IStationTrack _track)
    {
        track = _track;
        gain = 100;

        CreateTrack();
    }

    public void Init()
    {
        track.Init();
    }


    // creates a new track in this wrapper, called when the track type is chosen
    public void CreateTrack()
    {
        int index = Array.IndexOf(TrackNames, trackType);

        track = (IStationTrack) Activator.CreateInstance(TrackTypes[index]);
    }

    public static IStationTrack CreateTrackEditor(string _name)
    {
        int index = Array.IndexOf(TrackNames, _name);

        IStationTrack outTrack = (IStationTrack)Activator.CreateInstance(TrackTypes[index]);

        if (outTrack is ProceduralRadioTrack procTrack)
            procTrack.IsFinite = true;

        return outTrack;
    }

    public float GetGain()
    {
        float gainPower = gain / 100f; // get the volume based on the gain variable

        return gainPower;
    }

    public float GetSample(int _sampleIndex)
    {
        if (_sampleIndex < startAndEndRests.x)
            return 0;
        else if (_sampleIndex > (track.SampleCount + startAndEndRests.x))
            return 0;

        return track.GetSample(_sampleIndex);
    }
}