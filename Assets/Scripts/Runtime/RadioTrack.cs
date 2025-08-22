using NaughtyAttributes;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;


[System.Serializable]
public class RadioTrack
{
    public enum TrackType
    {
        AudioClip,
        Procedural
    }

    public enum ProceduralType
    {
        WhiteNoise,
        PinkNoise,
        BrownNoise,
        SineWave,
        Silence,
    }

    private const float RANGE_DECIMAL_MULTIPLIER = 10f;

    private const float WHITE_NOISE_MULTIPLIER = .5f;

    private const float SIN_BASE_SAMPLE_RATE = 1024;

    public static AnimationCurve DefaultGainCurve => new(new Keyframe[3] { 
        new(0, 0, 0, 0), 
        new(0.5f, 1, 0, 0), 
        new(1, 0, 0, 0) 
    });

    public string id;

    [SerializeField] protected TrackType trackType = TrackType.AudioClip;


    [AllowNesting, ShowIf("UseAudioClip")]
    public AudioClip clip;

    [AllowNesting, ShowIf("UseProcedural")]
    public ProceduralType proceduralType = ProceduralType.WhiteNoise;

    [AllowNesting, ShowIf("IsSineWave"), Range(1, 500)]
    public float waveFrequency = 100;


    [MinMaxSlider(RadioData.LOW_INDEX, RadioData.HIGH_INDEX), OnValueChanged("ScaleRange")]
    public Vector2 range;

    [CurveRange(0, 0, 1, 1)]
    public AnimationCurve gainCurve = new(DefaultGainCurve.keys);

    [Range(0, 500)]
    public float gain = 100;

    [Range(0, 1)]
    public float attenuation = 0.1f;

    public bool isGlobal = true;

    public bool loop = true;
    public bool playOnInit = true;

    private System.Random random;

    [HideInInspector] public List<RadioBroadcaster> broadcasters;


    protected float[] Samples { get; private set; }
    public int SampleLength { get; private set; } // set to epsilon for endless noise

    public int Channels { get; private set; }

    private bool UseAudioClip => trackType == TrackType.AudioClip;
    private bool UseProcedural => trackType == TrackType.Procedural;
    private bool IsSineWave => UseProcedural && proceduralType == ProceduralType.SineWave;


    // rounds range to 1 decimal point
    private void ScaleRange()
    {
        range = new(
            ((int)(range.x * RANGE_DECIMAL_MULTIPLIER)) / RANGE_DECIMAL_MULTIPLIER, 
            ((int)(range.y * RANGE_DECIMAL_MULTIPLIER)) / RANGE_DECIMAL_MULTIPLIER
        );
    }

    public void Init()
    {
        broadcasters = new List<RadioBroadcaster>();
        random = new System.Random();

        switch (trackType)
        {
            case TrackType.AudioClip:
                Samples = new float[clip.samples * clip.channels];
                SampleLength = Samples.Length;

                Channels = clip.channels;

                if (!clip.GetData(Samples, 0))
                    Debug.LogError("Cannot access clip data from track " + clip.name);

                break;

            case TrackType.Procedural:
                SampleLength = int.MaxValue;
                Channels = 1;

                break;
        }
    }


    public float GetSample(int _sampleIndex)
    {
        switch (trackType)
        {
            case TrackType.AudioClip:
                return Samples[_sampleIndex];

            case TrackType.Procedural:
                return GetProceduralSample(_sampleIndex);

            default:
                Debug.LogError("Attempting to get a sample from a RadioTrack with an invalid TrackType- this should not be possible.");
                return 0;
        }
    }

    private float GetProceduralSample(int _sampleIndex)
    {
        switch (proceduralType)
        {
            case ProceduralType.WhiteNoise:
                return (((float)random.NextDouble() * 2) - 1) * WHITE_NOISE_MULTIPLIER;

            case ProceduralType.PinkNoise:
                // generated using paul kellet's economy method, picked for performance as realtime generation
                // https://www.firstpr.com.au/dsp/pink-noise/#Filtering:~:text=(This%20is%20pke,b2%20%2B%20white%20*%200.1848%3B
                float b0=0, b1=0, b2=0, o=0, white=0;

                white = ((float)random.NextDouble() * 2) - 1;

                b0 = 0.99765f + white * 0.0990460f;
                b1 = 0.96300f + white * 0.2965164f;
                b2 = 0.57000f + white * 1.0526913f;
                o = b0 + b1 + b2 + white * 0.1848f;

                return o;

            case ProceduralType.SineWave:
                return Mathf.Sin((_sampleIndex / SIN_BASE_SAMPLE_RATE) * waveFrequency);

            case ProceduralType.Silence:
                return 0;

            default:
                Debug.LogError("Attempting to get a sample from a procedural RadioTrack with an invalid ProceduralType- this should not be possible.");
                return 0;
        }
    }

    public float GetGain(float _tune, float _otherGain)
    {
        if (_tune < range.x || _tune > range.y)
            return 0;

        float tunePower = gainCurve.Evaluate( _tune.Remap(range.x, range.y, 0f, 1f) );
        float gainPower = gain / 100f;
        float attenPower = 1f - (Mathf.Clamp01(_otherGain) * attenuation);

        //if (attenuation > 0.5f)
        //    Debug.Log(attenPower);

        return tunePower * gainPower * attenPower;
    }
}

static class FloatUtils
{
    public static float Remap(this float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}