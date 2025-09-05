using System;

// a track in a RadioData, playable in a RadioListener
// these are your basic objects to be played in the radio, can be clips, procedural, stations, and more if you create custom tracks
[System.Serializable]
public abstract class RadioTrack : IRadioTrack
{
    // variable comments in IRadioTrack

    public float SampleRate { get; set; }
    public virtual int SampleCount { get; set; }

    public abstract void Init();
    public abstract float GetSample(int _sampleIndex);

    public virtual void AddToPlayerEndCallback(ref Action<RadioTrackPlayer> _callback) { }
}