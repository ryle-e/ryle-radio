using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using System;


#if UNITY_EDITOR
using UnityEditor;
#endif

[AddComponentMenu("Ryle Radio/Radio Broadcaster")]
public class RadioBroadcaster : RadioComponentTrackAccessor
{
    [Space(8)]
    public Vector2 broadcastRadius;

    [SerializeField, CurveRange(0, 0, 1, 1)] 
    protected AnimationCurve distanceFalloff = new(new Keyframe[2] {
        new(0, 1, 0, 0),
        new(1, 0, 0, 0)
    });

    private Vector3 cachedPos;


    private void Update()
    {
        cachedPos = transform.position;
    }

    protected override void AssignToTrack(RadioTrackWrapper _track)
    {
        _track.broadcasters.Add(this);
        _track.OnAddBroadcaster(this, _track);
    }

    protected override void RemoveFromTrack(RadioTrackWrapper _track)
    {
        _track.broadcasters.Remove(this);
        _track.OnRemoveBroadcaster(this, _track);
    }

    public float GetPower(Vector3 _receiverPos)
    {
        float distance = Vector3.Distance(cachedPos, _receiverPos);
        float power = Mathf.Clamp01(Mathf.InverseLerp(broadcastRadius.x, broadcastRadius.y, distance));

        return distanceFalloff.Evaluate(power);
    }
}