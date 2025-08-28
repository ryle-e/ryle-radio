using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class StationRadioTrack : RadioTrack
{
    [AllowNesting]
    public bool randomSequence;

    [AllowNesting, ShowIf("randomSequence"), Range(0, 1)]
    public float thresholdBeforeRepeats;

    [AllowNesting]
    public List<StationRadioTrackWrapper> stationTrackWs;

    private int currentTrackIndex;

    private System.Random random;

    private int[] remainingTracksBeforeRepeat;

    private int completedSamples = 0;

    private StationRadioTrackWrapper CurrentTrackW => stationTrackWs[currentTrackIndex];


    public override void Init()
    {
        random = new System.Random();

        remainingTracksBeforeRepeat = new int[stationTrackWs.Count];
        completedSamples = 0;

        foreach (StationRadioTrackWrapper trackW in stationTrackWs)
            trackW.Init();

        NextTrack();
    }

    public override void AddToPlayerEndCallback(ref Action<RadioTrackPlayer> _callback)
    {
        _callback += p => p.UpdateSampleIncrement();
    }

    public override float GetSample(int _sampleIndex)
    {
        int adjustedIndex = _sampleIndex - completedSamples;

        if (adjustedIndex >= SampleCount - 1)
        {
            Debug.Log(adjustedIndex + " " + SampleCount);
            completedSamples = _sampleIndex;
            NextTrack();


            Debug.Log(completedSamples + " " + adjustedIndex + " " + (_sampleIndex - completedSamples));
        }

        Debug.Log(adjustedIndex + " " + _sampleIndex + " " + completedSamples);

        return CurrentTrackW.GetSample(adjustedIndex);
    }


    private void NextTrack()
    {
        if (randomSequence)
        {
            List<int> selectFrom = new();

            for (int i = 0; i < remainingTracksBeforeRepeat.Length; i++)
            {
                if (remainingTracksBeforeRepeat[i] <= 0)
                    selectFrom.Add(i);
            }

            int index = random.Next(0, selectFrom.Count);

            if (thresholdBeforeRepeats > 0)
            {
                for (int t = 0; t < remainingTracksBeforeRepeat.Length; t++)
                    remainingTracksBeforeRepeat[t]--;

                remainingTracksBeforeRepeat[index] = (int)((stationTrackWs.Count - 1) * thresholdBeforeRepeats);
            }

            currentTrackIndex = index;
        }
        else
        {
            currentTrackIndex++;

            if (currentTrackIndex >= stationTrackWs.Count)
                currentTrackIndex = 0;
        }

        SampleRate = CurrentTrackW.SampleRate;
        SampleCount = CurrentTrackW.SampleCount;
    }
}