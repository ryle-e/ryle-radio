using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class RadioTrackWrapper
{
    public static AnimationCurve DefaultGainCurve => new(new Keyframe[3] {  // the default curve used for gain, a bell curve-like shape
        new(0, 0, 0, 0), 
        new(0.5f, 1, 0, 0), 
        new(1, 0, 0, 0) 
    });

    private const float RANGE_DECIMAL_MULTIPLIER = 10f; // 2 ^ the number of decimal places that the range has, i.e 10 == 1dp, 100 == 2dp

    public string id; // the id used to find and use this track
    [HideInInspector] public string name; // the name of this track for inspector usage

    [MinMaxSlider(RadioData.LOW_TUNE, RadioData.HIGH_TUNE), OnValueChanged("ScaleRange")]
    public Vector2 range; // the range of tune in which this track can be heard

    [CurveRange(0, 0, 1, 1)]
    public AnimationCurve gainCurve = new(DefaultGainCurve.keys); // the volume of the track over its range

    [Range(0, 500)]
    public float gain = 100; // the volume of the track

    [Range(0, 1)]
    public float attenuation = 0.1f; // the amount that the track dims when another track is playing above it, e.g static becoming quieter when clip is audible

    public bool isGlobal = true; // does this track ignore any RadioBroadcasters and play everywhere?
    public bool playOnInit = true; // does this track play on start?

    [HideInInspector] public List<RadioBroadcaster> broadcasters; // the broadcasters in the scene, controlling the gain of the track
    [HideInInspector] public List<RadioInsulationZone> insulators; // the insulation zones in the scene, areas where the gain is weaker- inverse of broadcasters

    [SerializeField, Space(8), AllowNesting, OnValueChanged("CreateTrack"), Dropdown("TrackNames")]
    private string trackType;

    [SerializeReference]
    protected IRadioTrack track; // the track itself

    private static Type[] trackTypes;
    private static Type[] TrackTypes
    {
        get
        {
            trackTypes ??= RadioUtils.FindDerivedTypes(typeof(IRadioTrack));

            return trackTypes;
        }
    }

    private static string[] trackNames;
    private static string[] TrackNames
    {
        get
        {
            trackNames ??= TrackTypes
                .Select(t => (string)t.GetField("DISPLAY_NAME").GetValue(null))
                .ToArray();

            return trackNames;
        }
    }

    //  we provide aliases here so that no other class can directly access RadioTracks- this isn't necessarily vital, but it's much safer
    public float SampleRate => track.SampleRate;
    public int SampleCount => track.SampleCount;


    public static void OnScriptReload()
    {
        Debug.Log("reloading");

        trackTypes = null;
        trackNames = null;
    }

    public RadioTrackWrapper()
    {
        track = null;
        trackType = "";

        CreateTrack();
    }

    public static IRadioTrack CreateTrackEditor(string _name)
    {
        int index = Array.IndexOf(TrackNames, _name);

        if (index < 0)
            return null;

        IRadioTrack outTrack = (IRadioTrack)Activator.CreateInstance(TrackTypes[index]);

        return outTrack;
    }


    public void Init()
    {
        track.Init();

        broadcasters.Clear();
        insulators.Clear();
    }

    public void CreateTrack()
    {
        track = CreateTrackEditor(trackType);
    }

    public void AddToPlayerEndCallback(ref Action<RadioTrackPlayer> _callback)
    {
        track.AddToPlayerEndCallback(ref _callback);
    }

    // rounds range to decimal points
    private void ScaleRange()
    {
        range = new(
            ((int)(range.x * RANGE_DECIMAL_MULTIPLIER)) / RANGE_DECIMAL_MULTIPLIER, // increase the size of the range value, then cut its decimals and shrink it back down
            ((int)(range.y * RANGE_DECIMAL_MULTIPLIER)) / RANGE_DECIMAL_MULTIPLIER
        );
    }


    // calculate the volume of the track at a specific tune value
    public float GetGain(float _tune, float _otherGain)
    {
        if (_tune < range.x || _tune > range.y) // if the tune is out of this track's range, it cannot be heard
            return 0;

        float tunePower = gainCurve.Evaluate(_tune.Remap(range.x, range.y, 0f, 1f)); // get the volume based on the tune and where it sits on the range curve
        float gainPower = gain / 100f; // get the volume based on the gain variable
        float attenPower = 1f - (Mathf.Clamp01(_otherGain) * attenuation); // get the volume based on attenuation and other playing trackWs

        return tunePower * gainPower * attenPower; // combine the values into one singular volume
    }

    public float GetSample(int _sampleIndex)
    {
        return track.GetSample(_sampleIndex);
    }
}