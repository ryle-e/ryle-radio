using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyleRadio.Tracks
{

    /// <summary>
    /// A eventType of RadioTrack that contains other tracks. Has a custom editor in \ref StationRadioTrackEditor<br>
    /// This is meant to emulate an actual radio station by storing multiple different tracks and switching between them as it plays.
    /// It can be used for really any purpose that calls for switching tracks, though- e.g: ''''procedural'''' music, complex ambience, easter eggs (kinda)
    /// 
    /// Also uses \ref StationRadioTrackWrapper
    /// </summary>
    [System.Serializable]
    public class StationRadioTrack : RadioTrack
    {
        /// <summary>
        /// The display name of this track in the editor. Required by \ref RadioTrack
        /// </summary>
        public const string DISPLAY_NAME = "Station aka Multi-select";

        /// <summary>
        /// Whether or not this station plays in a random or semi-random order
        /// </summary>
        public bool randomSequence = true;

        /// <summary>
        /// When \ref randomSequence is true, this is the number of other tracks that need to be played before the same one is chosen again. Stops the same track from playing back-to-back, and forces variety in the track order.
        /// 
        /// The number of tracks to be played before one can be played again is `round_down( (track_count - 1) * threshold )`.
        /// <br>i.e with four tracks and a threshold of 0.5f, `round_down((4 - 1 == 3) * 0.5) == 1`: one other track will need to be played before a repeat
        /// <br>i.e with four tracks and a threshold of 0.7f, `round_down((4 - 1 == 3) * 0.7) == 2`: two other tracks will need to be played before a repeat
        /// <br>i.e with four tracks and a threshold of 1f, `round_down((4 - 1 == 3) * 1) == 3`: three other tracks (all other tracks) will need to be played before a repeat
        /// <br>i.e with eleven tracks and a threshold of 0.8f, `round_down((11 - 1 == 10) * 0.8f == 8`: eight other tracks will need to be played before a repeat
        /// <i>Do note that if this is set to 1, the tracks are forced to play in the same randomized sequence repeatedly</i>
        /// </summary>
        public float thresholdBeforeRepeats;

        /// <summary>
        /// The tracks contained within this station
        /// </summary>
        public List<StationRadioTrackWrapper> stationTrackWs;

        /// <summary>
        /// The index of the contained track that's currently playing
        /// </summary>
        private int currentTrackIndex;

#if !SKIP_IN_DOXYGEN
        // audio is on a separate threat to UnityEngine.Random so we need to use System.Random instead
        private System.Random random;
#endif

        /// <summary>
        /// The number of plays that need to happen before each track can be played again. Follows the layout described in \ref thresholdBeforeRepeats
        /// 
        /// i.e if tracks A, B and C are being randomly chosen with a \ref thresholdBeforeRepeats of 0.5f, they each need to have 1 other track play before each can repeat (see comments above thresholdBeforeRepeats)
        /// <br>So if track A was just played after B and C, this array would look like [1, 0, -1].
        /// That is, track A needs another track to be played once before it can be repeated, B doesn't need any other tracks to play and thus can be repeated, and same for C
        /// <br><i>(a number below 0 is treated as 0 for this system)</i><br><br>
        /// <b>See:</b> \ref NextTrack()
        /// </summary>
        private int[] remainingTracksBeforeRepeat;

#if !SKIP_IN_DOXYGEN
        // if this track has no stations, we want to print an error- but only once, otherwise it freezes the editor
        private bool hasPrintedError;
#endif

        /// <summary>
        /// A reference to the track that's currently playing
        /// </summary>
        private StationRadioTrackWrapper CurrentTrackW => stationTrackWs[currentTrackIndex];


        /// <summary>
        /// Initializes this station and all contained tracks
        /// </summary>
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

        /// <summary>
        /// When a RadioTrackPlayer for this station finishes the track we've given it, we update it to use whatever the next track chosen is. This method is called when the Player finishes, so we update it here.
        /// This works because the Player only gets destroyed if it's a one-shot- in which case only one track from the station will be playing anyway
        /// </summary>
        /// <param name="_callback">The callback invoked when the \ref RadioTrackPlayer ends</param>
        public override void AddToPlayerEndCallback(ref Action<RadioTrackPlayer> _callback)
        {
            _callback += p => NextTrack(); // choose the next track when the Player completes the last one
            _callback += p => p.UpdateSampleIncrement(); // update the Player's SampleCount/Length when the next track is chosen
        }

        /// <summary>
        /// Gets a sample from the currently playing track
        /// </summary>
        /// <param name="_sampleIndex">The index of the sample</param>
        /// <returns>A sample from \ref CurrentTrackW</returns>
        public override float GetSample(int _sampleIndex)
        {
            // get the sample from the currently playing track
            // station tracks also have their own gain variable so apply that
            return CurrentTrackW.GetSample(_sampleIndex) * CurrentTrackW.Gain;
        }

        /// <summary>
        /// Selects the next track for this station to play
        /// </summary>
        private void NextTrack()
        {
            // if there aren't any stations, alert the player and stop trying to get them
            if (stationTrackWs.Count == 0)
            {
                // if the error hasn't been printed, print it
                if (!hasPrintedError)
                { 
                    Debug.LogWarning($"Cannot play a StationRadioTrack, as there are no stations!"); 
                    hasPrintedError = true;
                }

                // otherwise stop
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