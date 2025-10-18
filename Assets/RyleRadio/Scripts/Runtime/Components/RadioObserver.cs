using RyleRadio.Tracks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyleRadio.Components
{

    // a component used to watch for specific happenings on a RadioOutput, e.g: a clip being a certain volume, a track starting, the tune being in a certain range
    // we don't inherit from RadioComponent here since we don't need to use a RadioData ref, but it's very similar
    // this is also partial as it's extended in ObserverEvent.cs
    [AddComponentMenu("Ryle Radio/Radio Observer")]
    public partial class RadioObserver : MonoBehaviour
    {
        // the event an observer is looking for
        public enum EventType
        {
            OutputVolume, // the volume of the track:  gain * broadcast power * insulation
            Gain, // the gain of the track:  this is combined from the gain variable on the track and the tuning power on the output
            TunePower, // the gain of the track:  this is combined from the gain variable on the track and the tuning power on the output
            BroadcastPower, // the broadcast power of the track:  how close to any active RadioBroadcasters the output is
            Insulation, // the insulation of the track:  the higher the value the less insulation- the power of any RadioInsulator the output is in
            TrackEnds, // the track ends, or loops
            TrackStarts, // the track starts, or loops (happens after TrackEnds)
            OutputTune, // the tune on the output is changed

            None // empty, mainly to temporarily disable an event without deleting it
        }

        // the method of comparison used for the value or range provided in the event
        public enum ComparisonType
        {
            Equal,
            GreaterThan,
            GreaterThanOrEqual,
            LessThan,
            LessThanOrEqual,
            BetweenInclusive, // between x and y, including if it's equal to x or y
            BetweenExclusive, // between x and y, but not equal to x or y
        }

        // the output this observer is attached to
        [SerializeField] private RadioOutput output;

        // the tracks that this observer is watching for events on
        // due to editor attribute limitations, all these events apply to the same tracks. if you want different events for different tracks, you need to make new observers
        [SerializeField, Multiselect("TrackNames")]
        private int affectedTracks;

        // the events tracked in this observer
        [SerializeField] private List<ObserverEvent> events;

        // as the audio methods are on a separate thread to UnityEvents, we need to create a buffer to run in Update
        // if we don't do this, we get errors from the UnityEvents e.g "IsPlaying can only be called from the main thread"
        // tl;dr: audio is on a separate thread to UnityEvents, this solves that issue
        private List<Action> toDoOnUpdate = new();

        // the names that the multiselect can pull from
        private List<string> TrackNames => output != null ? output.Data.TrackNames : new() { "Output not assigned!" };

        // the affected tracks as a list of names rather than an int
        // cached
        private string[] affectedTrackNames;
        public string[] AffectedTracks
        {
            get
            {
                if (affectedTrackNames == null) // the work is done for us in the MultiselectAttribute. thank you multiselectattribute i love you multiselectattribute
                    affectedTrackNames = MultiselectAttribute.To<string>(affectedTracks, TrackNames.ToArray());

                return affectedTrackNames;
            }
        }


        private void Awake()
        {
            // attach this observer to the output
            output.Observers.Add(this);
        }

        private void OnDestroy()
        {
            output.Observers.Remove(this);
        }

        private void Update()
        {
            // time to cache the update queue
            // we need to do this as toDoOnUpdate could be changed on a different thread while we proceed through it, causing errors
            List<Action> locUpdate;

            // lock the update queue to clone it uninterrupted
            lock (toDoOnUpdate)
            {
                locUpdate = new(toDoOnUpdate);
            }

            // move through the update queue to call the associated events
            foreach (Action a in locUpdate)
            {
                a();

                // remove this event from the update queue
                lock (toDoOnUpdate)
                    toDoOnUpdate.Remove(a);
            }
        }

        // link up each event to the associated track player
        public void AssignEvents(RadioTrackPlayer _player)
        {
            foreach (ObserverEvent e in events)
            {
                switch (e.type)
                {
                    // the _player events are mostly very similar- they give us the player it's called on, and the value
                    // we don't do anything with the player info in this script, but it's there for custom behaviour
                    case EventType.OutputVolume:
                        _player.OnVolume += (player, volume) => { StayEvent(e, volume); };
                        break;

                    case EventType.Gain:
                        _player.OnGain += (player, gain) => { StayEvent(e, gain); };
                        break;

                    case EventType.TunePower:
                        _player.OnTunePower += (player, tunePower) => { StayEvent(e, tunePower); };
                        break;

                    case EventType.BroadcastPower:
                        _player.OnBroadcastPower += (player, power) => { StayEvent(e, power); };
                        break;

                    case EventType.Insulation:
                        _player.OnInsulation += (player, insulation) => { StayEvent(e, insulation); };
                        break;

                    // this is a single event rather than a value- there is no value associated with the track starting or ending, it just starts or ends
                    case EventType.TrackStarts:
                        _player.OnPlay += (player) => { TriggerEvent(e); };
                        break;

                    case EventType.TrackEnds:
                        _player.OnEnd += (player) => { TriggerEvent(e); };
                        break;

                    // this event watches the tune of the output- it doesn't actually use a player here
                    case EventType.OutputTune:
                        output.OnTune += (tune) => { StayEvent(e, tune); };
                        break;

                    // an empty event gets no behaviour - gandhi
                    default:
                    case EventType.None:
                        break;
                }
            }

        }

        // a generic approach to an event tracking a value- we use a method here since we'd just be duplicating the code otherwise
        private void StayEvent(ObserverEvent _event, float _value) 
        {
            // if this event doesn't use a comparison, this method should not have been called- tell the user
            if (!_event.NeedComparison)
            {
                Debug.LogWarning($"Attempting to use the FloatEvent method on trigger event {_event}! To fix, either modify the NeedComparison property in ObserverEvent, or change the method used in AssignEvents to TriggerEvent.");
                return;
            }

            // evaluate the comparison
            if (_event.EvaluateComparison(_value))
            {
                // if it passes,

                // and this is the first time the event is called,
                if (!_event.staying)
                {
                    _event.staying = true;

                    // call the onTrigger event
                    lock (toDoOnUpdate)
                    { 
                        toDoOnUpdate.Add(() => _event.onTrigger.Invoke(_value)); 
                    }
                }
                // and this is not the first time the event is called,
                else
                {
                    // call the onStay event
                    lock (toDoOnUpdate)
                    { 
                        toDoOnUpdate.Add(() => _event.onStay.Invoke(_value)); 
                    }
                }
            }
            else
            {
                // if it doesn't pass,

                // and the event has just been called,
                if (_event.staying)
                {
                    _event.staying = false;

                    // call the onEnd event
                    lock (toDoOnUpdate)
                    {
                        toDoOnUpdate.Add(() => _event.onEnd.Invoke(_value));
                    }
                }
            }
        }

        // a generic approach to tracking a trigger rather than a value
        private void TriggerEvent(ObserverEvent _event)
        {
            // if the event is triggered, call the onTrigger event- the other events are not applicable
            lock (toDoOnUpdate)
                toDoOnUpdate.Add(() => _event.onTrigger.Invoke(1));
        }
    }

}