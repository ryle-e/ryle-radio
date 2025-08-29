using NaughtyAttributes;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class RadioComponentShape
{
    [SerializeField] private bool isSphere;


    [SerializeField, HideIf("isSphere")] private Bounds box;

    [SerializeField, ShowIf("isSphere")] private Vector3 position;
    [SerializeField, ShowIf("isSphere")] private float radius;

    [SerializeField, HideIf("isSphere"), ShowIf("hideInnerShape")] private Bounds innerBox;
    [SerializeField, HideIf("hideInnerShape"), ShowIf("isSphere")] private float innerRadius;

    [SerializeField, ShowIf("hasInnerShape")] 
    private AnimationCurve falloff;

    private bool hasInnerShape;


    public RadioComponentShape(bool _hasInnerShape)
    {
        hasInnerShape = _hasInnerShape;

        box = new Bounds(Vector3.zero, Vector3.one);
        innerBox = new Bounds(Vector3.zero, Vector3.one * 0.8f);

        position = Vector3.zero;

        radius = 1;
        innerRadius = 0.8f;

        falloff = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }

    public float GetPower(Vector3 _position)
    {
        if (isSphere)
        {
            float distanceToCenter = Vector3.Distance(_position, position);
            float clampedDistance = Mathf.Clamp(distanceToCenter, innerRadius, radius);
            float distancePower = RadioUtils.Remap(clampedDistance, innerRadius, 0, radius, 1);

            float power = falloff.Evaluate(distancePower);

            return power;
        }
        else
        {
            float distToOuter = Mathf.Sqrt(box.SqrDistance(_position));
            float distToInner = Mathf.Sqrt(innerBox.SqrDistance(_position));

            if (innerBox.Contains(_position))
                return falloff.Evaluate(1);

            else if (box.Contains(_position))
                return falloff.Evaluate(distToInner / distToOuter);

            else
                return falloff.Evaluate(0);
        }
    }
}
