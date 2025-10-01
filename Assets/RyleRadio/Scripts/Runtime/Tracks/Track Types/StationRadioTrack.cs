using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyleRadio.Tracks
{

    // a track that serves a multi-select- that is, a track that contains other tracks
    // this is meant to emulate a radio station by storing many individual clips or audio then switching between them
    // it can be used for really any purpose that requires switching tracks (at the end of the previous) though
    // e.g ''''procedural'''' music by shuffling procedural sine waves

    // this has a custom editor in StationRadioTrackEditor.cs
    [System.Serializable]
    public class StationRadioTrack : RadioTrack
    {
        public const string DISPLAY_NAME = "Station aka Multi-select";

        // whether or not this station plays in a random or semi-random order
        public bool randomSequence = true;

        // how many other tracks need to be played before the same one is chosen again in a random sequence
        // effectively stops the same track from playing back-to-back, and forces more variety in which tracks are played
        // the number of tracks to be played before one can be repeated is  round_down( (track_count - 1) * threshold )
        // i.e with four tracks and a threshold of 0.5f, rounddown((4 - 1 == 3) * 0.5) == 1 other track will need to be played before a repeat
        // i.e with four tracks and a threshold of 0.7f, rounddown((4 - 1 == 3) * 0.7) == 2 other tracks will need to be played before a repeat
        // i.e with four tracks and a threshold of 1f, rounddown((4 - 1 == 3) * 1) == 3, aka all other tracks will need to be played before a repeat
        // i.e with eleven tracks and a threshold of 0.8f, rounddown((11 - 1 == 10) * 0.8f == 8 other tracks will need to be played before a repeat
        // do note that if this is set to 1, the tracks are forced to play in the same randomized sequence repeatedly
        public float thresholdBeforeRepeats;

        // the child tracks of this station
        public List<StationRadioTrackWrapper> stationTrackWs;

        // the currently playing child track
        private int currentTrackIndex;

        // audio is on a separate threat to UnityEngine.Random so we need to use System.Random instead
        private System.Random random;

        // this is stored as the number of plays that need to happen before this track can be played again.
        // i.e if tracks A, B and C are being randomly chosen with a thresholdBeforeRepeats of 0.5f, they each need to have 1 other track play before it can repeat (see comments above thresholdBeforeRepeats)
        // so if track A was just played after B and C, this array would look like [1, 0, -1]
        // that is, track A needs another track to be played once before it can be repeated, B doesn't need any other tracks to play and thus can be repeated, and same for C
        // (a number below 0 is treated as 0 for this system)
        // see NextTrack() comments for more info
        private int[] remainingTracksBeforeRepeat;

        // if this track has no stations, we want to print an error- but only once, otherwise it freezes the editor
        private bool hasPrintedError;

        // the currently playing track
        private StationRadioTrackWrapper CurrentTrackW => stationTrackWs[currentTrackIndex];


        public override void Init()
        {
            random = new System.Random();

            // creates an entry for every track
            remainingTracksBeforeRepeat = new int[stationTrackWs.Count];
            currentTrackIndex = 0;

            hasPrintedError = false;

            // initializes all child tracks
            foreach (StationRadioTrackWrapper trackW in stationTrackWs)
                trackW.Init();

            // selects the first track to play
            NextTrack();
        }

        // when a RadioTrackPlayer for this track ends, we update it to match whatever the next track played on this station is
        // this works because the Player only gets destroyed if its a one-shot- in which case only one randomly chosen track from here will be selected anyway
        public override void AddToPlayerEndCallback(ref Action<RadioTrackPlayer> _callback)
        {
            _callback += p => NextTrack(); // choose the next track when the Player completes the last one
            _callback += p => p.UpdateSampleIncrement(); // update the Player's SampleCount/Length when the next track is chosen
        }

        public override float GetSample(int _sampleIndex)
        {
            // get the sample from the currently playing track
            // station tracks also have their own gain variable so apply that
            return CurrentTrackW.GetSample(_sampleIndex) * CurrentTrackW.Gain;
        }


        // selects the next track to play from the station
        private void NextTrack()
        {
            if (stationTrackWs.Count == 0)
            {
                if (!hasPrintedError)
                    Debug.LogWarning($"Cannot play a StationRadioTrack, as there are no stations!");

                hasPrintedError = true;
                return;
            }

            // if the next track is randomly chosen
            if (randomSequence)
            {
                // make a list for all of the tracks that can be chosen this time
                List<int> selectFrom = new();

                // find all tracks that can be played right now, and store their index from stationTrackWs
                for (int i = 0; i < remainingTracksBeforeRepeat.Length; i++)
                {
                    // if a track has <=0 tracks remaining before it can be played, then it can be played
                    if (remainingTracksBeforeRepeat[i] <= 0)
                        selectFrom.Add(i);
                }

                // select one track from the playable tracks
                int trackIndex = selectFrom[random.Next(0, selectFrom.Count)];

                // if each track needs to wait for a certain amount of other plays before it can be played again,
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
            // otherwise if the station uses a set sequence
            else
            {
                // increment the index
                currentTrackIndex++;

                // if the end of the station is reached, go back to the first track
                if (currentTrackIndex >= stationTrackWs.Count)
                    currentTrackIndex = 0;
            }

            // assign the sample data based on the newly chosen track
            SampleRate = CurrentTrackW.SampleRate;
            SampleCount = CurrentTrackW.SampleCount;
        }
    }

}