using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    [SerializeField, Range(RadioData.LOW_TUNE, RadioData.HIGH_TUNE)] 
    private float tune;

    private List<RadioTrackPlayer> players = new();

    private Vector3 cachedPos;

    private float baseSampleRate;

    public List<string> TrackIDs => data.TrackNames;

    public RadioData Data => data;
    
    public float Tune 
    { 
        get => tune; 
        set => tune = Mathf.Clamp(tune, RadioData.LOW_TUNE, RadioData.HIGH_TUNE); 
    }


    private void Start()
    {
        Init();
    }

    private void Update()
    {
        cachedPos = transform.position;
    }

    public void Init()
    {
        baseSampleRate = AudioSettings.outputSampleRate;

        data.Init();

        StartPlayers();
    }

    private void StartPlayers()
    {
        foreach (RadioTrackWrapper trackW in Data.TrackWrappers)
        {
            if (trackW.playOnInit)
            {
                RadioTrackPlayer player = new RadioTrackPlayer(trackW, RadioTrackPlayer.PlayerType.Loop, baseSampleRate);
                players.Add(player);
            }
        }
    }

    public RadioTrackPlayer PlayOneShot(string _id)
    {
        if (Data.TryGetTrack(_id, out RadioTrackWrapper track))
        {
            RadioTrackPlayer player = new(track, RadioTrackPlayer.PlayerType.OneShot, baseSampleRate);
            player.DoDestroy += player => players.Remove(player);

            players.Add(player);

            return player;
        }

        return null;
    }

    public RadioTrackPlayer PlayLoop(string _id)
    {
        if (Data.TryGetTrack(_id, out RadioTrackWrapper track))
        {
            RadioTrackPlayer player = new(track, RadioTrackPlayer.PlayerType.Loop, baseSampleRate);
            players.Add(player);

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

    private void OnAudioFilterRead(float[] data, int channels)
    {
        for (int index = 0; index < data.Length; index += channels)
        {
            float sample = 0;
            float otherGain = 0;

            // get combined audio
            foreach (RadioTrackPlayer player in players)
            {
                sample += player.GetSample(index / channels, Tune, cachedPos, otherGain, out float outGain, true); // get the audio at this sample
                otherGain += outGain; // store the gain so far so that trackWs with attenuation can adjust accordingly
            }

            foreach (RadioTrackPlayer player in players)
            {
                player.IncrementSample();
            }

            //sample /= players.Count;

            // this function uses data for each sample packed into one big list- each channel is joined end to end
            // that means if there are multiple channels, we need to read through each of them before jumping to the next sample
            // therefore we iterate through every channel, for every sample
            for (int channel = index; channel < index + channels; channel++) 
            {
                data[channel] += sample; // apply the sample
            }
        }

    }
}
