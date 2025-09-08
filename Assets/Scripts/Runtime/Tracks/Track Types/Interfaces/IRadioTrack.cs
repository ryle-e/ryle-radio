using System;

// internal interface for radiotracks, mainly kept for the purpose of logic issues
// tracks for stations can be accessed with an interface, so we should do the same with tracks in general

// truth be told i can't fully remember why i have this and RadioTrack as separate entities- i do know that i went through a lot of effort to make
// this layout work, though, and so for the moment i'm keeping it in- however if it turns out to be ultimately useless and can be deprecated without
// issue i'm happy to say goodbye to it

// use the RadioTrack class if you're making custom tracks
public interface IRadioTrack
{
    // the samples per second of this track right now (can change on some tracks)
    public abstract float SampleRate { get; set; }

    // the total amount of samples in this track right now (can change on some tracks)
    public abstract int SampleCount { get; set; }

    public abstract void Init();

    // get a sample at the provided index
    // this is the core method of a track- whatever you return here defines the audio that the track will play when selected
    // if you want to make a track that a triangle wave that oscillates between -1 and 1, for example
    public abstract float GetSample(int _sampleIndex); 

    // update a RadioTrackPlayer when this track ends- used in stations, and only works because a RadioData can only ever have one playing instance
    public virtual void AddToPlayerEndCallback(ref Action<RadioTrackPlayer> _callback) { }
}