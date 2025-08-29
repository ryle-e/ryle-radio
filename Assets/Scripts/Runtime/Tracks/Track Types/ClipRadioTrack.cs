using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class ClipRadioTrack : RadioTrack
{
    public AudioClip clip;

    protected float[] Samples { get; set; }

    
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