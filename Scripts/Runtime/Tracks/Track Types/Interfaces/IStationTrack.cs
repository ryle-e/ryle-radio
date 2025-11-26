
namespace RyleRadio.Tracks
{
    /// <summary>
    /// A RadioTrack that can be played as part of a station.
    /// </summary>
    /// <remarks>This may end up being useless in future if all tracks can be used as part of a station- currently only other \ref StationRadioTrack s don't have this (so you can't nest stations) but that could be changed in future.</remarks>
    /// <example><code>
    /// public class MyCustomTrack : RadioTrack, IStationTrack { ... }</code></example>
    public interface IStationTrack : IRadioTrack
    {
        /// <summary>
        /// Whether or not this track is part of a station.
        /// </summary>
        public bool IsInStation { get; set; }
    }

}