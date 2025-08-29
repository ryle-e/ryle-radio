using NaughtyAttributes;
using System;
using Unity.Multiplayer.Center.Common;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class StationRadioTrackWrapper
{
    public enum TrackType // ADD TO THIS IF YOU HAVE ANY CUSTOM TRACK TYPES
    {
        AudioClip,
        Procedural
    }

    [SerializeField] 
    private string id;

    [Range(0, 500)]
    public float gain = 100; // the volume of the track

    public Vector2 startAndEndRests = Vector2.zero;

    [SerializeField, AllowNesting, OnValueChanged("CreateTrack")]
    private TrackType trackType = TrackType.AudioClip;

    [SerializeReference]
    protected RadioTrack track; // the track itself

    public float SampleRate => track.SampleRate;

    public int SampleCount
    {
        get
        {
            return track.SampleCount + (int)((startAndEndRests.x + startAndEndRests.y) * track.SampleRate);
        }
    }

    public string ID => id;


    public StationRadioTrackWrapper(RadioTrack _track)
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
        track = trackType switch
        {
            TrackType.AudioClip => new ClipRadioTrack(),
            TrackType.Procedural => new ProceduralRadioTrack() { IsFinite = true },
            _ => new ClipRadioTrack(),
        };
    }

    public static RadioTrack CreateTrackEditor(int _type)
    {
        return (TrackType)_type switch
        {
            TrackType.AudioClip => new ClipRadioTrack(),
            TrackType.Procedural => new ProceduralRadioTrack() { IsFinite = true },
            _ => new ClipRadioTrack(),
        };
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