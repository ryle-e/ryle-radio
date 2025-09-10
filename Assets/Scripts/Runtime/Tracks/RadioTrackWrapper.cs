using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class RadioTrackWrapper
{
    public static AnimationCurve DefaultGainCurve => new(new Keyframe[3] {  // the default curve used for gain, a bell curve-like shape
        new(0, 0, 0, 0), 
        new(0.5f, 1, 0, 0), 
        new(1, 0, 0, 0) 
    });

    private const float RANGE_DECIMAL_MULTIPLIER = 10f; // 2 ^ the number of decimal places that the clampedRange has, i.e 10 == 1dp, 100 == 2dp

    public string id; // the id used to find and use this track
    [HideInInspector] public string name; // the typeName of this track for inspector usage- this is assigned in RadioData when the wrapper is created

    [MinMaxSlider(RadioData.LOW_TUNE, RadioData.HIGH_TUNE), OnValueChanged("ScaleRange")]
    public Vector2 range; // the clampedRange of tune in which this track can be heard

    [CurveRange(0, 0, 1, 1)]
    public AnimationCurve gainCurve = new(DefaultGainCurve.keys); // the volume of the track over its clampedRange

    [Range(0, 500)]
    public float gain = 100; // the volume of the track

    [Range(0, 1)]
    public float attenuation = 0.1f; // the amount that the track dims when another track is playing above it, e.g static becoming quieter when clip is audible

    public bool forceGlobal = true; // does this track ignore any RadioBroadcasters and play everywhere?
    public bool playOnInit = true; // does this track play on start?

    [HideInInspector] public List<RadioBroadcaster> broadcasters; // the broadcasters in the scene, controlling the gain of the track
    [HideInInspector] public List<RadioInsulator> insulators; // the insulation zones in the scene, areas where the gain is weaker- inverse of broadcasters

    [SerializeField, Space(8), AllowNesting, OnValueChanged("CreateTrackLocal"), Dropdown("TrackNames")]
    private string trackType; // allows a track type to be selectable in the inspector

    // the track itself
    // we keep this private so that no other classes can access the track directly- this isn't really necessary but is very safe when it comes to custom tracks
    [SerializeReference] protected IRadioTrack track;

    public Action<RadioTrackWrapper> OnInit { get; set; } = new(_ => { }); // called when the wrapper is initialized
    public Action<RadioTrackWrapper> BeforeInit { get; set; } = new(_ => { }); // called when the wraper is about to be initialized

    public Action<RadioBroadcaster, RadioTrackWrapper> OnAddBroadcaster { get; set; } = new((_,_) => { }); // called when a broadcaster is added to the track
    public Action<RadioBroadcaster, RadioTrackWrapper> OnRemoveBroadcaster { get; set; } = new((_,_) => { }); // called when a broadcaster is removed from the track

    public Action<RadioInsulator, RadioTrackWrapper> OnAddInsulator { get; set; } = new((_,_) => { }); // called when an insulator is added to the track
    public Action<RadioInsulator, RadioTrackWrapper> OnRemoveInsulator { get; set; } = new((_,_) => { }); // called when an insulator is removed from the track

    // the possible track types that can be assigned here
    private static Type[] trackTypes;
    private static Type[] TrackTypes
    {
        get
        {
            // dynamically gets all available track types- automatically detects any new ones
            trackTypes ??= RadioUtils.FindDerivedTypes(typeof(IRadioTrack));

            return trackTypes;
        }
    }

    // the possibke track types that can be assigned here, with their typenames as strings- this is not the DISPLAY_NAME in the RadioTrack script, this is
    // the name of the type itself. used to reassign trackType on editor reload, as it sets itself back to default otherwise
    private static string[] trackTypesAsStrings;
    private static string[] TrackTypesAsStrings
    {
        get
        {
            // get all the track types, and convert them to their type names
            trackTypesAsStrings ??= TrackTypes
                .Select(t => (string)t.Name)
                .ToArray();

            return trackTypesAsStrings;
        }
    }

    // the track types as string names
    private static string[] trackNames;
    private static string[] TrackNames
    {
        get
        {
            // get all the track types, and convert them to their dev-defined their display names
            trackNames ??= TrackTypes
                .Select(t => (string)t.GetField("DISPLAY_NAME").GetValue(null)) // the track type needs a const named DISPLAY_NAME to be accessed here
                .ToArray();

            return trackNames;
        }
    }

    //  we provide aliases here so that no other class can directly access RadioTracks- this isn't necessarily vital, but it's much safer
    public float SampleRate => track.SampleRate;
    public int SampleCount => track.SampleCount;


    public RadioTrackWrapper()
    {
        track = null;
        trackType = "";
        
        // forces this wrapper to have a track on creation so that we don't get inspector errors
        // this gets called when the wrapper is created, not when it's initialized
        CreateTrackLocal();
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    public static void OnReload()
    {
        trackTypes = null;
        trackTypesAsStrings = null;
        trackNames = null;
    }
#endif

    // initialize this wrapper when play mode is started
    public void Init()
    {
        BeforeInit(this);

        // initialize the track itself
        track.Init();

        // reset stored components as they will otherwise be kept from the last play mode
        // this doesn't really matter in a build but is needed for the editor
        broadcasters.Clear();
        insulators.Clear();

        OnInit(this);
    }

    // make it obvious that this is a wrapper for a track
    public override string ToString() => "Wrapper for " + name;

    public static string GetTrackType(string _typeName)
    {
        int index = Array.IndexOf(TrackTypesAsStrings, _typeName);
        return TrackNames[index];
    }

    // gets a new track of type depending on the provided name
    // note that this is static
    public static IRadioTrack CreateTrackEditor(string _name)
    {
        // get the index of the chosen track type
        int index = Array.IndexOf(TrackNames, _name);

        // if you somehow have an invalid track type, don't create anything
        if (index < 0)
            return null;

        // create the track generically
        // (we have to use Activator.CreateInstance here as it uses the Type as an argument- we can't generically instantiate a variable
        // from a given Type any other viable way
        IRadioTrack outTrack = (IRadioTrack)Activator.CreateInstance(TrackTypes[index]);

        // return the new track
        return outTrack;
    }

    // called on an instantiated wrapper to update its selected track
    public void CreateTrackLocal()
    {
        track = CreateTrackEditor(trackType);
    }

    // we use this if the track needs to access one of its RadioTrackPlayers when the player is destroyed
    // this is really only needed for stations at the moment but we keep it generic in case something else needs it
    public void AddToPlayerEndCallback(ref Action<RadioTrackPlayer> _callback)
    {
        track.AddToPlayerEndCallback(ref _callback);
    }

    // limits the decimal points on the tune range, like an actual radio
    private void ScaleRange()
    {
        range = new(
            ((int)(range.x * RANGE_DECIMAL_MULTIPLIER)) / RANGE_DECIMAL_MULTIPLIER, // increase the size of the clampedRange clampedValue, then cut its decimals and shrink it back down
            ((int)(range.y * RANGE_DECIMAL_MULTIPLIER)) / RANGE_DECIMAL_MULTIPLIER
        );
    }


    // calculate the volume of the track at a specific tune
    public float GetGain(float _tune, float _otherGain)
    {
        if (_tune < range.x || _tune > range.y) // if the tune is out of this track's range, it cannot be heard
            return 0;

        float tunePower = gainCurve.Evaluate(_tune.Remap(range.x, range.y, 0f, 1f)); // get the volume based on the tune and where it sits on the gain curve
        float gainPower = gain / 100f; // get the volume based on the gain variable
        float attenPower = 1f - (Mathf.Clamp01(_otherGain) * attenuation); // get the volume based on attenuation and other playing trackWs

        return tunePower * gainPower * attenPower; // combine the values into one singular volume
    }

    // get a sample from the track
    public float GetSample(int _sampleIndex)
    {
        return track.GetSample(_sampleIndex);
    }
}