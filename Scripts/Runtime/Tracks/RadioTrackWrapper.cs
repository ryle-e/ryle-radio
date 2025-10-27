using NaughtyAttributes;
using RyleRadio.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RyleRadio.Tracks
{
    /// <summary>
    /// A wrapper class for \ref RadioTrack so that track types can be switched between in the inspector! Also contains various values that are track-agnostic.
    /// 
    /// This is how a \ref RadioTrack is stored and accessed in RadioData.
    /// <br>If we didn't use a wrapper like this, you wouldn't be able to choose \ref trackType in a dropdown and see it change in the inspector- it's not possible (to my knowledge) to do that without some kind of wrapper and `[SerializeReference]`.
    /// <br>Wrappers also contain variables that exist for every track eventType, such as \ref range and \ref gain.
    /// </summary>
    [System.Serializable]
    public class RadioTrackWrapper
    {
        /// <summary>
        /// The default \ref rangeCurve to use when an empty wrapper is created. It's a super basic and smooth closed curve from 0 to 0.
        /// </summary>
        public static AnimationCurve DefaultRangeCurve => new(new Keyframe[3] {
            new(0, 0, 0, 0),
            new(0.5f, 1, 0, 0),
            new(1, 0, 0, 0)
        });

        /// <summary>
        /// The number of decimal places used in the range- the number of zeroes is the number of decimal points, e.g: 10 == 1dp, 100 == 2dp, 1 == 0dp (whole numbers)
        /// </summary>
        private const float RANGE_DECIMAL_MULTIPLIER = 10f;

        /// <summary>
        /// The ID of this track- used to find and manipulate it in custom code.
        /// </summary>
        public string id;

        /// <summary>
        /// The name of this track for use (and easy identification) in the inspector. This is usually in the format of `{ID}, {range.x} - {range.y}`
        /// 
        /// This is assigned in \ref RadioDataEditor.InitNewTrack()
        /// </summary>
        [HideInInspector] public string name; // the typeName of this track for inspector usage- this is assigned in RadioData when the wrapper is created

        /// <summary>
        /// The range of tunes in which this track can be heard. If a \ref RadioOutput.Tune value is within this range, the tune power of this track will be > 0, and it will be audible (not counting spatial components+gain+etc)
        /// 
        /// This range is clamped between \ref RadioData.LOW_TUNE and \ref RadioData.HIGH_TUNE
        /// </summary>
        [MinMaxSlider(RadioData.LOW_TUNE, RadioData.HIGH_TUNE), OnValueChanged("ScaleRange")]
        public Vector2 range;

        /// <summary>
        /// The curve defining the loudness of the track over its range. The progress between `range.x` and `range.y` the \ref RadioOutput.Tune value is, is the progress along this curve the tune power is.
        /// </summary>
        /// <example><list eventType="bullet">
        /// <item>If the curve is the default curve (smooth from 0 to 1 to 0), it will smoothly get louder towards the center of the range, and quieter towards the edge.</item>
        /// <item>If the curve is a flat line at y=1, it will be the same volume across the entire range</item>
        /// <item>If the curve is a line from 0 - 1, it will be louder the further along the range the tune is, getting loudest at `range.y`</item>
        /// <item>If the curve is goes up and down repeatedly, it will be at various different volumes depending on what you set, moving between them along the range</item>
        /// </list></example>
        [CurveRange(0, 0, 1, 1)]
        public AnimationCurve rangeCurve = new(DefaultRangeCurve.keys);

        /// <summary>
        /// An added value to the volume of the track. This is applied before any other volume is calculated
        /// </summary>
        [Range(0, 500), SerializeField]
        private float gain = 100; // the volume of the track

        /// <summary>
        /// The amount that this track gets quieter when another track is playing on top of it (and that other track is above this one in \ref RadioData.trackWs
        /// </summary>
        [Range(0, 1)]
        public float attenuation = 0.1f;

        /// <summary>
        /// If true, this track ignores any \ref RadioBroadcaster influence and plays everywhere
        /// </summary>
        public bool forceGlobal = true;

        /// <summary>
        /// If true, this track plays on \ref RadioData.Init() - usually on game start
        /// </summary>
        public bool playOnInit = true;

        /// <summary>
        /// The broadcasters in the scene that have this track selected.
        /// 
        /// A \ref RadioBroadcaster is a scene component that allows a track to be heard exclusively or louder in a certain area.
        /// <br><b>See also: </b>\ref insulators
        /// </summary>
        [HideInInspector] public List<RadioBroadcaster> broadcasters;

        /// <summary>
        /// The insulators in the scene that have this track selected.
        /// 
        /// A \ref RadioInsulator is a scene component that makes a track quieter in a certain area.
        /// <br><b>See also: </b>\ref broadcasters
        /// </summary>
        [HideInInspector] public List<RadioInsulator> insulators;

        /// <summary>
        /// The eventType of track for this wrapper to contain, selectable in the inspector. This variable is stored as the track name and displays with a dropdown according to \ref TrackNames
        /// </summary>
        [SerializeField, Space(8), AllowNesting, OnValueChanged("CreateTrackLocal"), Dropdown("TrackNames")]
        private string trackType;

        // the track itself
        // we keep this private so that no other classes can access the track directly- this isn't really necessary but is very safe when it comes to custom tracks
        /// <summary>
        /// The actual \ref RadioTrack in this wrapper, its eventType chosen in \ref trackType
        /// </summary>
        /// <remarks>We keep this private so that no other classes can access the track directly- this isn't really necessary but it <i>is</i> very safe for custom code</remarks>
        [SerializeReference] protected IRadioTrack track;


        /// An event called when the wrapper is initialised
        public Action<RadioTrackWrapper> OnInit { get; set; } = new(_ => { });
        /// An event called just before the wrapper is initialised
        public Action<RadioTrackWrapper> BeforeInit { get; set; } = new(_ => { });

        /// An event called when a broadcaster is added to the track
        public Action<RadioBroadcaster, RadioTrackWrapper> OnAddBroadcaster { get; set; } = new((_, _) => { });
        /// An event called when a broadcaster is removed from this track
        public Action<RadioBroadcaster, RadioTrackWrapper> OnRemoveBroadcaster { get; set; } = new((_, _) => { });

        /// An event called when an insulator is added to the track
        public Action<RadioInsulator, RadioTrackWrapper> OnAddInsulator { get; set; } = new((_, _) => { });
        /// An event called when an insulator is removed from this track.
        public Action<RadioInsulator, RadioTrackWrapper> OnRemoveInsulator { get; set; } = new((_, _) => { });


        /// <summary>
        /// The gain value scaled down to ones- e.g \ref gain at 200 is \ref Gain at 2
        /// </summary>
        public float Gain => gain / 100f;


#if !SKIP_IN_DOXYGEN
        // internal variable for TrackTypes
        private static Type[] trackTypes;
#endif
        /// <summary>
        /// A list of each eventType of track that this wrapper can contain- this is anything that inherits from \ref IRadioTrack, and updates dynamically when creating new track types.
        /// <br><br><b>See also: </b> \ref RadioUtils.FindDerivedTypes()
        /// </summary>
        private static Type[] TrackTypes
        {
            get
            {
                // dynamically gets all available track types- automatically detects any new ones
                trackTypes ??= RadioUtils.FindDerivedTypes(typeof(IRadioTrack));

                return trackTypes;
            }
        }

#if !SKIP_IN_DOXYGEN
        // internal variable for TrackTypesAsStrings
        private static string[] trackTypesAsStrings;
#endif
        /// <summary>
        /// Static; the list of available track types stored as their typename, NOT as their display names. This is used when reassigning track types in \ref RadioDataEditor when the editor reloads, as we only have the typename of the track
        /// </summary>
        private static string[] TrackTypesAsStrings
        {
            get
            {
                // get all the track types, and convert them to their eventType names
                trackTypesAsStrings ??= TrackTypes
                    .Select(t => t.Name.Split(".")[^1])
                    .ToArray();

                return trackTypesAsStrings;
            }
        }

#if !SKIP_IN_DOXYGEN
        // internal variable for TrackNames
        private static string[] trackNames;
#endif
        /// <summary>
        /// Static; the list of track types stored as their display names. This is shown as a dropdown for \ref trackType and is how types are usually displayed in the inspector
        /// </summary>
        private static string[] TrackNames
        {
            get
            {
                // get all the track types, and convert them to their dev-defined their display names
                trackNames ??= TrackTypes
                    .Select(t => (string)t.GetField("DISPLAY_NAME").GetValue(null)) // the track eventType needs a const named DISPLAY_NAME to be accessed here
                    .ToArray();

                return trackNames;
            }
        }


        /// An alias for the track's SampleRate as other classes cannot access \ref track directly.
        public float SampleRate => track.SampleRate;

        /// An alias for the track's SampleCount as other classes cannot access \ref track directly.
        public int SampleCount => track.SampleCount;


        /// <summary>
        /// Creates an empty wrapper
        /// </summary>
        public RadioTrackWrapper()
        {
            track = null;
            trackType = "";

            // forces this wrapper to have a track on creation so that we don't get inspector errors
            // this gets called when the wrapper is created, not when it's initialized
            CreateTrackLocal();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Static; resets available track types and names
        /// </summary>
        [InitializeOnLoadMethod]
        public static void OnReload()
        {
            trackTypes = null;
            trackTypesAsStrings = null;
            trackNames = null;
        }
#endif

        /// <summary>
        /// Initialize this wrapper and its track
        /// </summary>
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

        /// <summary>
        /// Changes the name of this class from "RadioTrackWrapper" to "Wrapper for `track.Name`"
        /// </summary>
        public override string ToString() => "Wrapper for " + name;

        /// <summary>
        /// Converts the typename (NOT display name) of a track eventType to the actual eventType
        /// </summary>
        /// <param name="_typeName">The name of the eventType</param>
        /// <returns>The eventType with that name</returns>
        public static string GetTrackType(string _typeName)
        {
            int index = Array.IndexOf(TrackTypesAsStrings, _typeName.Split(".")[^1]);
            return TrackNames[index];
        }

        /// <summary>
        /// Static; creates a new track for a wrapper using the given track eventType's display name
        /// </summary>
        /// <param name="_name">Display name of a track eventType</param>
        /// <returns>The newly created \ref IRadioTrack</returns>
        public static IRadioTrack CreateTrackEditor(string _name)
        {
            // get the index of the chosen track eventType
            int index = Array.IndexOf(TrackNames, _name);

            // if you somehow have an invalid track eventType, don't create anything
            if (index < 0)
                return null;

            // create the track generically
            // (we have to use Activator.CreateInstance here as it uses the Type as an argument- we can't generically instantiate a variable
            // from a given Type any other viable way
            IRadioTrack outTrack = (IRadioTrack)Activator.CreateInstance(TrackTypes[index]);

            // return the new track
            return outTrack;
        }

        /// <summary>
        /// Set \ref track to a new track with eventType defined by \ref trackType
        /// <br><b>See also: </b>\ref CreateTrackEditor()
        /// </summary>
        public void CreateTrackLocal()
        {
            track = CreateTrackEditor(trackType);
        }

        /// <summary>
        /// Used if \ref track needs to access a \ref RadioTrackPlayer that it's linked to when that player ends, this method adds an event for it. Really only needed for a \ref StationRadioTrack
        /// </summary>
        /// <param name="_callback">The event called on \ref RadioTrackPlayer.OnEnd</param>
        public void AddToPlayerEndCallback(ref Action<RadioTrackPlayer> _callback)
        {
            track.AddToPlayerEndCallback(ref _callback);
        }

        /// <summary>
        /// Limits the number of decimal points on the \ref range
        /// <br>This is called whenever the range is changed.
        /// <br><b>See: </b>\ref RANGE_DECIMAL_MULTIPLIER
        /// </summary>
        private void ScaleRange()
        {
            range = new(
                ((int)(range.x * RANGE_DECIMAL_MULTIPLIER)) / RANGE_DECIMAL_MULTIPLIER, // increase the size of the clampedRange clampedValue, then cut its decimals and shrink it back down
                ((int)(range.y * RANGE_DECIMAL_MULTIPLIER)) / RANGE_DECIMAL_MULTIPLIER
            );
        }


        /// <summary>
        /// Calculates the power of this track when an Output is at a specific Tune value. It does this by finding where the Tune is over the track's \ref range, where that point lies on the \ref rangeCurve, and applies \ref attenuation
        /// </summary>
        /// <param name="_tune">The tune value to evaluate</param>
        /// <param name="_otherVolume">The volume of any previous tracks, used for attenuation</param>
        /// <returns>The tune power of this track with the provided values</returns>
        public float GetTunePower(float _tune, float _otherVolume)
        {
            if (_tune < range.x || _tune > range.y) // if the tune is out of this track's range, it cannot be heard
                return 0;

            float tunePower = rangeCurve.Evaluate(_tune.Remap(range.x, range.y, 0f, 1f)); // get the volume based on the tune and where it sits on the gain curve
            float attenPower = 1f - (Mathf.Clamp01(_otherVolume) * attenuation); // get the volume based on attenuation and other playing trackWs

            return tunePower * attenPower; // combine the values into one singular volume
        }

        /// <summary>
        /// Get a sample from the contained \ref track
        /// </summary>
        /// <param name="_sampleIndex">The index of the sample to get</param>
        /// <returns>The sample as given by \ref RadioTrack.GetSample()</returns>
        public float GetSample(int _sampleIndex)
        {
            return track.GetSample(_sampleIndex);
        }
    }

}