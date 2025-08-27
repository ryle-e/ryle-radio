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
    public AudioClip clip;

    protected float[] Samples { get; set; }

    
    public override void Init()
    {
        Samples = new float[clip.samples * clip.channels];
        SampleCount = Samples.Length;
        SampleRate = clip.frequency;

        Channels = clip.channels;

        if (!clip.GetData(Samples, 0))
            Debug.LogError("Cannot access clip data from track " + clip.name);
    }
    
    public override float GetSample(int _sampleIndex)
    {
        return Samples[_sampleIndex];
    }
}