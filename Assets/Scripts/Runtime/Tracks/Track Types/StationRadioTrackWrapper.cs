using NaughtyAttributes;
using Unity.Multiplayer.Center.Common;
using UnityEngine;

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

    [SerializeField, AllowNesting, OnValueChanged("CreateTrack")]
    private TrackType trackType = TrackType.AudioClip;

    [SerializeReference]
    protected RadioTrack track; // the track itself

    public float SampleRate => track.SampleRate;
    public int SampleCount => track.SampleCount;

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

    public float GetGain(float _tune, float _otherGain)
    {
        float gainPower = gain / 100f; // get the volume based on the gain variable

        return gainPower;
    }

    public float GetSample(int _sampleIndex)
    {
        return track.GetSample(_sampleIndex);
    }
}