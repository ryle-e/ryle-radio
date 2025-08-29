using System;
using UnityEngine;

public class RadioTrackPlayer
{
    public enum PlayerType
    {
        Loop,
        OneShot,
    }

    //public RadioTrack Track { get; private set; }
    public RadioTrackWrapper TrackW { get; private set; }

    public double Progress { get; private set; } = 0;
    public float ProgressFraction
    {
        get
        {
            if (Progress < 0)
                return 1;

            float maxSamples = TrackW.SampleCount - 1;
            return Mathf.Clamp01((float)Progress / maxSamples);
        }
    }

    public PlayerType PlayType { get; private set; }

    public Action<RadioTrackPlayer> DoDestroy { get; set; } = _ => { };

    public Action<RadioTrackPlayer> OnPlay { get; set; } = _ => { }; // when the track starts to play
    public Action<RadioTrackPlayer> OnStop { get; set; } = _ => { }; // when the track is stopped and destroyed
    public Action<RadioTrackPlayer> OnPause { get; set; } = _ => { }; // when the track is paused partway
    public Action<RadioTrackPlayer> OnSample { get; set; } = _ => { };
    public Action<RadioTrackPlayer> OnEnd  // when the track reaches the end, not when it stops (i.e when the loop happens)
    { 
        get => onEnd; 
        set => onEnd = value; 
    }

    public bool Paused { get; set; } = false;

    private double sampleIncrement;
    private float baseSampleRate;

    private Action<RadioTrackPlayer> onEnd = _ => { };

    private bool isStopped = false;


    public RadioTrackPlayer(RadioTrackWrapper _trackW, PlayerType _playerType, float _baseSampleRate)
    {
        TrackW = _trackW;
        Progress = 0;

        PlayType = _playerType;

        baseSampleRate = _baseSampleRate;
        UpdateSampleIncrement();

        TrackW.AddToPlayerEndCallback(ref onEnd);

        Debug.Log(baseSampleRate + " " + TrackW.SampleRate + " " + sampleIncrement);

        if (TrackW.broadcasters.Count <= 0 && !TrackW.isGlobal)
            Debug.LogWarning("Track " + TrackW.id + " is not global, but has no RadioBroadcasters in the scene! It will not be heard playing until a RadioBroadcaster is created.");
    }

    public void UpdateSampleIncrement()
    {
        sampleIncrement = TrackW.SampleRate / baseSampleRate;
    }


    public float GetSample(int _channel, float _tune, Vector3 _receiverPosition, float _otherGain, out float _outGain, bool _applyGain = true)
    {
        if (isStopped || Paused)
        {
            _outGain = 0f;
            return 0; 
        }

        float gain = TrackW.GetGain(_tune, _otherGain) * GetBroadcastPower(_receiverPosition);
        float sample = TrackW.GetSample((int) Progress) * (_applyGain ? gain : 1);

        _outGain = gain;

        return sample;
    }

    public void IncrementSample()
    {
        switch (PlayType)
        {
            case PlayerType.Loop:
                if (ProgressFraction >= 1)
                {
                    OnEnd.Invoke(this);
                    Progress = 0;
                }
                else
                {
                    OnSample.Invoke(this);
                    Progress = Math.Clamp(Progress + sampleIncrement, 0, TrackW.SampleCount - 1);
                }

                break;

            case PlayerType.OneShot:
                if (ProgressFraction >= 1)
                {
                    OnEnd.Invoke(this);
                    Stop();
                }
                else
                {
                    OnSample.Invoke(this);
                    Progress = Math.Clamp(Progress + sampleIncrement, 0, TrackW.SampleCount - 1);
                }

                break;
        }
    }

    public void Stop()
    {
        if (isStopped)
            return;

        isStopped = true;

        switch (PlayType)
        {
            case PlayerType.Loop:
                OnStop.Invoke(this); // the track does not reach the end, so it just stops
                DoDestroy(this);

                break;

            case PlayerType.OneShot:
                OnStop.Invoke(this);
                DoDestroy(this);

                break;
        }

        OnStop.Invoke(this);
    }

    public float GetBroadcastPower(Vector3 _receiverPosition)
    {
        if (TrackW.isGlobal)
            return 1;

        float outGain = 0;

        foreach (RadioBroadcaster broadcaster in TrackW.broadcasters)
            outGain += broadcaster.GetPower(_receiverPosition);

        return Mathf.Clamp01(outGain);
    }
}