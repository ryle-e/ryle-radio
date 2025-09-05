using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class RadioBroadcaster : MonoBehaviour
{
    public static Action InitBroadcasters { get; private set; }

    [SerializeField] protected RadioData data;

    [SerializeField, Dropdown("TrackNames")]
    protected string selectedTrack;

    //[Radii]
    public Vector2 broadcastRadius;

    [SerializeField, CurveRange(0, 0, 1, 1)] 
    protected AnimationCurve distanceFalloff = new(new Keyframe[2] {
        new(0, 1, 0, 0),
        new(1, 0, 0, 0)
    });

    private string lastTrackAssignedToName = "";

    private Vector3 cachedPos;

    public Action<RadioBroadcaster> OnInit { get; set; } = new(_ => { });

    protected List<string> TrackNames => data != null ? data.TrackNames: new() { "Data not assigned!" };

    public RadioData Data => data;


    private void Awake()
    {
        InitBroadcasters += Init;
    }

    private void OnDestroy()
    {
        InitBroadcasters -= Init;
    }

    private void Update()
    {
        cachedPos = transform.position;
    }

    private void Init()
    {
        AssignToTrack();
        OnInit(this);
    }

    public void AssignToTrack()
    {
        if (lastTrackAssignedToName != "")
        {
            if (data.TryGetTrack(lastTrackAssignedToName, out RadioTrackWrapper lastTrack, false))
            { 
                lastTrack.broadcasters.Remove(this);
                lastTrack.OnRemoveBroadcaster(this, lastTrack);
            }
            else
                Debug.LogWarning("Couldn't remove broadcaster " + gameObject.name + " from track " + lastTrackAssignedToName + "!");
        }

        if (data.TryGetTrack(selectedTrack, out RadioTrackWrapper track, false))
        { 
            track.broadcasters.Add(this);
            track.OnAddBroadcaster(this, track);

            lastTrackAssignedToName = selectedTrack;
        }
        else
            Debug.LogWarning("Couldn't add broadcaster " + gameObject.name + " to track " + selectedTrack + "!");
    }

    public float GetPower(Vector3 _receiverPos)
    {
        float distance = Vector3.Distance(cachedPos, _receiverPos);
        float power = Mathf.Clamp01(Mathf.InverseLerp(broadcastRadius.x, broadcastRadius.y, distance));

        return distanceFalloff.Evaluate(power);
    }
}