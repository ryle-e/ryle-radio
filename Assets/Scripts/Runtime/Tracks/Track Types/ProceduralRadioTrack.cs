using NaughtyAttributes;
using System.Net.NetworkInformation;
using UnityEditor;
using UnityEngine;


[System.Serializable]
public class ProceduralRadioTrack : RadioTrack<ProceduralRadioTrack>, IStationTrack
{
    public const string DISPLAY_NAME = "Procedural";

    public enum ProceduralType
    {
        WhiteNoise,
        PinkNoise,
        BrownNoise,
        SineWave,
        Silence,
    }

    private const float NOISE_MULTIPLIER = .2f;
    private const float PINK_MULTIPLIER = .5f; // pink noise is slightly louder so we curb it a little
    private const float BASE_SAMPLE_RATE = 44100;

    public ProceduralType proceduralType = ProceduralType.WhiteNoise;

    [AllowNesting, ShowIf("IsFinite")]
    public float duration = 0;

    [AllowNesting, ShowIf("proceduralType", ProceduralType.SineWave), Range(1, 2000)]
    public float waveFrequency = 100;

    [AllowNesting, ShowIf("proceduralType", ProceduralType.BrownNoise), Range(0, 1)]
    public float brownWalkPower = 0.5f;

    private System.Random random;

    private float phase = 0;

    public bool IsFinite { get; set; } = false;

    float p0 = 0, p1 = 0, p2 = 0, p3 = 0, p4 = 0, p5 = 0, p6 = 0;
    float lastBrown = 0;


    public override void Init()
    {
        random = new System.Random();
        phase = 0;

        SampleCount = (duration > 0) ? (int)(duration * BASE_SAMPLE_RATE) : int.MaxValue;
        SampleRate = BASE_SAMPLE_RATE;
    }


    public override float GetSample(int _sampleIndex)
    {
        float white = 0;

        switch (proceduralType)
        {
            case ProceduralType.WhiteNoise:
                return (((float)random.NextDouble() * 2) - 1) * NOISE_MULTIPLIER;

            case ProceduralType.PinkNoise:
                // generated using paul kellet's refined method (pk3), picked for performance as realtime generation
                // https://www.firstpr.com.au/dsp/pink-noise/#Filtering:~:text=(This%20is%20pke,p2%20%2B%20white%20*%200.1848%3B

                white = ((float)random.NextDouble() * 2) - 1;

                p0 = 0.99886f * p0 + white * 0.0555179f;
                p1 = 0.99332f * p1 + white * 0.0750759f;
                p2 = 0.96900f * p2 + white * 0.1538520f;
                p3 = 0.86650f * p3 + white * 0.3104856f;
                p4 = 0.55000f * p4 + white * 0.5329522f;
                p5 = -0.7616f * p5 - white * 0.0168980f;
                float pink = p0 + p1 + p2 + p3 + p4 + p5 + p6 + white * 0.5362f;
                p6 = white * 0.115926f;

                return pink * NOISE_MULTIPLIER * PINK_MULTIPLIER;

            // theory explained by gemini, adjusted from https://forum.juce.com/t/creating-colored-noise/30012/4
            case ProceduralType.BrownNoise:
                white = ((float)random.NextDouble() * 2) - 1;
                lastBrown += white * brownWalkPower;
                lastBrown = Mathf.Clamp(lastBrown, -1, 1) * 0.998f;

                return lastBrown * NOISE_MULTIPLIER;

            // from https://discussions.unity.com/t/generating-a-simple-sinewave/665023/16
            case ProceduralType.SineWave:
                float lastPhase = phase + (2 * Mathf.PI * (waveFrequency / SampleRate));
                
                phase = lastPhase;
                if (phase > 2 * Mathf.PI)
                    phase -= 2 * Mathf.PI;

                return Mathf.Sin(lastPhase) * NOISE_MULTIPLIER;

            case ProceduralType.Silence:
                return 0;

            default:
                Debug.LogError("Attempting to get a sample from a procedural RadioTrack with an invalid ProceduralType- this should not be possible.");
                return 0;
        }
    }

    
}