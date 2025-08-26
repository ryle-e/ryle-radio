using NaughtyAttributes;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;


[System.Serializable]
public abstract class RadioTrack
{
    protected float[] Samples { get; set; }
    public int Channels { get; protected set; }

    public float SampleRate { get; protected set; }
    public float SampleCount { get; protected set; }

    public abstract RadioTrackPlayer.PlayerType PlayerType { get; }


    public abstract void Init();
    public abstract float GetSample(int _sampleIndex);
}