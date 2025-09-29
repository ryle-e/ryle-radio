using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

// a component that plays a RadioData through an AudioSource
// most of the documentation explaining specific parts of playback is in RadioTrackPlayer.cs, so check there as well
[AddComponentMenu("Ryle Radio/Radio Output")]
[RequireComponent(typeof(AudioSource))]
public class RadioOutput : RadioComponent
{
    // when attempting to pull a RadioTrackPlayer from this output, you can choose the method by which it picks it
    // this only matters if you're playing a bunch of one-shots of the same track
    public enum MultiplePlayersSelector
    {
        Youngest,
        Oldest,
        Random
    }

    // the current tune of this output- controls what tracks can be heard and when
    [SerializeField, Range(RadioData.LOW_TUNE, RadioData.HIGH_TUNE), OnValueChanged("ExecOnTune")]
    protected float tune;

    // the players applied to this output;
    protected List<RadioTrackPlayer> players = new();

    // the position of this output as of the last update
    // we need to cache this value as audio is on a separate thread to the rest of Unity- this means we get errors if we access position as
    // transform.position rather than caching it here
    protected Vector3 cachedPos;

    // the sample rate of the player
    private float baseSampleRate;

    private Action playEvents = () => { };

    // all observers associated with this output
    public List<RadioObserver> Observers { get; private set; } = new();

    // called whenever the tune is changed
    public Action<float> OnTune { get; set; } = new(_ => { });
    
    public float Tune 
    { 
        get => tune;
        set
        {
            // clamp the tune to the available range (not needed in inspector, needed if tune changed in code)
            tune = Mathf.Clamp(value, RadioData.LOW_TUNE, RadioData.HIGH_TUNE);

            // invoke the tune change callback
            OnTune(tune);
        }
    }


    // called when the tune is changed in the inspector
    private void ExecOnTune()
    {
        // we invoke the callback here too so that it reacts when the tune is changed in the inspector
        OnTune(tune);
    }


    protected virtual void Update()
    {
        // cache the position of this output
        cachedPos = transform.position;
    }

    // starts the radio system- this component basically serves as a manager
    private void Start()
    {
        LocalInit();
    }

    // we have to separate this and Init as otherwise data.Init() would call Init(), which calls data.Init(), which calls Init()......
    // this is just how the RadioComponent class works really
    protected void LocalInit()
    {
        // initialize the associated RadioData
        // note: this should be the only component that calls this- just for safety
        data.Init();

    }

    public override void Init()
    {
        // save the sample rate of the whole Unity player
        baseSampleRate = AudioSettings.outputSampleRate;

        // create and start all track players
        StartPlayers();

        OnTune(tune);
    }

    // stores a new RadioTrackPlayer and alerts any observers
    protected void PlayerCreation(RadioTrackPlayer _player)
    {
        foreach (RadioObserver observer in Observers)
        {
            // if an observer is watching this track
            if (observer.AffectedTracks.Contains(_player.TrackW.name))
            {
                observer.AssignEvents(_player); // point it towards this new player
            }
        }

        // below here is a section where we insert the new player into the list while preserving the order of the tracks in the Data inspector.
        // we do this so that attenuation is preserved. if we just added players to the list one by one, the order would change, and so tracks
        // that are supposed to get quieter when another is played (attenuate) will not do so if the other player was created afterwards.

        // the index of the track in the RadioData track list- the order of the Data in the inspector
        int indexInData = Data.TrackIDs.IndexOf(_player.TrackW.name);

        // the index at which to put the newly created player
        int indexForNewPlayer = players.Count;

        // search through the currently existent players to find one with a higher index in Data
        for (int i = 0; i < players.Count; i++)
        {
            if (Data.TrackIDs.IndexOf(players[i].TrackW.id) > indexInData)
            { 
                indexForNewPlayer = i;
                break;
            }
        }

        // store the player
        if (players.Count == 0)
            players.Add(_player);
        else
            players.Insert(indexForNewPlayer + 1, _player);
    }

    // create all RadioTrackPlayers for this output
    private void StartPlayers()
    {
        // for every track in the RadioData
        foreach (RadioTrackWrapper trackW in Data.TrackWrappers)
        {
            // if the track is supposed to play on game start
            if (trackW.playOnInit)
            {
                // create a looping player for it
                RadioTrackPlayer player = new RadioTrackPlayer(trackW, RadioTrackPlayer.PlayerType.Loop, baseSampleRate);

                // and store the player
                PlayerCreation(player);
            }
        }
    }

