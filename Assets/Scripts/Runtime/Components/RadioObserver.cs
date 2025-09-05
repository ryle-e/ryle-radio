using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;


// a premade class used to watch for specific happenings on a RadioListener, e.g: a clip being a certain volume, a track starting, the tune being in a certain range
[AddComponentMenu("Ryle Radio/Radio Observer")]
public class RadioObserver : MonoBehaviour
{
    // the event an observer is looking for
    public enum EventType
    {
        OutputVolume, // the volume of the track:  gain * broadcast power * insulation
        GainTune, // the gain of the track:  this is combined from the gain variable on the track and the tuning power on the listener
        BroadcastPower, // the broadcast power of the track:  how close to any active RadioBroadcasters the listener is
        Insulation, // the insulation of the track:  the higher the value the less insulation- the power of any RadioInsulator the listener is in
        TrackEnds, // the track ends, or loops
        TrackStarts, // the track starts, or loops (happens after TrackEnds)
        Tune, // the tune on the listener is changed

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

    [System.Serializable]
    public class ObserverEvent
    {
        public EventType type;

        [AllowNesting, ShowIf("NeedComparison")]
        public ComparisonType comparison;

        // a value to compare to- clamped between 0 and 1
        // shows if the event has a comparison, is not using a range, and is not set to tune
        [AllowNesting, ShowIf(EConditionOperator.And, "NeedComparison", "NotNeedVector", "NotIsTune"), Range(0, 1)]
        public float clampedValue;

        // a value to compare to- clamped between 0 and 1000
        // shows if the event has a comparison, is not using a range, and is set to tune
        [AllowNesting, ShowIf(EConditionOperator.And, "NeedComparison", "NotNeedVector", "IsTune"), Range(0.0f, 1000.0f)]
        public float tuneValue;

        // a range to compare to- clamped between 0 and 1
        // shows if the event has a comparison, is using a range, and is not set to tune
        [AllowNesting, ShowIf(EConditionOperator.And, "NeedComparison", "NeedVector", "NotIsTune"), MinMaxSlider(0, 1)]
        public Vector2 clampedRange;

        // a range to compare to- clamped between 0 and 100
        // shows if the event has a comparison, is using a range, and is set to tune
        [AllowNesting, ShowIf(EConditionOperator.And, "NeedComparison", "NeedVector", "IsTune"), MinMaxSlider(0, 1000)]
        public Vector2 tuneRange;

        // for inspector cleanliness, show/hide the events as they are quite big
        public bool showEvents;

        // called when the event is triggered
        // if this has a comparison, then it can be triggered more than once while the comparison is true- e.g the volume can remain above 0.5
        // therefore, with a comparison, this changes to mean the FIRST time the event is triggered
        [ShowIf("showEvents"), AllowNesting] 
        public UnityEvent onTrigger;

        // called when the event remains triggered
        // this is not called without a comparison
        // with a comparison, this is called if the event is triggered, and has been triggered just before
        [ShowIf(EConditionOperator.And, "showEvents", "NeedComparison"), AllowNesting] 
        public UnityEvent onStay;

        // called when the event ends
        // this is not called without a comparison
        [ShowIf(EConditionOperator.And, "showEvents", "NeedComparison"), AllowNesting] 
        public UnityEvent onEnd;

        // tracks if the event has happened before
        [HideInInspector] public bool staying = false;

        // if this event needs a value to compare to
        public bool NotNeedComparison => !NeedComparison;
        public bool NeedComparison =>
            type == EventType.OutputVolume
            || type == EventType.GainTune
            || type == EventType.BroadcastPower
            || type == EventType.Insulation
            || type == EventType.Tune;

        // if this event needs a range to compare to
        public bool NotNeedVector => !NeedVector;
        public bool NeedVector =>
            comparison == ComparisonType.BetweenInclusive
            || comparison == ComparisonType.BetweenExclusive;

        // if this event is set to track Tune
        public bool NotIsTune => !IsTune;
        public bool IsTune => 
            type == EventType.Tune;


        // if this event is a comparison, check if the comparison is satisfied
        public bool EvaluateComparison(float _cValue)
        {
            // if the event is not a comparison you should not be calling this method (unless it's being used on many events at once)
            if (NotNeedComparison)
                return false;

            float value;
            Vector2 range;

            // if this event uses Tune, then make sure we're comparing to the tune values
            if (IsTune)
            {
                range = tuneRange;
                value = tuneValue;
            }
            else
            {
                value = clampedValue;
                range = clampedRange;
            }

            // evaluate the specific comparison- just logical operators
            return comparison switch
            {
                ComparisonType.Equal => _cValue == value,
                ComparisonType.GreaterThan => _cValue > value,
                ComparisonType.GreaterThanOrEqual => _cValue >= value,
                ComparisonType.LessThan => _cValue < value,
                ComparisonType.LessThanOrEqual => _cValue <= value,

                ComparisonType.BetweenExclusive => range.x < _cValue && _cValue < range.y, // i want (x < value < y) notation >:)
                ComparisonType.BetweenInclusive => range.x <= _cValue && _cValue <= range.y,

                _ => false,
            };
        }
    }
    
    // the listener this observer is attached to
    [SerializeField] private RadioListener listener;

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
    private List<string> TrackNames => listener != null ? listener.Data.TrackNames : new() { "Listener not assigned!" };

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
        // attach this observer to the listener
        listener.Observers.Add(this);
    }

    private void OnDestroy()
    {
        listener.Observers.Remove(this);
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

                case EventType.GainTune:
                    _player.OnGain += (player, gain) => { StayEvent(e, gain); };
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

                // this event watches the tune of the listener- it doesn't actually use a player here
                case EventType.Tune:
                    listener.OnTune += (tune) => { StayEvent(e, tune); };
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
                    toDoOnUpdate.Add(() => _event.onTrigger.Invoke());
            }
            // and this is not the first time the event is called,
            else
            {
                // call the onStay event
                lock (toDoOnUpdate) 
                    toDoOnUpdate.Add(() => _event.onStay.Invoke());
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
                    toDoOnUpdate.Add(() => _event.onEnd.Invoke());            }
        }
    }

    // a generic approach to tracking a trigger rather than a value
    private void TriggerEvent(ObserverEvent _event)
    {
        // if the event is triggered, call the onTrigger event- the other events are not applicable
        lock(toDoOnUpdate)
            toDoOnUpdate.Add(() => _event.onTrigger.Invoke());
    }
}
