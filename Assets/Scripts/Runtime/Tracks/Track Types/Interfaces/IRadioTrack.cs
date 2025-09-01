using System;

public interface IRadioTrack
{
    public abstract float SampleRate { get; set; }
    public abstract int SampleCount { get; set; }

    public abstract void Init();
    public abstract float GetSample(int _sampleIndex);

    public virtual void AddToPlayerEndCallback(ref Action<RadioTrackPlayer> _callback) { }
}