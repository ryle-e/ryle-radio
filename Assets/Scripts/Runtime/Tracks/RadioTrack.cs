using System;
using UnityEditor;

[System.Serializable]
public abstract class RadioTrack : IRadioTrack
{
    public float SampleRate { get; set; }
    public virtual int SampleCount { get; set; }

    public abstract void Init();
    public abstract float GetSample(int _sampleIndex);

    public virtual void AddToPlayerEndCallback(ref Action<RadioTrackPlayer> _callback) { }
}