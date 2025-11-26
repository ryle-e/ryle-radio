using NaughtyAttributes;
using UnityEngine;

namespace RyleRadio.Tracks
{

    /// <summary>
    /// A RadioTrack that plays procedurally generated audio, such as noice, silence, and waveforms.
    /// </summary>
    [System.Serializable]
    public class ProceduralRadioTrack : RadioTrack, IStationTrack
    {
        /// <summary>
        /// The name of this class in the editor- required by RadioTrack
        /// </summary>
        public const string DISPLAY_NAME = "Procedural";

        /// <summary>
        /// The eventType of procedural audio this track is generating.
        /// </summary>
        public enum ProceduralType // you can add custom ones of these if you like, but they're not as malleable as tracks themselves and you'll have to adjust the code
        {
            WhiteNoise, ///< White noise: random samples between 0 and 1
            PinkNoise, ///< Special eventType of noise defined by Paul Kellet's refined method (pk3): sounds "fuller" than white noise
            BrownNoise, ///< Special eventType of noise using a value (\ref brownWalkPower): sounds softer and deeper
            SineWave, ///< A waveform: shaped as a sine wave at a given frequency
            Silence, ///< Silence: samples at 0
        }

        private const float NOISE_MULTIPLIER = .2f; ///< A base multiplier for noise- because the samples can go all the way up to 1, noise tends to be a lot louder than other tracks, e.g: AudioClips in ClipRadioTrack
        private const float PINK_MULTIPLIER = .5f; ///< Pink noise is even louder than the other noise types, so we curb it a little
        private const float BASE_SAMPLE_RATE = 44100; ///< The default sample rate for the procedural tracks, can adjust this if required

        /// <summary>
        /// The selected eventType of noise for this track
        /// </summary>
        public ProceduralType proceduralType = ProceduralType.WhiteNoise;

        /// <summary>
        /// If this track is inside of a StationRadioTrack, then it should only play for a certain duration- this is that duration
        /// </summary>
        [AllowNesting, ShowIf("IsInStation")]
        public float duration = 0;

        /// <summary>
        /// The frequency/pitch of the waveform
        /// </summary>
        [AllowNesting, ShowIf("proceduralType", ProceduralType.SineWave), Range(1, 2000)]
        public float waveFrequency = 100;

        /// <summary>
        /// The value used to define the sound of brown noise.<br><br>
        /// Brown noise works by adding the generated sample to all previous generated samples. This float is what these generated samples are multiplied by when stored.
        /// This means that the higher the walk power, the larger the difference that each sample makes on average, and the closer it sounds to white noise.
        /// </summary>
        [AllowNesting, ShowIf("proceduralType", ProceduralType.BrownNoise), Range(0, 1)]
        public float brownWalkPower = 0.5f;

#if !SKIP_IN_DOXYGEN
        // we can't use UnityEngine.Random during audio updates as it runs on a different thread, so we need to use System.Random instead
        private System.Random random;
#endif

        /// <summary>
        /// The progress of the waveform used when generating it
        /// </summary>
        private float phase = 0;

#if !SKIP_IN_DOXYGEN
        // values used for the sample generation of pink noise- it needs to use running values and stores them here
        private float p0 = 0, p1 = 0, p2 = 0, p3 = 0, p4 = 0, p5 = 0, p6 = 0;
#endif

        /// <summary>
        /// The generated brown noise from the previous sample
        /// </summary>
        private float lastBrown = 0;

        /// <summary>
        /// Whether this is in a station or not. Required by IStationTrack
        /// </summary>
        public bool IsInStation { get; set; }


        /// <summary>
        /// Initializes this track
        /// </summary>
        public override void Init()
        {
            random = new System.Random();
            phase = 0;

            // if this track has a duration, set the sample count to that duration, otherwise set it to ''''infinite'''' (as big a number as possible)
            SampleCount = (duration > 0) ? (int)(duration * BASE_SAMPLE_RATE) : int.MaxValue;
            SampleRate = BASE_SAMPLE_RATE;
        }

        /// <summary>
        /// Get the next sample of the selected procedural audio eventType
        /// </summary>
        /// <param name="_sampleIndex">The index of the sample- useless for noise, useful for waveforms</param>
        /// <returns>The sample of generated audio</returns>
        public override float GetSample(int _sampleIndex)
        {
            // all the noise algorithms use a white noise float
            float white = 0;

            switch (proceduralType)
            {
                // generate a random value between -1 and 1
                case ProceduralType.WhiteNoise:
                    white = ((float)random.NextDouble() * 2) - 1;
                    return white * NOISE_MULTIPLIER;

                // generate a random value with a pink noise filter applied
                case ProceduralType.PinkNoise:
                    white = ((float)random.NextDouble() * 2) - 1;

                    // generated using paul kellet's refined method (pk3)
                    // https://www.firstpr.com.au/dsp/pink-noise/#Filtering:~:text=(This%20is%20pke,p2%20%2B%20white%20*%200.1848%3B
                    p0 = 0.99886f * p0 + white * 0.0555179f;
                    p1 = 0.99332f * p1 + white * 0.0750759f;
                    p2 = 0.96900f * p2 + white * 0.1538520f;
                    p3 = 0.86650f * p3 + white * 0.3104856f;
                    p4 = 0.55000f * p4 + white * 0.5329522f;
                    p5 = -0.7616f * p5 - white * 0.0168980f;
                    float pink = p0 + p1 + p2 + p3 + p4 + p5 + p6 + white * 0.5362f;
                    p6 = white * 0.115926f;

                    // pink is louder than normal noise so we use a secondary multiplier on it
                    return pink * NOISE_MULTIPLIER * PINK_MULTIPLIER;

                // generate a random value, using previous values to soften the noise
                // theory explained by gemini, adjusted from https://forum.juce.com/t/creating-colored-noise/30012/4
                case ProceduralType.BrownNoise:
                    white = ((float)random.NextDouble() * 2) - 1;

                    lastBrown += white * brownWalkPower;
                    lastBrown = Mathf.Clamp(lastBrown, -1, 1) * 0.998f; // use a small <1 constant to ensure the noise doesn't get constantly louder

                    return lastBrown * NOISE_MULTIPLIER;

                // use a sine wave to create a basic single tone
                // from https://discussions.unity.com/t/generating-a-simple-sinewave/665023/16
                case ProceduralType.SineWave:
                    float lastPhase = phase + (2 * Mathf.PI * (waveFrequency / SampleRate));

                    phase = lastPhase;
                    if (phase > 2 * Mathf.PI) // ensure that phase doesn't get exponentially larger
                        phase -= 2 * Mathf.PI;

                    return Mathf.Sin(lastPhase) * NOISE_MULTIPLIER;

                // just silence in case you need it
                case ProceduralType.Silence:
                    return 0;

                default:
                    Debug.LogError("Attempting to get a sample from a procedural RadioTrack with an invalid ProceduralType- this should not be possible.");
                    return 0;
            }
        }


    }

}