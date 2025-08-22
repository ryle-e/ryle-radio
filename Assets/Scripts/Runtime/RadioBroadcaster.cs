using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class RadioBroadcaster : MonoBehaviour
{
    private static Action initBroadcasters = new(() => { });
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

    protected List<string> TrackNames => data != null ? data.TrackNames: new() { "Data not assigned!" };

    private void Awake()
    {
        InitBroadcasters += AssignToTrack;
    }

    private void OnDestroy()
    {
        InitBroadcasters -= AssignToTrack;
    }

    private void Update()
    {
        cachedPos = transform.position;
    }

    public void AssignToTrack()
    {
        if (lastTrackAssignedToName != "")
        {
            if (data.TryGetTrack(lastTrackAssignedToName, out RadioTrack lastTrack))
                lastTrack.broadcasters.Remove(this);
            else
                Debug.LogWarning("Couldn't remove broadcaster " + gameObject.name + " from track " + lastTrackAssignedToName + "!");
        }

        
        if (data.TryGetTrack(selectedTrack, out RadioTrack track))
        { 
            track.broadcasters.Add(this);
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

#if UNITY_EDITOR
[CustomEditor(typeof(RadioBroadcaster))]
public class RadioBroadcasterEditor : Editor
{
    private void OnSceneGUI()
    {
        RadioBroadcaster broadcaster = (RadioBroadcaster)target;
        Vector2 o = broadcaster.broadcastRadius;

        Handles.color = Color.magenta;

        o.x = Mathf.Min(o.y, Handles.RadiusHandle(Quaternion.identity, broadcaster.transform.position, o.x));
        o.y = Mathf.Max(o.x, Handles.RadiusHandle(Quaternion.identity, broadcaster.transform.position, o.y));

        broadcaster.broadcastRadius = o;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}
#endif