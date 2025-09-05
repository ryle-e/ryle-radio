using NaughtyAttributes;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

public class RadioObserver : MonoBehaviour
{
    public enum EventType
    {
        OutputVolume,
        Gain,
        BroadcastPower,
        Insulation,
        TrackEnds,
        TrackStarts,
        Tune,
    }

    public enum ComparisonType
    {
        Equal,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        BetweenInclusive,
        BetweenExclusive,
    }

    [System.Serializable]
    public class ObserverEvent
    {
        public EventType type;

        [AllowNesting, ShowIf("NeedComparison")]
        public ComparisonType comparison;

        [AllowNesting, HideIf("NeedVector"), ShowIf("NeedComparison")]
        public float value;

        [AllowNesting, ShowIf(EConditionOperator.And, "NeedComparison", "NeedVector")]
        public Vector2 range;

        public bool showEvents;

        [ShowIf("showEvents"), AllowNesting] 
        public UnityEvent onTrigger;

        [ShowIf(EConditionOperator.And, "showEvents", "NeedComparison"), AllowNesting] 
        public UnityEvent onStay;

        [ShowIf(EConditionOperator.And, "showEvents", "NeedComparison"), AllowNesting] 
        public UnityEvent onEnd;

        [HideInInspector] public bool staying = false;

        public bool NeedComparison =>
            type == EventType.OutputVolume
            || type == EventType.Gain
            || type == EventType.BroadcastPower
            || type == EventType.Insulation
            || type == EventType.Tune;

        public bool NeedVector =>
            comparison == ComparisonType.BetweenInclusive
            || comparison == ComparisonType.BetweenExclusive;

        public bool EvaluateComparison(float _cValue)
        {
            return comparison switch
            {
                ComparisonType.Equal => _cValue == value,
                ComparisonType.GreaterThan => _cValue > value,
                ComparisonType.GreaterThanOrEqual => _cValue >= value,
                ComparisonType.LessThan => _cValue < value,
                ComparisonType.LessThanOrEqual => _cValue <= value,

                ComparisonType.BetweenExclusive => range.x < _cValue && _cValue < range.y,
                ComparisonType.BetweenInclusive => range.x <= _cValue && _cValue <= range.y,

                _ => false,
            };
        }
    }

    [SerializeField] private RadioListener listener;

    [SerializeField, Multiselect("TrackNames")] 
    private int affectedTracks;

    [SerializeField] private List<ObserverEvent> events;

    private List<Action> toDoOnUpdate = new();

    private List<string> TrackNames => listener != null ? listener.Data.TrackNames : new() { "Listener not assigned!" };

    private string[] affectedTrackNames;
    public string[] AffectedTracks
    {
        get
        {
            if (affectedTrackNames == null)
                affectedTrackNames = MultiselectAttribute.To<string>(affectedTracks, TrackNames.ToArray());

            return affectedTrackNames;
        }
    }

    
    private void Awake()
    {
        listener.Observers.Add(this);
    }

    private void OnDestroy()
    {
        listener.Observers.Remove(this);
    }

    private void Update()
    {
        List<Action> locUpdate;

        lock (toDoOnUpdate)
        {
            locUpdate = new(toDoOnUpdate);
        }

        foreach (Action a in locUpdate)
        { 
            a();

            lock (toDoOnUpdate)
                toDoOnUpdate.Remove(a);
        }
    }

    public void AssignEvents(RadioTrackPlayer _player)
    {
        foreach (ObserverEvent e in events)
        {
            switch (e.type)
            {
                case EventType.OutputVolume:
                    _player.OnVolume += (player, volume) => { FloatEvent(e, volume); };
                    break;

                case EventType.Gain:
                    _player.OnGain += (player, gain) => { FloatEvent(e, gain); };
                    break;

                case EventType.BroadcastPower:
                    _player.OnBroadcastPower += (player, power) => { FloatEvent(e, power); };
                    break;

                case EventType.Insulation:
                    _player.OnInsulation += (player, insulation) => { FloatEvent(e, insulation); };
                    break;

                case EventType.TrackStarts:
                    _player.OnPlay += (player) => { TriggerEvent(e); };
                    break;

                case EventType.TrackEnds:
                    _player.OnEnd += (player) => { TriggerEvent(e); };
                    break;

                case EventType.Tune:
                    listener.OnTune += (tune) => { FloatEvent(e, tune); };
                    break;
            }
        }

    }

    private void FloatEvent(ObserverEvent _event, float _value)
    {
        if (!_event.NeedComparison)
        {
            Debug.LogWarning($"Attempting to use the FloatEvent method on trigger event {_event}! To fix, either modify the NeedComparison property in ObserverEvent, or change the method used in AssignEvents.");
        }

        if (_event.EvaluateComparison(_value))
        {
            if (!_event.staying)
            { 
                _event.staying = true;

                lock (toDoOnUpdate) 
                    toDoOnUpdate.Add(() => _event.onTrigger.Invoke());
            }
            else
            {
                lock (toDoOnUpdate) 
                    toDoOnUpdate.Add(() => _event.onStay.Invoke());
            }
        }
        else
        {
            if (_event.staying)
            {
                _event.staying = false;

                lock (toDoOnUpdate) 
                    toDoOnUpdate.Add(() => _event.onEnd.Invoke());            }
        }
    }

    private void TriggerEvent(ObserverEvent _event)
    {
        lock(toDoOnUpdate)
            toDoOnUpdate.Add(() => _event.onTrigger.Invoke());
    }
}
