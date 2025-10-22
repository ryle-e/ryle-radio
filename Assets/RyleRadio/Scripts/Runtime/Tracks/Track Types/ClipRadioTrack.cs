using UnityEngine;

namespace RyleRadio.Tracks
{

    /// <summary>
    /// A eventType of RadioTrack that plays from a chosen AudioClip object
    /// </summary>
    [System.Serializable]
    public class ClipRadioTrack : RadioTrack, IStationTrack
    {
        /// <summary>
        /// The display name of this track in the editor. Required in \ref RadioTrack
        /// </summary>
        public const string DISPLAY_NAME = "Audio Clip";

        /// <summary>
        /// The clip that this track plays
        /// </summary>
        public AudioClip clip;

        /// <summary>
        /// The individual samples of this clip, as it needs to be played sample-by-sample (a limitation of Unity's AudioClip)
        /// </summary>
        protected float[] Samples { get; set; }

        /// <summary>
        /// Whether or not this is in a \ref StationRadioTrack
        /// Required by \ref IStationTrack
        /// </summary>
        public bool IsInStation { get; set; }


        /// <summary>
        /// Initializes this track. This needs to be called every time the clip is changed
        /// </summary>
        public override void Init()
        {
            ReadClipAndForceToMono();

            SampleCount = Samples.Length; // assign clip values based on chosen clip
            SampleRate = clip.frequency;
        }

        /// <summary>
        /// Reads the clip into the \ref Samples array, and combines its channels into one.
        /// 
        /// We need to flatten the clip into one channel to play it from a \ref RadioOutput as the Output is only using one channel. Theoretically, we could expand the Output to use multiple channels, but given this would be the only track eventType to do this it's probably not worth the significant effort.
        /// <br>For the moment, we'll treat Outputs as AM radios (which are mono in real life)
        /// </summary>
        public void ReadClipAndForceToMono()
        {
            float[] allSamples = new float[clip.samples * clip.channels]; // all samples from both channels
            Samples = new float[clip.samples]; // samples combined to one channel

            // if the clip is invalid for some reason, tell the user
            if (!clip.GetData(allSamples, 0))
            {
                Debug.LogError("Cannot access clip data from track " + clip.name);
                return;
            }

            // for each sample (all channels)
            for (int sample = 0; sample < clip.samples; sample++)
            {
                float combined = 0;

                // for each channel in this sample, combine the audio
                for (int channel = 0; channel < clip.channels; channel++)
                    combined += allSamples[(sample * clip.channels) + channel];

                combined /= clip.channels; // find the average of the channels' audio
                Samples[sample] = combined; // make the mono sample the average
            }
        }

        /// <summary>
        /// Gets a sample from the clip
        /// </summary>
        /// <param name="_sampleIndex">The index of the sample</param>
        /// <returns>A sample from the clip at the given index</returns>
        public override float GetSample(int _sampleIndex)
        {
            // we already have all the samples, so we just get the one at the given index
            return Samples[_sampleIndex];
        }
    }

}