using NaughtyAttributes;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;


[System.Serializable]
public class ProceduralRadioTrack : RadioTrack
{
    public enum ProceduralType
    {
        WhiteNoise,
        PinkNoise,
        BrownNoise,
        SineWave,
        Silence,
    }

    private const float WHITE_NOISE_MULTIPLIER = .5f;

    private const float SIN_BASE_SAMPLE_RATE = 1024;

    public override RadioTrackPlayer.PlayerType PlayerType => RadioTrackPlayer.PlayerType.Loop;

    [AllowNesting, ShowIf("UseProcedural")]
    public ProceduralType proceduralType = ProceduralType.WhiteNoise;

    [AllowNesting, ShowIf("IsSineWave"), Range(1, 500)]
    public float waveFrequency = 100;

    private System.Random random;


    public override void Init()
    {
        random = new System.Random();

        Samples = new float[0]; 
        SampleCount = int.MaxValue;

        Channels = 1;
    }


    public override float GetSample(int _sampleIndex)
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

    
}