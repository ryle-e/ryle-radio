using RyleRadio.Tracks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyleRadio.Components
{
    /// <summary> 
    /// A component used to watch for specific happenings on a RadioOutput, e.g: a clip being a certain volume, a track starting, the tune being in a certain range\n
    /// We don't inherit from RadioComponent here since we don't need to use a RadioData ref, but it's very similar
    /// </summary>
    /// <remarks>
    /// One major thing to notice about this class is that each individual Observer component has specific tracks it's watching for- NOT each individual ObserverEvent.
    /// This is due to limitations of MultiselectAttribute- it doesn't display in nested lists properly. This is theoretically something I can fix, but I'm not fantastic at custom editors so I'd prefer to accept this limitation for now.
    /// With that being said, this means it *is* a bit cleaner to navigate multiple Observers in the scene- not all bad!
    /// </remarks>
    
    // This is partial as it's extended in ObserverEvent.cs
    [AddComponentMenu("Ryle Radio/Radio Observer")]
    public partial class RadioObserver : MonoBehaviour
    {
        /// <summary>
        /// The event an observer is looking for. 
        /// 
        /// Except for Trigger events, we need a value(s) to check for a change with. E.g: checking if volume is above a threshold. 
        /// Trigger events wait for a certain thing to happen that doesn't need a value. E.g: checking if a track has just started playing
        /// </summary>
        public enum EventType
        {
            OutputVolume, ///< The volume of the track: tune power * broadcast power * insulation
            Gain, ///< The gain of the track:  this is currently defined exclusively in the track's gain variable
            TunePower, ///< The tune power of the track: how close the RadioOutput.Tune value is to the range of the track
            BroadcastPower, ///< The broadcast power of the track:  how close to any active RadioBroadcasters the output's transform is
            Insulation, ///< The insulation of the track:  the higher the value the less insulation- the power of any RadioInsulator the output is inside of
            TrackEnds, ///< The track ends, or loops- this is a Trigger event
            TrackStarts, ///< The track starts, or loops (happens after TrackEnds)- this is a Trigger event
            OutputTune, ///< The tune on the RadioOutput is changed

            None, ///< Empty, mainly to temporarily disable an event without deleting it
        }

        /// <summary>
        /// The method of comparisonType used for an event. For example, checking if the volume is greater than a number, or in a certain range
        /// </summary>
        public enum ComparisonType
        {
            Equal, /// The value is equal to a number <remarks>We're using floats for almost every EventType with a value, so this won't be used often</remarks>
            GreaterThan, /// The value is greater than a number
            GreaterThanOrEqual, /// The value is greater than or equal to a number
            LessThan, /// The value is less than a number
            LessThanOrEqual, /// The value is less than or equal to a number
            BetweenInclusive, /// The value is between numbers x and y, including if it's equal to x or y
            BetweenExclusive, /// The value between numbers x and y, but not equal to x or y
        }

        /// <summary>
        /// The RadioOutput this observer is attached to 
        /// </summary>
        [SerializeField] private RadioOutput output;

        /// <summary>
        /// The tracks that this observer is watching for events on. This is a flag int and is translated to a list of names in \ref AffectedTracks
        /// </summary>
        [SerializeField, Multiselect("TrackNames")]
        private int affectedTracks;

        /// <summary>
        /// The events that this Observer responds uses, containing values/triggers to watch for and events to call
        /// </summary>
        [SerializeField] private List<ObserverEvent> events;

        /// <summary>
        /// A buffer for events to run on Update. 
        /// We cannot call \ref UnityEvents
        /// in the audio thread, so we need a buffer here so we can run them in \ref Update instead.
        /// 
        /// <b>See also: </b>\ref stayedEvents 
        /// </summary>
        private List<Action> toDoOnUpdate = new();

        /// <summary>
        /// A tracker for which ObserverEvents have been called for this specific frame- prevents us from calling an OnStay event hundreds of times in a frame due to the audio thread being WAY faster
        /// 
        /// <b>See also:</b> \ref toDoOnUpdate
        /// </summary>
        private List<ObserverEvent> stayedEvents = new();

#if !SKIP_IN_DOXYGEN
        // The names that the MultiselectAttribute on affectedTracks can use
        private List<string> TrackNames => output != null ? output.Data.TrackNames : new() { "Output not assigned!" };

#endif
       
        /// <summary>
        /// \ref affectedTracks as a list of names rather than a flag int. Created and cached in \ref AffectedTracks
        /// </summary>
        private string[] affectedTrackNames;

        /// <summary>
        /// The tracks selected on this Observer.
        /// 
        /// This is an accessor for \ref affectedTrackNames and \ref affectedTracks - uses MultiselectAttribute.To to automatically convert the flag int to a string array
        /// </summary>
        public string[] AffectedTracks
        {
            get
            {
                if (affectedTrackNames == null) // the work is done for us in the MultiselectAttribute. thank you multiselectattribute i love you multiselectattribute
                    affectedTrackNames = MultiselectAttribute.To<string>(affectedTracks, TrackNames.ToArray());

                return affectedTrackNames;
            }
        }


        /// <summary>
        /// Attaches the Observer to \ref output
        /// </summary>
        private void Awake()
        {
            // attach this observer to the output
            output.Observers.Add(this);
        }

        /// <summary>
        /// Detaches this Observer from \ref output
        /// </summary>
        private void OnDestroy()
        {
            output.Observers.Remove(this);
        }

        /// <summary>
        /// Run the buffered events and reset for the next frame
        /// </summary>
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

            // clear stay events from last frame so they can be called again this frame
            stayedEvents.Clear();
        }

        /// <summary>
        /// Assigns each event to a RadioTrackPlayer for one of our \ref AffectedTracks\. This is called when a new RadioTrackPlayer is created that's playing a Track in \ref AffectedTracks.
        /// </summary>
        /// <param name="_player">A RadioTrackPlayer playing one of our \ref AffectedTracks</param>
        public void AssignEvents(RadioTrackPlayer _player)
        {
            foreach (ObserverEvent e in events)
            {
                // if the OnStay event for this ObserverEvent has already been called this frame, don't call it again
                // if we didn't have this, it would be calling the OnStay events hundreds of times per frame
                if (stayedEvents.Contains(e)) 
                    break;
                else 
                    stayedEvents.Add(e);
                
                switch (e.eventType)
                {
                    // the _player events are mostly very similar- they give us the player it's called on, and the value
                    // we don't do anything with the player info in this script, but it's there for custom behaviour
                    //
                    // each sample, we check if a copy of this event has been called yet. If it has, we don't add another one, as it would hugely slow down at runtime
                    case EventType.OutputVolume:
                        _player.OnVolume += (player, volume) => { ValueEvent(e, volume); };
                        break;

                    case EventType.Gain:
                        _player.OnGain -= (player, gain) => { ValueEvent(e, gain); };
                        break;

                    case EventType.TunePower:
                        _player.OnTunePower += (player, tunePower) => { ValueEvent(e, tunePower); };
                        break;

                    case EventType.BroadcastPower:
                        _player.OnBroadcastPower += (player, power) => { ValueEvent(e, power); };
                        break;

                    case EventType.Insulation:
                        _player.OnInsulation += (player, insulation) => { ValueEvent(e, insulation); };
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
                        output.OnTune += (tune) => { ValueEvent(e, tune); };
                        break;

                    // an empty event gets no behaviour - pretty sure gandhi said this at one point idk man
                    default:
                    case EventType.None:
                        break;
                }
            }

        }

        /// <summary>
        /// A generic method letting an ObserverEvent tracking a value watch for its change.
        /// 
        /// This is what's called every time a Track's observed value is changed. If the new value fulfills the given ObserverEvent, it'll be called. E.g: volume is in given range- call event
        /// 
        /// <b>See also: \ref TriggerEvent()</b>
        /// </summary>
        /// <param name="_event">Contains the change we're watching for, and the event to call when the change happens</param>
        /// <param name="_value">The observed value right now</param>
        private void ValueEvent(ObserverEvent _event, float _value) 
        {
            // if this event doesn't use a comparisonType, this method should not have been called- tell the user
            if (!_event.NeedComparison)
            {
                Debug.LogWarning($"Attempting to use the FloatEvent method on trigger event {_event}! To fix, either modify the NeedComparison property in ObserverEvent, or change the method used in AssignEvents to TriggerEvent.");
                return;
            }

            // evaluate the comparisonType
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

        /// <summary>
        /// A generic method tracking if an ObserverEvent's trigger has been called. E.g: a track has just started playing, call event
        /// 
        /// <b>See also: </b> \ref ValueEvent()
        /// </summary>
        /// <param name="_event">Contains the trigger we're watching for, and the event to call when it's triggered</param>
        private void TriggerEvent(ObserverEvent _event)
        {
            // if the event is triggered, call the onTrigger event- the other events are not applicable
            lock (toDoOnUpdate)
                toDoOnUpdate.Add(() => _event.onTrigger.Invoke(1));
        }
    }

}