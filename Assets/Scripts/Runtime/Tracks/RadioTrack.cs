using System;

[System.Serializable]
public abstract class RadioTrack
{
    public static abstract string DisplayName { get; }

    public float SampleRate { get; protected set; }
    public virtual int SampleCount { get; protected set; }


    public abstract void Init();
    public abstract float GetSample(int _sampleIndex);

    public virtual void AddToPlayerEndCallback(ref Action<RadioTrackPlayer> _callback) { }
}