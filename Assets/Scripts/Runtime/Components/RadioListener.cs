using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Random = UnityEngine.Random;

[AddComponentMenu("Ryle Radio/Radio Listener")]
[RequireComponent(typeof(AudioSource))]
public class RadioListener : MonoBehaviour
{
    public enum MultiplePlayersSelector
    {
        Youngest,
        Oldest,
        Random
    }

    [SerializeField] private RadioData data;

    [SerializeField, Range(RadioData.LOW_TUNE, RadioData.HIGH_TUNE), OnValueChanged("ExecOnTune")] 
    protected float tune;

    protected List<RadioTrackPlayer> players = new();

    protected Vector3 cachedPos;

    private float baseSampleRate;

    public List<string> TrackIDs => data.TrackNames;

    public RadioData Data => data;

    public List<RadioObserver> Observers { get; private set; } = new();
    public Action<float> OnTune { get; set; } = new(_ => { });
    
    public float Tune 
    { 
        get => tune;
        set
        {
            tune = Mathf.Clamp(value, RadioData.LOW_TUNE, RadioData.HIGH_TUNE);
            OnTune(tune);
        }
    }


    private void Start()
    {
        Init();
    }

    protected virtual void Update()
    {
        cachedPos = transform.position;
    }

    private void ExecOnTune()
    {
        OnTune(tune);
    }

    public virtual void Init()
    {
        baseSampleRate = AudioSettings.outputSampleRate;

        data.Init();

        StartPlayers();
    }

    public void PlayerCreation(RadioTrackPlayer _player)
    {
        foreach (RadioObserver observer in Observers)
        {
            if (observer.AffectedTracks.Contains(_player.TrackW.name))
                observer.AssignEvents(_player);
        }

        players.Add(_player);
    }

    private void StartPlayers()
    {
        foreach (RadioTrackWrapper trackW in Data.TrackWrappers)
        {
            if (trackW.playOnInit)
            {
                RadioTrackPlayer player = new RadioTrackPlayer(trackW, RadioTrackPlayer.PlayerType.Loop, baseSampleRate);
                PlayerCreation(player);
            }
        }
    }

    public RadioTrackPlayer PlayOneShot(string _id)
    {
        if (Data.TryGetTrack(_id, out RadioTrackWrapper track))
        {
            RadioTrackPlayer player = new(track, RadioTrackPlayer.PlayerType.OneShot, baseSampleRate);
            PlayerCreation(player);

            player.DoDestroy += player => players.Remove(player);

            return player;
        }

        return null;
    }

    public RadioTrackPlayer PlayLoop(string _id)
    {
        if (Data.TryGetTrack(_id, out RadioTrackWrapper track))
        {
            RadioTrackPlayer player = new(track, RadioTrackPlayer.PlayerType.Loop, baseSampleRate);
            PlayerCreation(player);

            return player;
        }

        return null;
    }

    public bool TryGetPlayer(string _trackID, out RadioTrackPlayer _player, bool _createNew, MultiplePlayersSelector _multiplePlayerSelector = MultiplePlayersSelector.Youngest)
    {
        var found = players.Where(p => p.TrackW.id == _trackID);

        if (found.Count() > 1)
        {
            switch (_multiplePlayerSelector)
            {
                default:
                case MultiplePlayersSelector.Youngest:
                    _player = found.Last();
                    break;

                case MultiplePlayersSelector.Oldest:
                    _player = found.First();
                    break;

                case MultiplePlayersSelector.Random:
                    int index = Random.Range(0, found.Count());
                    _player = found.ElementAt(index);

                    break;
            }

            return true;
        }
        else if (found.Count() > 0)
        {
            _player = found.First();
            return true;
        }
        else if (_createNew)
        {
            _player = PlayLoop(_trackID);
            return true;
        }
        else
        {
            _player = null;
            return false;
        }
    }

    protected virtual void OnAudioFilterRead(float[] _data, int _channels)
    {
        int monoSampleCount = _data.Length / _channels;

        for (int index = 0; index < monoSampleCount; index++)
        {
            float sample = 0;
            float otherGain = 0;

            // get combined audio
            foreach (RadioTrackPlayer player in players)
            {
                sample += player.GetSample(index, Tune, cachedPos, otherGain, out float outGain, true); // get the audio at this sample
                player.IncrementSample();

                //if (outGain > 0)
                //    Debug.Log(sample);

                otherGain += outGain; // store the gain so far so that trackWs with attenuation can adjust accordingly
            }

            //sample /= players.Count;

            // this function uses _data for each sample packed into one big list- each channel is joined end to end
            // that means if there are multiple _channels, we need to read through each of them before jumping to the next sample
            // therefore we iterate through every channel, for every sample
            int indexWithChannels = index * _channels;

            for (int channel = indexWithChannels; channel < indexWithChannels + _channels; channel++) 
                _data[channel] += sample; // apply the sample

            //Debug.Log(_data[index] + " " + _data[index + 1] + " " + _data[Mathf.Clamp(index + 2, 0, _data.Length)]);
        }

    }
}
