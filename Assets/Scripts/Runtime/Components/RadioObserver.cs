using NaughtyAttributes;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RadioObserver : MonoBehaviour
{
    public enum EventType
    {
        TrackVolume,
        BroadcasterProximity,
        TrackInsulated,
        TrackLoops,
        OneShotEnds,
        StationSwitches,
    }

    [System.Serializable]
    public class ObserverEvent
    {
        public EventType type;

        [Foldout("Events"), AllowNesting] public UnityEvent onTrigger;
        [Foldout("Events"), AllowNesting] public UnityEvent onStay;
        [Foldout("Events"), AllowNesting] public UnityEvent onEnd;
    }

    [SerializeField] private List<ObserverEvent> events;
}
