using System;
using UnityEngine;

// a class that plays a given RadioTrack at runtime- this manages the playback in a output, and as such will be created newly for each Output
// this is effectively a central point for most documentation about how a track is played
public class RadioTrackPlayer
{
    // what happens when this player ends
    public enum PlayerType
    {
        Loop, // when it ends, the player goes back to the start and plays it again
        OneShot, // when it ends, the player destroys itself and stops playing
    }

    // the associated track
    public RadioTrackWrapper TrackW { get; private set; }

    // how many samples through the track are we
    // we use floats here so that we can easily translate between sample rates- see UpdateSampleIncrement()
    public float Progress { get; private set; } = 0;
    public float ProgressFraction // progress through the track from 0 - 1
    {
        get
        {
            // if progress is < 0, the player has finished i.e it's a one-shot
            if (Progress < 0)
                return 1;

            float maxSamples = TrackW.SampleCount - 1; // get the total samples
            return Mathf.Clamp01(Progress / maxSamples); // get the progress at those samples
        }
    }

    // the chosen type ==> what happens when this player ends
    public PlayerType PlayType { get; private set; }

    // called to destroy this player
    public Action<RadioTrackPlayer> DoDestroy { get; set; } = new(_ => { });

    public Action<RadioTrackPlayer> OnPlay { get; set; } = new(_ => { }); // when the track starts to play
    public Action<RadioTrackPlayer> OnStop { get; set; } = new(_ => { }); // when the track is stopped and destroyed
    public Action<RadioTrackPlayer, bool> OnPause { get; set; } = new((_,_) => { }); // when the track is paused partway
    public Action<RadioTrackPlayer> OnSample { get; set; } = new(_ => { }); // when a sample is retrieved from this track

    private Action<RadioTrackPlayer> onEnd = _ => { };
    public Action<RadioTrackPlayer> OnEnd  // when the track reaches the end, not when it stops (i.e when the loop happens)
    { 
        get => onEnd; // we need an alias here as we're adressing the delegate with a ref in the constructor- we can't use ref on a property
        set => onEnd = value; 
    }

    public Action<RadioTrackPlayer, float> OnVolume { get; set; } = new((_,_) => { }); // when the volume is calculated
    public Action<RadioTrackPlayer, float> OnGain { get; set; } = new((_,_) => { }); // when the gain is calculated
    public Action<RadioTrackPlayer, float> OnBroadcastPower { get; set; } = new((_,_) => { }); // when the broadcast power is calculated
    public Action<RadioTrackPlayer, float> OnInsulation { get; set; } = new((_,_) => { }); // when the insulationMultiplier is calculated

    private bool paused = false;
    public bool Paused  // pauses playback temporarily
    {
        get => paused;
        set
        {
            paused = value;
            OnPause(this, value); // call the delegate
        }
    }

    private float sampleIncrement; // the amount that the sample progress is increased every increment
    private float baseSampleRate; // the sample rate of the Output this is associated with

    private bool isStopped = false; // if this player has been stopped already, e.g reaches the end of a one-shot


    // creates a new player for a particular track
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

    // update the increment added to the progress each sample
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
        sampleIncrement = TrackW.SampleRate / baseSampleRate;
    }

    // gets the sample at the current progress, using the tune, broadcaster/insulators, attenuation, gain, etc
    public float GetSample(float _tune, Vector3 _receiverPosition, float _otherVolume, out float _outVolume, bool _applyVolume = true)
    {
        // if this track is paused, return silence
        if (isStopped || Paused)
        {
            _outVolume = 0f;
            return 0; 
        }

        // the output volume of this track right now
        float volume = 1;

        if (_applyVolume)
        {
            // long explanation of each method is in here so this can be kind of a core script for documentation

            // get the gain of the track- this is a combination of the track's individual Gain variable, as well as its tuning power and attenuation
            // the tuning power is defined by how closely the Output is tuned to this track. e.g, if this track's range is 100 - 300, and the
            // Output's tune is 200, the track will likely be very loud- but if the tune is 120, it will be very quiet- the amount of loudness or
            // quietness defined by the tune is named tune power here
            //
            // as well as the tuning power, this value is created with attenutation: how much quieter this track is when there are others playing
            // the _otherVolume variable is the volume of other samples so far- if the attenuation of the track is high, then the gain will be
            // lower depending on how high the other tracks' calculated gain is
            //
            // multiply the track's Gain variable to tune power and attenutation, and we get the gain stored here
            // for more info, check inside the GetGain method
            float gain = TrackW.GetGain(_tune, _otherVolume);
            OnGain(this, gain);

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

            // combine the gain, broadcast power and insulation multiplier to the unified volume
            volume = gain * broadcastPower * insulationMultiplier;
            OnVolume(this, volume);
        }

        // get the sample at this moment from the track, and apply the volume to it
        float sample = TrackW.GetSample((int) Progress) * volume;

        // give back the volume to the Output
        _outVolume = volume;

        // and return the sample
        return sample;
    }

    // move to the next sample in the track
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

    // stop and destroy this player
    public void Stop()
    {
        if (isStopped) // if it's already been stopped, don't do it again
            return;

        isStopped = true;

        // the track does not reach the end, so we don't invoke the end callback
        // but we do invoke the stop callback
        OnStop.Invoke(this);

        // destroy this player
        DoDestroy(this);
    }

    // resets the progress of this player to 0
    public void ResetProgress()
    {
        Progress = 0;
    }

    // get the broadcast power of the current track depending on its position
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

    // get the insulation multiplier of the current track depending on its posiiton
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