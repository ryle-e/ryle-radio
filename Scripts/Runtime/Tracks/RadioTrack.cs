using System;

namespace RyleRadio.Tracks
{
    // a track in a RadioData, playable in a RadioOutput
    // these are your basic objects to be played in the radio, can be clips, procedural, stations, and more if you create custom tracks
    /// <summary>
    /// A track to play as part of a radio. These are the fundamental objects that define the content of the radio, such as clips, procedural audio, stations, and any custom content you create.
    /// </summary>
    /// <see cref="IRadioTrack"/>
    /// <remarks><b>NOTE FOR CUSTOM TRACK CREATORS!!</b>
    /// 
    /// !!! IMPORTANT NOTE - if you're creating a custom RadioTrack, you need to both inherit from this class AND have a const string named `DISPLAY_NAME`.
    /// Because the version of c# unity uses does not support static virtual data, we have no way of enforcing that you do this- but you will get errors otherwise
    /// It's necessary for wrappers when they're listing each available track eventType in a dropdown- NaughtyAttributes' reflection system is used to pull 
    ///the display id so that it's selectable in the inspector. the only downside is a lack of enforcement :(((
    /// </remarks>
    [System.Serializable]
    public abstract class RadioTrack : IRadioTrack
    {
        // main comments are in IRadioTrack.cs

        /// <summary>
        /// The sample rate of this track.
        /// </summary>
        /// <see cref="IRadioTrack.SampleRate"/>
        public float SampleRate { get; set; }

        /// <summary>
        /// The number of samples in this track.
        /// </summary>
        /// <see cref="IRadioTrack.SampleCount"/>
        public virtual int SampleCount { get; set; }

        /// <summary>
        /// Initializes this track.
        /// </summary>
        /// <see cref="IRadioTrack.Init"/>
        public abstract void Init();

        /// <summary>
        /// Gets a sample from the track
        /// </summary>
        /// <param name="_sampleIndex">The index of the sample to retrieve</param>
        /// <returns>The height of the sample</returns>
        /// <see cref="IRadioTrack.GetSample(int)"/>
        public abstract float GetSample(int _sampleIndex);

        /// <summary>
        /// Activates an event to run whenever this track ends. This is mainly used for stations to switch track when the previous one ends
        /// </summary>
        /// <param name="_callback">A function to run when this track ends</param>
        public virtual void AddToPlayerEndCallback(ref Action<RadioTrackPlayer> _callback) { }
    }

}