// a track that can be played as part of a station- i.e, anything but stations themselves as you can't have nested stations
// i will duly note that the nested station thing could be changed eventually if the need ever arises, so this may end up being redundant eventually
public interface IStationTrack : IRadioTrack
{
    // if this track is part of a station or not
    public bool IsInStation { get; set; }
}