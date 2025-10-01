using UnityEngine;

namespace RyleRadio.Tracks
{

    // a track that plays from a chosen AudioClip
    [System.Serializable]
    public class ClipRadioTrack : RadioTrack, IStationTrack
    {
        public const string DISPLAY_NAME = "Audio Clip";

        // the clip you're providing to this track
        public AudioClip clip;

        // we read the clip into an array of individual samples so that we can play it sample-by-sample
        protected float[] Samples { get; set; }

        public bool IsInStation { get; set; }


        // needs to be called again if the clip is changed
        public override void Init()
        {
            ReadClipAndForceToMono();

            SampleCount = Samples.Length; // assign clip values based on chosen clip
            SampleRate = clip.frequency;
        }

        // in order to play the radio generically and mix different clips, we need to flatten the clip to mono/one channel
        // realistically the radio is playing from a single speaker and is mono anyway
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

        public override float GetSample(int _sampleIndex)
        {
            // we already have all the samples, so we just get the one at the given index
            return Samples[_sampleIndex];
        }
    }

}