    // play a track as a one-shot- a one-shot destroys itself after the track ends
    public RadioTrackPlayer PlayOneShot(string _id)
    {
        // get the track with the given id
        if (Data.TryGetTrack(_id, out RadioTrackWrapper track))
        {
            // create a new player for the track, set to one-shot
            RadioTrackPlayer player = new(track, RadioTrackPlayer.PlayerType.OneShot, baseSampleRate);

            // store the player
            lock (playEvents)
                playEvents += () => PlayerCreation(player);

            // ensure that the new player is cleaned up when it's destroyed
            player.DoDestroy += player => 
            {
                lock (playEvents)
                    playEvents += () => players.Remove(player);
            };

            // return the player
            return player;
        }
        // if it can't find a player with the id, warn the user
        else
        {
            Debug.LogWarning($"Can't find track with id {_id} to play as a one-shot!");
            return null;
        }
    }

    // play a track as a loop- it will restart when the track ends, and keep playing
    public RadioTrackPlayer PlayLoop(string _id)
    {
        // get the track with the given id
        if (Data.TryGetTrack(_id, out RadioTrackWrapper track))
        {
            // create a new player for the track, set to loop
            RadioTrackPlayer player = new(track, RadioTrackPlayer.PlayerType.Loop, baseSampleRate);

            // store the player
            lock(playEvents)
                playEvents += () => PlayerCreation(player);

            // return the player
            return player;
        }
        // if it can't find a player with the id, warn the user
        else
        {
            Debug.LogWarning($"Can't find track with id {_id} to play as a loop!");
            return null;
        }

    }

    // try to find an active RadioTrackPlayer on this output
    public bool TryGetPlayer(
        string _trackID, 
        out RadioTrackPlayer _player, 
        bool _createNew, 
        MultiplePlayersSelector _multiplePlayerSelector = MultiplePlayersSelector.Youngest
    )
    {
        // find any players associated with the track with the given id
        var found = players.Where(p => p.TrackW.id == _trackID);

        // if there are multiple players,
        if (found.Count() > 1)
        {
            // select one based off the given selector
            switch (_multiplePlayerSelector)
            {
                default: // choose the youngest player
                case MultiplePlayersSelector.Youngest:
                    _player = found.Last();
                    break;

                // choose the oldest player
                case MultiplePlayersSelector.Oldest:
                    _player = found.First();
                    break;

                // choose a random player
                case MultiplePlayersSelector.Random:
                    int index = Random.Range(0, found.Count());
                    _player = found.ElementAt(index);

                    break;
            }

            // a player was found, so return true
            return true;
        }
        // if there is one player,
        else if (found.Count() > 0)
        {
            // choose it and return true
            _player = found.First();
            return true;
        }

        // if there aren't any players for this track,

        // and the method is set to make a new one,
        else if (_createNew)
        {
            // create a new player for this track
            // if you want it to be a loop, play it manually- this is more of an error catch than an actual method of creation
            _player = PlayOneShot(_trackID);
            return true; // and return true
        }
        // and we aren't creating a new one,
        else
        {
            _player = null; // there is no player
            return false; // so we return false
        }
    }

    // the core method- this is where the audio is output to the linked AudioSource
    // this method appears to have initially been introduced for custom audio filters, but we can use it for custom audio output too
    //
    // _data here is whatever other audio is playing from the source- normally is nothing, unless you wanted some other audio playing
    // if you do want this i would recommend using a separate audiosource so that you can control the volume separately
    protected virtual void OnAudioFilterRead(float[] _data, int _channels)
    {
        // the output only plays back one channel, so we have to account for this when the radio is using
        // tracks with more than one channel

        // the number of samples for, total- the _data array has an entry for each channel by default. e.g if _data was 2048
        // entries long, and there were two channels- there would actually only be 1024 samples
        int monoSampleCount = _data.Length / _channels;

        // for every sample in the data array
        for (int index = 0; index < monoSampleCount; index++)
        {
            // we want to store the sample itself, and the added volume of all previous tracks
            float sample = 0;
            float combinedVolume = 0;

            // for every active track,
            foreach (RadioTrackPlayer player in players)
            {
                // get the audio in this sample
                sample += player.GetSample(Tune, cachedPos, combinedVolume, out float outVolume, true);

                // then move along to the next sample
                player.IncrementSample();

                // store the volume so far so that tracks with attenuation can adjust accordingly- see RadioTrackPlayer.GetSample()
                combinedVolume += outVolume;
            }

            // this function uses _data for each sample packed into one big list- each channel is joined end to end
            // that means if there are multiple _channels, we need to apply the audio to each of them before jumping to the next sample
            // therefore we iterate through every channel, for every sample
            int indexWithChannels = index * _channels;

            // for each channel,
            for (int channel = indexWithChannels; channel < indexWithChannels + _channels; channel++) 
                _data[channel] += sample; // apply the sample
        }

        lock (playEvents)
        {
            playEvents();
            playEvents = () => { };
        }
    }

}
