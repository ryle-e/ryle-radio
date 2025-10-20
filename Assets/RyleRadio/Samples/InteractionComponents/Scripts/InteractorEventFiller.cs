using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InteractorEventFiller : MonoBehaviour
{
    [System.Serializable]
    private class FillerEvent
    {
        public KeyCode key;
        public UnityEvent uEvent;
    }

    [SerializeField] private List<FillerEvent> events;

    private void Update()
    {
        foreach (var e in events)
            if (Input.GetKeyDown(e.key))
                e.uEvent.Invoke();
    }
}
