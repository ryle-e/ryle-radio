using RyleRadio.Components;
using System;
using UnityEngine;

namespace RyleRadio.Tracks
{

    /// <summary>
    /// A class that plays a certain \ref RadioTrack at runtime. It's created newly for each track on each RadioOutput, and manages the playback entirely.
    /// 
    /// As such, this script is a central point for information about the playback process.
    /// </summary>
    public class RadioTrackPlayer
    {
        /// <summary>
        /// The different types of Player- mainly changes what it does when the end of the track is reached
        /// </summary>
        public enum PlayerType
        {
            Loop, ///< When it ends, the player goes back to the start and plays the track again
            OneShot, ///< When it ends, the player destroys itself and stops playing
        }

        /// <summary>
        /// The track that this player is associated with and plays during runtime
        /// </summary>
        public RadioTrackWrapper TrackW { get; private set; }

        /// <summary>
        /// How many samples through the track this player is- not a whole number as we increment it with different values depending on the track's sample rate
        /// </summary>
        /// <remarks>
        /// This is stored as a double for greater precision with sample rates- using a float here causes clipping or distortion.
        /// <br>We could use a `decimal` here, but we're opting to change sample rates of the tracks rather than messing with them here
        /// </remarks>
        public decimal Progress { get; private set; } = 0;

        /// <summary>
        /// How far through the track this player is, from [0 - 1]
        /// </summary>
        public float ProgressFraction
        {
            get
            {
                // if progress is < 0, the player has finished i.e it's a one-shot
                if (Progress < 0)
                    return 1;

                float maxSamples = TrackW.SampleCount - 1; // get the total samples
                return Mathf.Clamp01((float)Progress / maxSamples); // get the progress at those samples
            }
        }

        /// <summary>
        /// The type of player this is- what happens when the track ends.
        /// </summary>
        public PlayerType PlayType { get; private set; }

        /// <summary>
        /// Event called in order to destroy this player- this can either be invoked directly or as part of the \ref Destroy() method.
        /// 
        /// We're using an event here so that other scripts can add their own functions to be called when this player is destroyed- e.g: removing one-shot players from a \ref RadioOutput
        /// </summary>
        public Action<RadioTrackPlayer> DoDestroy { get; set; } = new(_ => { });

        /// Event called when the player starts playing
        public Action<RadioTrackPlayer> OnPlay { get; set; } = new(_ => { });
        /// Event called when the player is stopped through \ref Stop()
        public Action<RadioTrackPlayer> OnStop { get; set; } = new(_ => { });
        /// Event called when the player's pause state is changed- the bool is true if the player is being paused, false if unpaused
        public Action<RadioTrackPlayer, bool> OnPause { get; set; } = new((_, _) => { });
        /// Event called when this player retreieves a sample from its track
        public Action<RadioTrackPlayer> OnSample { get; set; } = new(_ => { }); // when a sample is retrieved from this track

#if !SKIP_IN_DOXYGEN
        // internal event for OnEnd
        private Action<RadioTrackPlayer> onEnd = _ => { };
#endif
        /// Event called when the player reaches the end of its track naturally, before it takes an action depending on the \ref PlayType (e.g: looping). This is not invoked when the player is stopped or reset.
        public Action<RadioTrackPlayer> OnEnd 
        {
            get => onEnd; // we need an alias here as we're adressing the delegate with a ref in this class' constructor- we can't use ref on a property
            set => onEnd = value;
        }

