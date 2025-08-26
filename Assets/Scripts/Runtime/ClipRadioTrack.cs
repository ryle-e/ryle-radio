using NaughtyAttributes;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;


[System.Serializable]
public class ClipRadioTrack : RadioTrack
{
    [AllowNesting, ShowIf("UseAudioClip")]
    public AudioClip clip;


    protected float[] Samples { get; private set; }
    public int SampleLength { get; private set; } // set to epsilon for endless noise

    public int Channels { get; private set; }

    
    /*
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

            case TrackType.Station:
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
    */

    /*
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
    */

    /*
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
    */
    
}