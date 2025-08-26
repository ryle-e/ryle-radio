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

    public override RadioTrackPlayer.PlayerType PlayerType => RadioTrackPlayer.PlayerType.Loop;

    
    public override void Init()
    {
        Samples = new float[clip.samples * clip.channels];
        SampleCount = Samples.Length;

        Channels = clip.channels;

        if (!clip.GetData(Samples, 0))
            Debug.LogError("Cannot access clip data from track " + clip.name);
    }
    
    public override float GetSample(int _sampleIndex)
    {
        return Samples[_sampleIndex];
    }
}