        /// Event called when the volume of this player is captured for a sample. Volume is the product of Tune power, Broadcast power, and Insulation
        public Action<RadioTrackPlayer, float> OnVolume { get; set; } = new((_, _) => { });
        /// Event called when the gain for this player is captured for a sample. Gain is a direct change to the loudness of a track.
        public Action<RadioTrackPlayer, float> OnGain { get; set; } = new((_, _) => { });
        /// Event called when the tune power for this player is captured for a sample. Tune power is the loudness of a track based on the Tune value of the \ref RadioOutput
        public Action<RadioTrackPlayer, float> OnTunePower { get; set; } = new((_, _) => { });
        /// Event called when the broadcast power for this player is captured for a sample. Broadcast power is the loudness of a track based on the position of the \ref RadioOutput relative to any \ref RadioBroadcaster
        public Action<RadioTrackPlayer, float> OnBroadcastPower { get; set; } = new((_, _) => { });
        /// Event called when the insulation for this player is captured for a sample. Insulation is the quietness of a track based on the position of the \ref RadioOutput relative to any \ref RadioInsulator
        public Action<RadioTrackPlayer, float> OnInsulation { get; set; } = new((_, _) => { });

#if !SKIP_IN_DOXYGEN
        // internal value for Paused
        private bool paused = false;
#endif
        /// <summary>
        /// Whether or not this player has been paused, temporarily halting playback of the track. Changing this value pauses/unpauses the player
        /// </summary>
        public bool Paused 
        {
            get => paused;
            set
            {
                paused = value;
                OnPause(this, value); // call the delegate
            }
        }

        /// <summary>
        /// The amount that \ref Progress is increased by every sample- the ratio of the track's sample speed to the \ref baseSampleRate
        /// </summary>
        private decimal sampleIncrement;

        /// <summary>
        /// The sample rate of the \ref RadioOutput that this player is used by- that is, the sample rate of the radio
        /// </summary>
        private float baseSampleRate;

        /// <summary>
        /// Whether or not this player has been stopped- prevents it from being stopped multiple times
        /// </summary>
        private bool isStopped = false;


        /// <summary>
        /// Creates a new player for the provided track
        /// </summary>
        /// <param name="_trackW">The track for this player to play</param>
        /// <param name="_playerType">The type of player this is (what happens when the track ends)</param>
        /// <param name="_baseSampleRate">The sample rate of the RadioOutput using this Player</param>
        public RadioTrackPlayer(RadioTrackWrapper _trackW, PlayerType _playerType, float _baseSampleRate)
        {
            TrackW = _trackW;
            Progress = 0;

            PlayType = _playerType;

            // assign the sample rate of the Oistener
            baseSampleRate = _baseSampleRate;
            UpdateSampleIncrement();

            // add any applicable track methods to the end delegate
            TrackW.AddToPlayerEndCallback(ref onEnd);
        }

        /// <summary>
        /// Updates the \ref sampleIncrement variable to match the current track
        /// </summary>
        public void UpdateSampleIncrement()
        {
            // we need to have a float variable for sampleIncrement as the sample rate of the track and the sample rate of the output may be different
            // if they're different, it means that incrememnting Progress by 1 will make this track sound faster or slower depending on its SampleRate
            // in order to counteract this, we set up a specific increment as the ratio between these two sample rates
            // 
            // let's say that the Output's sampleRate is 44100, but the track's sample rate is 48000
            // if we incremented Progress by 1 every sample, then, the track would sound slightly slower than usual as we're using the wrong SampleRate
            // if we use this method though, we get an increment of ~0.92
            // if we increment it by 0.92 every sample, then, the track sounds to be playing at the correct speed!
            //
            // this can get a bit confusing when we use Progress to get the sample INDEX, though, as we can't get an index with a float
            // the solution here is just to convert it to an int, as seen in GetSample below
            // this means that instead of skipping samples, it simply repeats the same one until the increment reaches the next sample
            // the samples are happening so quickly that this repeat is completely unnoticeable- as far as i can tell, this is how all audio
            // software and programs operate. as such, we use it here :)
            //
            // note: this explanation was mostly for my own future reference if i forget how this works lol
            sampleIncrement = (decimal)TrackW.SampleRate / (decimal)baseSampleRate;

            //if (TrackW.id == "music_old2")
            //    Debug.Log(TrackW.SampleCount);
        }

