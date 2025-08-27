using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

public class StationRadioTrackWrapper
{
    public enum TrackType // ADD TO THIS IF YOU HAVE ANY CUSTOM TRACK TYPES
    {
        AudioClip,
        Procedural
    }

    [Range(0, 500)]
    public float gain = 100; // the volume of the track

    [SerializeReference]
    protected RadioTrack track; // the track itself

    public float SampleRate => track.SampleRate;
    public int Channels => track.Channels;
    public int SampleCount => track.SampleCount;


    public StationRadioTrackWrapper(RadioTrack _track)
    {
        track = _track;
    }


    // creates a new track in this wrapper, called when the track type is chosen
    public RadioTrack CreateTrack(TrackType _trackType)
    {
        switch (_trackType)
        {
            case TrackType.AudioClip:
                return new ClipRadioTrack();

            case TrackType.Procedural:
                return new ProceduralRadioTrack() { IsFinite = true };

            default:
                return null;
        }
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