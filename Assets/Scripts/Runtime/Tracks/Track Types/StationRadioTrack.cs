using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class StationRadioTrack : RadioTrack
{
    public const string DISPLAY_NAME = "Station aka Multi-select";

    public bool randomSequence;

    // how many other tracks need to be played before the same one is chosen again
    // effectively stops the same track from playing back-to-back, and forces more variety in which tracks are played
    // the number of tracks to be played before one can be repeated is  round_down( (track_count - 1) * threshold )
    // i.e with four tracks and a threshold of 0.5f, rounddown((4 - 1 == 3) * 0.5) == 1 other track will need to be played before a repeat
    // i.e with four tracks and a threshold of 0.7f, rounddown((4 - 1 == 3) * 0.7) == 2 other tracks will need to be played before a repeat
    // i.e with four tracks and a threshold of 1f, rounddown((4 - 1 == 3) * 1) == 3, aka all other tracks will need to be played before a repeat
    // i.e with eleven tracks and a threshold of 0.8f, rounddown((11 - 1 == 10) * 0.8f == 8 other tracks will need to be played before a repeat
    // do note that if this is set to 1, the tracks are forced to play in the same randomized sequence repeatedly
    public float thresholdBeforeRepeats;

    public List<StationRadioTrackWrapper> stationTrackWs;

    private int currentTrackIndex;

    private System.Random random;

    private int[] remainingTracksBeforeRepeat;

    private StationRadioTrackWrapper CurrentTrackW => stationTrackWs[currentTrackIndex];


    public override void Init()
    {
        random = new System.Random();

        remainingTracksBeforeRepeat = new int[stationTrackWs.Count];
        currentTrackIndex = 0;

        foreach (StationRadioTrackWrapper trackW in stationTrackWs)
            trackW.Init();

        NextTrack();
    }

    public override void AddToPlayerEndCallback(ref Action<RadioTrackPlayer> _callback)
    {
        _callback += p => NextTrack();
        _callback += p => p.UpdateSampleIncrement();
    }

    public override float GetSample(int _sampleIndex)
    {
        return CurrentTrackW.GetSample(_sampleIndex) * CurrentTrackW.GetGain(); 
    }


    // selects the next track to play from the station
    private void NextTrack()
    {
        // if the next track is randomly chosen
        if (randomSequence)
        {
            List<int> selectFrom = new();

            // find all tracks that can be played now, and store their index from stationTrackWs
            for (int i = 0; i < remainingTracksBeforeRepeat.Length; i++)
            {
                // if it has <=0 tracks remaining before it can be played, then it can be played
                if (remainingTracksBeforeRepeat[i] <= 0)
                    selectFrom.Add(i);
            }

            // select one track from the playable tracks
            int trackIndex = selectFrom[random.Next(0, selectFrom.Count)];

            // if a track needs to wait for a certain amount of other plays before it can be played again,
            if (thresholdBeforeRepeats > 0)
            {
                // decrement the amount of plays each track needs before it can be played again
                for (int t = 0; t < remainingTracksBeforeRepeat.Length; t++)
                    remainingTracksBeforeRepeat[t]--;

                // set the selected track's amount to the maximum number (threshold * track count)
                remainingTracksBeforeRepeat[trackIndex] = (int)((stationTrackWs.Count - 1) * thresholdBeforeRepeats);
            }

            // assign the chosen track
            currentTrackIndex = trackIndex;
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