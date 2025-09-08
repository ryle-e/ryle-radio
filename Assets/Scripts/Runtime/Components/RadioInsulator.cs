using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;


[AddComponentMenu("Ryle Radio/Radio Insulator")]
public class RadioInsulator : RadioComponentTrackAccessor
{
    [Space(8)]
    public Vector3 innerBoxSize = Vector3.one * 0.8f;
    public Vector3 outerBoxSize = Vector3.one;

    [Space(8)]
    [SerializeField, MinMaxSlider(0, 1)] 
    private Vector2 insulation = new(0, 0.5f);

    [SerializeField, CurveRange(0, 0, 1, 1)]
    private AnimationCurve insulationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    private Vector3 cachedPos;


    public Vector3 InnerBoxSizeAdjusted { get; private set; }
    public Vector3 OuterBoxSizeAdjusted { get; private set; }


    private void Update()
    {
        InnerBoxSizeAdjusted = transform.localToWorldMatrix * innerBoxSize;
        OuterBoxSizeAdjusted = transform.localToWorldMatrix * outerBoxSize;

        cachedPos = transform.position;
    }

    protected override void AssignToTrack(RadioTrackWrapper _track)
    {
        _track.insulators.Add(this);
        _track.OnAddInsulator(this, _track);
    }

    protected override void RemoveFromTrack(RadioTrackWrapper _track)
    {
        _track.insulators.Remove(this);
        _track.OnRemoveInsulator(this, _track);
    }

    public float GetPower(Vector3 _position)
    {
        float t = 0;

        Bounds innerBounds = new Bounds(cachedPos, InnerBoxSizeAdjusted);
        Bounds outerBounds = new Bounds(cachedPos, OuterBoxSizeAdjusted);

        if (innerBounds.Contains(_position))
            t = 1;

        else if (!outerBounds.Contains(_position))
            t = 0;

        else
        {
            Vector3 dir = (_position - outerBounds.center).normalized;
            Vector3 scaledDir = new Vector3(dir.x * outerBounds.size.x, dir.y * outerBounds.size.y, dir.z * outerBounds.size.z);

            Ray ray = new Ray(outerBounds.center, scaledDir);

            innerBounds.IntersectRay(ray, out float distance2);
            outerBounds.IntersectRay(ray, out float distance);

            Vector3 closestOnInner = innerBounds.center + ray.direction.normalized * -distance2;
            Vector3 closestOnOuter = outerBounds.center + ray.direction.normalized * -distance;

            float outerInnerDistance = Vector3.Distance(closestOnOuter, closestOnInner);
            float d = Vector3.Distance(_position, closestOnInner);

            t = 1 - d / outerInnerDistance;
        }

        float adjustedT = insulationCurve.Evaluate(t);
        return Mathf.Lerp(insulation.x, insulation.y, adjustedT);
    }
}
