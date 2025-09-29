using System;

// a track in a RadioData, playable in a RadioOutput
// these are your basic objects to be played in the radio, can be clips, procedural, stations, and more if you create custom tracks
[System.Serializable]
public abstract class RadioTrack : IRadioTrack
{
    // main comments in IRadioTrack.cs

    // !!! IMPORTANT NOTE - if you're creating a custom RadioTrack, you need to both inherit from this class AND have a const string named DISPLAY_NAME
    // because the version of c# unity uses does not support static virtual data, we have no way of enforcing that you do this- but you will get errors otherwise
    // it's necessary for wrappers when they're listing each available track type in a dropdown- NaughtyAttributes' reflection system is used to pull
    // -the display id so that it's selectable in the inspector. the only downside is a lack of enforcement :(((

    public float SampleRate { get; set; }
    public virtual int SampleCount { get; set; }

    public abstract void Init();
    public abstract float GetSample(int _sampleIndex);

    public virtual void AddToPlayerEndCallback(ref Action<RadioTrackPlayer> _callback) { }
}