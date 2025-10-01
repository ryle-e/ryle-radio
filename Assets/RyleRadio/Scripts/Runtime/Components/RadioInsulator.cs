using NaughtyAttributes;
using RyleRadio.Components.Base;
using RyleRadio.Tracks;
using UnityEngine;

namespace RyleRadio.Components
{

    // an insulator for a RadioTrack- the further in the output, the quieter the track
    // this has a custom editor in RadioInsulatorEditor.cs
    [AddComponentMenu("Ryle Radio/Radio Insulator")]
    public class RadioInsulator : RadioComponentDataAccessor
    {
        // the inner and outer sizes of the insulator- if it's in the inner box, insulation power is at the maximum. if it's between the outer and inner
        // boxes, insulation power is between the minimum and the maximum. if it's outside both, insulation power is at the minimum.
        [Space(8)]
        public Vector3 innerBoxSize = Vector3.one * 0.9f;
        public Vector3 outerBoxSize = Vector3.one;

        [Space(8)]

        // the minimum and maximum insulation- how high the insulation is when the output is inside the inner box or between the inner and
        // outer boxes
        [SerializeField, MinMaxSlider(0, 1)]
        private Vector2 insulation = new(0, 0.5f);

        // the curve that defines insulation between the inner and outer boxes
        [SerializeField, CurveRange(0, 0, 1, 1)]
        private AnimationCurve insulationCurve = AnimationCurve.Linear(0, 0, 1, 1);

        // if minimum insulation is greater than 0, we need to know if it applies to all outputss outside of the insulator as well
        // if this is set to true and min insulation is set to 0.1, for example, every output outside of the insulator will still have 0.1 insulation
        // applied to it
        [SerializeField, AllowNesting, ShowIf("ShowApplyToAllOutputs")]
        private bool applyToAllOutputsOutside = false;

        // the position of this insulator at the last frame
        // we cache this as we cannot use transform.position in GetPower, as audio is on a different thread and we would otherwise get errors
        private Vector3 cachedPos;

        // the inner and outer boxes with transform.localScale applied
        public Vector3 InnerBoxSizeAdjusted { get; private set; }
        public Vector3 OuterBoxSizeAdjusted { get; private set; }

        private bool ShowApplyToAllOutputs => insulation.x > 0;


        private void Update()
        {
            // applies the local scale to the inner and outer boxes
            InnerBoxSizeAdjusted = transform.localToWorldMatrix * innerBoxSize;
            OuterBoxSizeAdjusted = transform.localToWorldMatrix * outerBoxSize;

            // caches position for this frame
            cachedPos = transform.position;
        }

        // links and unlinks this insulator to a track
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

        // gets the power of this insulator with the output at position
        public float GetPower(Vector3 _position)
        {
            float power = 0;

            // converts the boxes to bounds so that we can use the basic methods in the bounds class
            Bounds innerBounds = new Bounds(cachedPos, InnerBoxSizeAdjusted);
            Bounds outerBounds = new Bounds(cachedPos, OuterBoxSizeAdjusted);

            // if the output is in the inner bounds, insulation is at maximum
            if (innerBounds.Contains(_position))
                power = 1;

            // if the output is outside the outer bounds, insulation is at minimum
            else if (!outerBounds.Contains(_position))
                power = 0;

            // if the output is between the inner and outer bounds, calculate how far between them it is
            else
            {
                // originally i used a signed distance field for this, but for reasons i do not recall it didn't end up working as i wanted-
                // i eventually worked out a way to calculate how far between a and b the point is using rays and bounds

                // get the direction from the center of the boxes towards the given position
                Vector3 dir = (_position - outerBounds.center).normalized;

                // scale that distance by the size of the larger box so that it sits just outside (or on the corner of) the largest box
                Vector3 scaledDir = new Vector3(dir.x * outerBounds.size.x, dir.y * outerBounds.size.y, dir.z * outerBounds.size.z);

                // create a ray from the center of the box to the point just outside
                Ray ray = new Ray(outerBounds.center, scaledDir);

                // use this ray to find the distance from the center of the closest point on the outside of the inner and outer boxes
                innerBounds.IntersectRay(ray, out float distance2);
                outerBounds.IntersectRay(ray, out float distance);

                // find the closest points on the faces of the boxes using this distance
                Vector3 closestOnInner = innerBounds.center + ray.direction.normalized * -distance2;
                Vector3 closestOnOuter = outerBounds.center + ray.direction.normalized * -distance;

                // get the distance between the outer and inner points
                float outerInnerDistance = Vector3.Distance(closestOnOuter, closestOnInner);

                // get the distance from the inner to the given position
                float d = Vector3.Distance(_position, closestOnInner);

                // work out how far between the inner and outer points the given position is
                power = 1 - d / outerInnerDistance;
            }

            // adjust the power by the insulation curve
            float adjustedT = insulationCurve.Evaluate(power);

            // if it's outside the boxes and does not universally apply, set the insulation to 0
            if (adjustedT <= 0 && (ShowApplyToAllOutputs && !applyToAllOutputsOutside))
                return 0;

            // return the actual insulation using the min and max values
            return Mathf.Lerp(insulation.x, insulation.y, adjustedT);
        }
    }

}