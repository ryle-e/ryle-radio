using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


// a track that plays from a chosen AudioClip
[System.Serializable]
public class ClipRadioTrack : RadioTrack, IStationTrack
{
    public const string DISPLAY_NAME = "Audio Clip";

    // the clip you're providing to this track
    public AudioClip clip;

    // we read the clip into an array of individual samples so that we can play it sample-by-sample as tracks are required to
    protected float[] Samples { get; set; }

    public bool IsInStation { get; set; }


    public override void Init()
    {
        ReadClipAndForceToMono();

        SampleCount = Samples.Length;
        SampleRate = clip.frequency;
    }

    public void ReadClipAndForceToMono()
    {
        float[] allSamples = new float[clip.samples * clip.channels];
        Samples = new float[clip.samples];

        if (!clip.GetData(allSamples, 0))
        {
            Debug.LogError("Cannot access clip data from track " + clip.name);
            return;
        }

        for (int sample = 0; sample < clip.samples; sample++)
        {
            float combined = 0;

            for (int channel = 0; channel < clip.channels; channel++)
                combined += allSamples[(sample * clip.channels) + channel];

            //Debug.Log(sample + " " + clip.channels + " " + combined);
            combined /= clip.channels;
            Samples[sample] = combined;
        }
    }
    
    public override float GetSample(int _sampleIndex)
    {
        return Samples[_sampleIndex];
    }
}