        /// <summary>
        /// Gets the current sample from the track according to playback. <i>I would recommend reading the code comments for this method as they explain how the entire sample playback and evaluation process works</i>
        /// </summary>
        /// <param name="_tune">The tune value of the Output</param>
        /// <param name="_receiverPosition">The position of the Output</param>
        /// <param name="_otherVolume">The sum of the samples of previous tracks, according to the order in \ref RadioData.<br><b>See: </b>\ref RadioTrack.attenuation</param>
        /// <param name="_outVolume">The volume of this sample to be added to `_otherVolume`</param>
        /// <param name="_applyVolume">Whether or not Volume (`tune power * broadcast power * insulation`) should be applied</param>
        /// <returns>The current sample</returns>
        public float GetSample(float _tune, Vector3 _receiverPosition, float _otherVolume, out float _outVolume, bool _applyVolume = true)
        {
            // if this track is paused, return silence
            if (isStopped || Paused)
            {
                _outVolume = 0f;
                return 0;
            }

            // the output volume of this track right now
            float volume = 0;
            float gain = 100;

            if (_applyVolume)
            {
                // long explanation of each method is in here so this can be kind of a core script for documentation

                // get the gain of the track- this is a variable assigned in the inspector that serves as a basic increase to the
                // loudness of the track without affecting attenuation or other audio values
                // if the gain is at 100, it will be at the default loudness
                // if the gain is at 200, it will be double the loudness
                // if the gain is at 50, it will be half the loudness
                gain = TrackW.Gain;
                OnGain(this, gain);

                // get the tunePower of the track- this is a combination of the track's tuning power and attenuation
                // the tuning power is defined by how closely the Output is tuned to this track. e.g, if this track's range is 100 - 300, and the
                // Output's tune is 200, the track will likely be very loud- but if the tune is 120, it will be very quiet- the amount of loudness or
                // quietness defined by the tune is named tune power here
                //
                // as well as the tuning power, this value is created with attenutation: how much quieter this track is when there are others playing
                // the _otherVolume variable is the volume of other samples so far- if the attenuation of the track is high, then the tunePower will be
                // lower depending on how high the other tracks' calculated tunePower is
                //
                // for more info, check inside the GetTunePower method
                float tunePower = TrackW.GetTunePower(_tune, _otherVolume);
                OnTunePower(this, tunePower);

                // get the broadcast power of the track- this is dependent on where the Output is in relation to any RadioBroadcasters in the scene
                // if the range of a Broadcaster is 100 units, and the output is 5 units away from it, it will likely hear the track loudly
                // if the output is 95 units away, though, it will be heard quietly
                // this works with many broadcasters and outputs- see RadioBroadcaster for more info
                float broadcastPower = GetBroadcastPower(_receiverPosition);
                OnBroadcastPower(this, broadcastPower);

                // get the insulation of the track- this is effectively the inverse of broadcastPower, and is dependent on the position of the Output
                // if the Insulator is a box 100 units wide, and the output is outside of it- the insulationMultiplier will be set to 1, as the track
                // is not being insulated at all. if the output is inside the box, though, insulationMultiplier will be < 1 depending on the power in
                // the RadioInsulator script.
                // this was introduced to simulate "dead zones", or areas in which a broadcast can't be heard. if your output is inside an Insulator,
                // the track will sound quieter- hence a lower insulationMultiplier the stronger the insulation
                float insulationMultiplier = GetInsulation(_receiverPosition);
                OnInsulation(this, insulationMultiplier);

                // combine the broadcast power, tune power and insulation multiplier to the unified volume
                volume = tunePower * broadcastPower * insulationMultiplier;
                OnVolume(this, volume);
            }

            // get the sample at this moment from the track, and apply the volume and tunePower to it
            float sample = TrackW.GetSample((int)Progress) * gain * volume;

            // give back the volume to the Output
            _outVolume = volume;

            // and return the sample
            return sample;
        }

        /// <summary>
        /// Move this player to the next sample
        /// </summary>
        public void IncrementSample()
        {
            // if this track can't be played, don't increment its sample
            if (isStopped || Paused)
                return;

            // the normal method if incrementing the sample if the track hasn't ended
            void NormalIncrement()
            {
                OnSample.Invoke(this); // invoke the sample callback

                // increment progress with the sampleIncrement, and clamp it between 0 and the maximum sample count
                // see UpdateSampleIncrement() for more info on the sampleIncrement
                Progress = Math.Clamp(Progress + sampleIncrement, 0, TrackW.SampleCount - 1);

                UpdateSampleIncrement();
            }

            switch (PlayType)
            {
                // if this player loops when the track ends,
                case PlayerType.Loop:
                    if (ProgressFraction >= 1) // if the track has ended,
                    {
                        OnEnd.Invoke(this); // invoke the end callback
                        Progress = 0; // reset the progress to 0, i.e loop the track
                        OnPlay.Invoke(this); // invoke the play callback as the track has been restarted
                    }
                    else // if the track is still partway playing,
                    {
                        NormalIncrement(); // increment normally
                    }

                    break;

                // if this player only plays once,
                case PlayerType.OneShot:
                    if (ProgressFraction >= 1) // if the track has ended,
                    {
                        OnEnd.Invoke(this); // invoke the end callback
                        Stop(); // stop and destroy this track
                    }
                    else
                    {
                        NormalIncrement(); // increment normally
                    }

                    break;
            }
        }

