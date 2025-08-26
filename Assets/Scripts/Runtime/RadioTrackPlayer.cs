using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class RadioTrackPlayer
{
    public enum PlayerType
    {
        Once,
        Loop,
        OneShot,
    }

    //public RadioTrack Track { get; private set; }
    public RadioTrackWrapper TrackW { get; private set; }
    public RadioTrack Track => TrackW.track;

    public float Progress { get; private set; } = 0;
    public float ProgressFraction => Mathf.Clamp01((float)Progress / (Track.SampleLength - 1));

    public PlayerType PlayType { get; private set; }

    public Action<RadioTrackPlayer> DoDestroy { get; set; } = _ => { };

    private float sampleRateRatio;


    public RadioTrackPlayer(RadioTrackWrapper _trackW)
    {
        TrackW = _trackW;
        PlayType = _trackW.track.PlayerType;

        Progress = 0;

        if (TrackW.broadcasters.Count <= 0 && !TrackW.isGlobal)
            Debug.LogWarning("Track " + TrackW.id + " is not global, but has no RadioBroadcasters in the scene! It will not be heard playing until a RadioBroadcaster is created.");

        sampleRateRatio = _trackW.track.sampleKL
    }


    public float NextSample(float _tune, Vector3 _receiverPosition, float _otherGain, out float _outGain, bool _applyGain = true)
    {
        if (Progress < -99) // if completed as a oneshot, do not provide any more samples
        {
            Debug.LogWarning("Attempting to move to the next sample on a completed RadioTrackPlayer set to OneShot.");

            _outGain = 0f;
            return 0; 
        }

        float gain = TrackW.GetGain(_tune, _otherGain) * GetBroadcastPower(_receiverPosition);
        float sample = Track.GetSample(Progress) * (_applyGain ? gain : 1);

        _outGain = gain;

        switch (PlayType)
        {
            case PlayerType.Once:
                if (ProgressFraction >= 1)
                    Progress = -1;

                else if (Progress >= 0)
                    Progress = Mathf.Clamp(Progress + Track.Channels, 0, Track.SampleLength - 1);

                break;

            case PlayerType.Loop:
                if (ProgressFraction >= 1)
                    Progress = 0;
                
                else
                    Progress = Mathf.Clamp(Progress + Track.Channels, 0, Track.SampleLength - 1);

                break;

            case PlayerType.OneShot:
                if (ProgressFraction >= 1)
                {
                    Progress = -1;
                    DoDestroy.Invoke(this);
                }
                else
                    Progress = Mathf.Clamp(Progress + Track.Channels, 0, Track.SampleLength - 1);

                break;
        }

        return sample;
    }

    public float GetBroadcastPower(Vector3 _receiverPosition)
    {
        if (Track.isGlobal)
            return 1;

        float outGain = 0;

        foreach (RadioBroadcaster broadcaster in Track.broadcasters)
            outGain += broadcaster.GetPower(_receiverPosition);

        return Mathf.Clamp01(outGain);
    }
}