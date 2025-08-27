using NaughtyAttributes;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;


[System.Serializable]
public class StationRadioTrack : RadioTrack
{
    [AllowNesting]
    public bool randomSequence;

    [AllowNesting]
    public List<StationRadioTrackWrapper> stationTrackWs;

    private int currentTrackIndex;

    private System.Random random;

    private StationRadioTrackWrapper CurrentTrackW => stationTrackWs[currentTrackIndex];


    public override void Init()
    {
        random = new System.Random();

        NextTrack();
    }

    public override float GetSample(int _sampleIndex)
    {
        if (_sampleIndex >= SampleCount - 1)
            NextTrack();

        Channels = CurrentTrackW.Channels;
        SampleRate = CurrentTrackW.SampleRate;
        SampleCount = CurrentTrackW.SampleCount;

        return CurrentTrackW.GetSample(_sampleIndex);
    }


    private void NextTrack()
    {
        if (randomSequence)
        {
            currentTrackIndex = random.Next(0, stationTrackWs.Count);
        }
        else
        {
            currentTrackIndex++;

            if (currentTrackIndex >= stationTrackWs.Count)
                currentTrackIndex = 0;
        }
    }
}