        /// <summary>
        /// Stop playback and destroy this player. Make sure any references to it are removed as well
        /// </summary>
        public void Stop()
        {
            if (isStopped) // if it's already been stopped, don't do it again
                return;

            isStopped = true;

            // the track does not reach the end, so we don't invoke the end callback
            // but we do invoke the stop callback
            OnStop.Invoke(this);

            // destroy this player
            Destroy();
        }

        /// <summary>
        /// Resets the \ref Progress of this player to 0
        /// </summary>
        public void ResetProgress()
        {
            Progress = 0;
        }

        /// <summary>
        /// Invokes \ref DoDestroy, destroying the player
        /// </summary>
        public void Destroy()
        {
            DoDestroy.Invoke(this);
        }

        /// <summary>
        /// Gets the broadcast power of this player using the position of the Output and any any \ref RadioBroadcaster
        /// </summary>
        /// <param name="_receiverPosition">The position of the Output</param>
        /// <returns>The broadcast power- higher the closer the Output is to broadcasters</returns>
        public float GetBroadcastPower(Vector3 _receiverPosition)
        {
            // if this track is forced to be global, it is forced to not use broadcasters- as such, its broadcast power is always 1
            if (TrackW.forceGlobal)
                return 1;

            float outPower = 0;

            // if this track has broadcasters,
            if (TrackW.broadcasters.Count > 0)
            {
                foreach (RadioBroadcaster broadcaster in TrackW.broadcasters) // get the power from each broadcaster
                    outPower = Mathf.Max(outPower, broadcaster.GetPower(_receiverPosition)); // select the highest broadcast power and use that

                // short discussion- this initially just added the broadcast power of each broadcaster together and used that- but after some thinking
                // and research, i've changed it to use the maximum power overall to be closest to real-world function
                // in real life, if we put too weak broadcasters next to each other with a receiver in the middle, the receiver doesn't get the combined
                // signal of both broadcasters- it just gets two weak signals, from which is chooses the strongest. as such, we recreate this here-
                // combining the power of the signals just wouldn't make sense from a practical standpoint
                //
                // i will note, however, that i was debating making this a modifiable option, like a BroadcasterCombineType package preference or
                // something, but for the time being we're just using the one method here
            }
            // if this track has no broadcasters (and is therefore global),
            else
                outPower = 1; // the broadcast power is 1

            // return the broadcast power, which should only be from 0 to 1
            return Mathf.Clamp01(outPower);
        }

        /// <summary>
        /// Gets the insulation multiplier of this player using the position of the output and the bounds of any \ref RadioInsulator
        /// </summary>
        /// <param name="_receiverPosition">The position of the Output</param>
        /// <returns>The insulation multiplier- the more insulated the Output is, the lower the multiplier</returns>
        public float GetInsulation(Vector3 _receiverPosition)
        {
            // if there are no insulators, the multiplier is 1
            float outGain = 1;

            foreach (RadioInsulator insulator in TrackW.insulators) // get the insulation power from each insulator
                outGain -= insulator.GetPower(_receiverPosition); // subtract that power from the insulation multiplier

            // shorter discussion- we use the opposite method to broadcast power here, that is combining all the insulation into one value- this makes
            // more sense to me than just choosing the maximum- i imagine if we put two lead blocks in front of a receiever, the signal will be reduced
            // by the combined insulation of both blocks, not just the biggest one
            // (if this is incorrect, please tell me so i don't sound too stupid :(((( )

            // return the insulation multiplier, which can only be from 0 to 1
            return Mathf.Clamp01(outGain);
        }
    }

}