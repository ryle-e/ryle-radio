using NaughtyAttributes;
using RyleRadio.Components.Base;
using RyleRadio.Tracks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RyleRadio.Components
{

    /// <summary>
    /// The main scene component for a radio that plays it through an AudioSource.
    /// 
    /// <b>See </b>\ref RadioTrackPlayer as well for more info on how playback works
    /// </summary>
    [AddComponentMenu("Ryle Radio/Radio Output")]
    [RequireComponent(typeof(AudioSource))]
    public class RadioOutput : RadioComponent
    {
        /// <summary>
        /// The method by which a RadioTrackPlayer is chosen from this output. Really only matters when you're playing the same track repeatedly causing overlaps
        /// </summary>
        public enum MultiplePlayersSelector
        {
            Youngest, ///< Selects the youngest player
            Oldest, ///< Selects the oldest player
            Random ///< Selects a random player (probably useless but funny to have)
        }

        /// <summary>
        /// The current tune value of this output- akin to the frequency of a real radio. Controls what tracks can be heard through tune power. Never modify this directly except for in the inspector, use \ref Tune instead
        /// </summary>
        [SerializeField, Range(RadioData.LOW_TUNE, RadioData.HIGH_TUNE), OnValueChanged("ExecOnTune")]
        protected float tune;

        /// <summary>
        /// The players used by this output
        /// </summary>
        protected List<RadioTrackPlayer> players = new();

        /// <summary>
        /// The position of this object as of the last frame update. We can't access `transform.position` from the audio thread, so we cache it here
        /// </summary>
        protected Vector3 cachedPos;

        /// <summary>
        /// The normal sample rate of this output, applied to each \ref RadioTrackPlayer
        /// </summary>
        private float baseSampleRate;

        /// <summary>
        /// Called at the end of every audio cycle so that we don't interrupt threads when manipulating RadioTrackPlayers
        /// </summary>
        private Action playEvents = () => { };

        /// <summary>
        /// Every \ref RadioObserver associated with this output
        /// </summary>
        public List<RadioObserver> Observers { get; private set; } = new();

        /// <summary>
        /// Event called whenever \ref Tune is changed
        /// </summary>
        public Action<float> OnTune { get; set; } = new(_ => { });

        /// <summary>
        /// The \ref tune clamped to the full range
        /// </summary>
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

        /// <summary>
        /// \ref Tune scaled to [0 - 1], useful for UI
        /// </summary>
        public float Tune01
        {
            get => Mathf.InverseLerp(RadioData.LOW_TUNE, RadioData.HIGH_TUNE, Tune);
        }

        /// <summary>
        /// \ref Tune with limited decimal points- looks better when displayed, more like an actual radio
        /// </summary>
        public float DisplayTune
        {
            get => Mathf.Round(tune * 10) / 10;
        }

        /// <summary>
        /// Called when tune is modified in the inspector
        /// </summary>
        private void ExecOnTune()
        {
            // we invoke the callback here too so that it reacts when the tune is changed in the inspector
            OnTune(tune);
        }


        /// <summary>
        /// Updates \ref cachedPos
        /// </summary>
        protected virtual void Update()
        {
            // cache the position of this output
            cachedPos = transform.position;
        }

#if !SKIP_IN_DOXYGEN
        // starts the radio system- this component basically serves as a manager
        private void Start()
        {
            LocalInit();
        }
#endif

        // we have to separate this and Init as otherwise data.Init() would call Init(), which calls data.Init(), which calls Init()......
        // this is just a consequence of how the RadioComponent class works really
        /// <summary>
        /// Initializes the RadioData- this needs to be separated from \ref Init() as it would be recursive otherwise
        /// </summary>
        protected void LocalInit()
        {
            // initialize the associated RadioData
            // note: this should be the only component that calls this- just for safety
            data.Init();

        }

        /// <summary>
        /// Initializes the output itself, and creates all required every required \ref RadioTrackPlayer
        /// </summary>
        public override void Init()
        {
            // save the sample rate of the whole Unity player
            baseSampleRate = AudioSettings.outputSampleRate;

            // create and start all track players
            StartPlayers();

            OnTune(tune);
        }

        /// <summary>
        /// Sets up and stores a new \ref RadioTrackPlayer and alerts any \ref RadioObserver of its creation
        /// </summary>
        /// <param name="_player">The new player to set up</param>
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
            int indexInData = Data.TrackIDs.IndexOf(_player.TrackW.id);

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
                players.Insert(indexForNewPlayer, _player);
        }

        /// <summary>
        /// Creates every \ref RadioTrackPlayer that this output needs for playback
        /// </summary>
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

        /// <summary>
        /// Plays a track as a one-shot. A one-shot destroys itself when its track ends.
        /// </summary>
        /// <param name="_id"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Plays a track as a loop. A loop restarts when the track ends, then continues to play.
        /// </summary>
        /// <param name="_id"></param>
        /// <returns></returns>
        public RadioTrackPlayer PlayLoop(string _id)
        {
            // get the track with the given id
            if (Data.TryGetTrack(_id, out RadioTrackWrapper track))
            {
                // create a new player for the track, set to loop
                RadioTrackPlayer player = new(track, RadioTrackPlayer.PlayerType.Loop, baseSampleRate);

                // store the player
                lock (playEvents)
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
        /// <summary>
        /// Gets an active \ref RadioTrackPlayer from this output.
        /// </summary>
        /// <param name="_trackID">The ID of the track used by the player</param>
        /// <param name="_player">Output parameter containing the found player</param>
        /// <param name="_createNew">Whether or not a new player should be created if one can't be found. Players created this way are always one-shots</param>
        /// <param name="_multiplePlayerSelector">How a player is selected when multiple are present for the same track</param>
        /// <returns>True if a player was found or created, false if not</returns>
        public bool TryGetPlayer(
            string _trackID,
            out RadioTrackPlayer _player,
            bool _createNew = false,
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

        /// <summary>
        /// Gets a set of samples from the radio to play from the AudioSource- this preserves the settings on the Source, e.g: volume, 3D. This is the main driving method for the radio's playback.
        /// 
        /// The method itself appears to have been initially introduced so devs could create custom audio filters, but it just so happens we can use it for direct output of samples too!
        /// </summary>
        /// <param name="_data">Whatever other audio is playing from the AudioSource- preferably nothing</param>
        /// <param name="_channels">The number of channels the AudioSource is using- the radio itself is limited to one channel, but still outputs as two- they'll just be identical.</param>
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

            // execute all events waiting for this read sequence to end
            lock (playEvents)
            {
                playEvents(); // execute the delegate
                playEvents = () => { }; // clear it
            }
        }

    }

}