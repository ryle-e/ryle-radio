using NaughtyAttributes;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;


// a shrunken RadioTrackWrapper for use in a Station
// we could possibly use an interface or something for both this and RadioTrackWrapper but i don't know if we'd ever need to use it again
// possibly something for future implementation though, if useful
[System.Serializable]
public class StationRadioTrackWrapper
{
    // the id of this track
    [SerializeField] private string id;

    // the volume of the track
    [Range(0, 500)] public float gain = 100;

    // the amount of time at the start and finish of the track for which there is silence
    public Vector2 startAndEndRests = Vector2.zero;

    // the type of this current track as chosen in the inspector
    [SerializeField, AllowNesting, OnValueChanged("CreateTrackLocal"), Dropdown("TrackNames")]
    private string trackType = "None";

    // the track itself
    // note that this is an IStationTrack, not an IRadioTrack like in the normal RadioTrackWrapper
    // this is because some custom track types might not be supported to be played in a Station- e.g (and this one could change) a Station can't be nested in a Station
    [SerializeReference]
    protected IStationTrack track;

    public string ID => id;

    public float SampleRate => track.SampleRate;
    public int SampleCount
    {
        get
        {
            // adds the rests to the number of samples in this track
            return track.SampleCount + (int)((startAndEndRests.x + startAndEndRests.y) * track.SampleRate);
        }
    }

    public float Gain => gain / 100f; // get the volume based on the gain variable

    private static Type[] trackTypes;
    private static Type[] TrackTypes
    {
        get
        {
            // just like RadioTrackWrapper, get all available track types dynamically
            trackTypes ??= RadioUtils.FindDerivedTypes(typeof(IStationTrack));

            return trackTypes;
        }
    }

    private static string[] trackNames;
    private static string[] TrackNames { 
        get 
        {
            // convert available track types to their class names, as usual
            trackNames ??= TrackTypes
                .Select(t => (string)t.GetField("DISPLAY_NAME").GetValue(null))
                .ToArray();

            return trackNames;
        } 
    }


    public StationRadioTrackWrapper(IStationTrack _track)
    {
        track = _track;
        gain = 100;

        // ensure this wrapper is not empty
        CreateTrackLocal();
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    public static void OnReload()
    {
        trackTypes = null;
        trackNames = null;
    }
#endif

    // init the stored track
    public void Init()
    {
        track.Init();
    }

    // gets a new track of type depending on the provided id
    // note that this is static
    // also note that this uses IStationTrack, unlike RadioTrackWrapper
    public static IStationTrack CreateTrackEditor(string _name)
    {
        // get the index of the chosen track type
        int index = Array.IndexOf(TrackNames, _name);

        // if you somehow have an invalid track type, don't create anything
        if (index < 0)
            return null;

        // create the track generically
        // more info in RadioTrackWrapper.CreateTrackEditor
        IStationTrack outTrack = (IStationTrack)Activator.CreateInstance(TrackTypes[index]);
        outTrack.IsInStation = true;

        // return the track
        return outTrack;
    }

    // creates a new track in this wrapper, called when the track type is chosen
    public void CreateTrackLocal()
    {
        track = CreateTrackEditor(trackType);
    }

    // get a sample from this track
    public float GetSample(int _sampleIndex)
    {
        // if the track has a rest at the beginning at that sample index, return a silent sample
        if (_sampleIndex < startAndEndRests.x)
            return 0;
        // same thing but for a rest at the end of the track
        else if (_sampleIndex > (track.SampleCount + startAndEndRests.x))
            return 0;

        // otherwise, return the sample from the track
        return track.GetSample(_sampleIndex);
    }
}