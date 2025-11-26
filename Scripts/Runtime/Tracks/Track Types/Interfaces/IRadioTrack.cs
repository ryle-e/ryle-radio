using System;

namespace RyleRadio.Tracks
{

    // internal interface for radiotracks, mainly kept for the purpose of logic issues
    // tracks for stations can be accessed with an interface, so we should do the same with tracks in general



    // use the RadioTrack class if you're making custom tracks
    /// <summary>
    /// Internal interface for a \ref RadioTrack
    /// </summary>
    /// <remarks>
    /// <i>truth be told i can't fully remember why i have this and RadioTrack as separate entities- i do know that i went through a lot of effort to make 
    ///this layout work, though, and so for the moment i'm keeping it in- however if it turns out to be ultimately useless and can be deprecated without
    ///issue i'm happy to say goodbye to it</i>
    ///</remarks>
    public interface IRadioTrack
    {
        /// <summary>
        /// The samples per second of this track right now. Can be changed at runtime, e.g: StationRadioTrack
        /// </summary>
        public abstract float SampleRate { get; set; }

        /// <summary>
        /// The total number of samples in this track right now. Can be changed at runtime, e.g: StationRadioTrack
        /// </summary>
        public abstract int SampleCount { get; set; }

        /// <summary>
        /// Initializes this track.
        /// </summary>
        public abstract void Init();

        /// <summary>
        /// Get a sample at the provided index. This is the core method of a track- whatever you return here defines the audio that the track will play when selected
        /// </summary>
        /// <param name="_sampleIndex">The index of the sample to get</param>
        /// <returns>The sample- a value between -1 and 1 representing the y-value of the sound wave</returns>
        public abstract float GetSample(int _sampleIndex);

        /// <summary>
        /// Update a RadioTrackPlayer when this current track ends. Used in StationRadioTrack.
        /// 
        /// <b>See also:</b> \ref RadioTrackPlayer.OnEnd, \ref StationRadioTrack.AddToPlayerEndCallback()
        /// </summary>
        /// <param name="_callback">The function to run when the track ends</param>
        public virtual void AddToPlayerEndCallback(ref Action<RadioTrackPlayer> _callback) { }
    }

}