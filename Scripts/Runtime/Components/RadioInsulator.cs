using NaughtyAttributes;
using RyleRadio.Components.Base;
using RyleRadio.Tracks;
using UnityEngine;

namespace RyleRadio.Components
{

    /// <summary>
    /// An "insulator" for a \ref RadioTrackWrapper - if a RadioOutput is inside the bounds of this object, the affected tracks on it will become quieter.
    /// 
    /// There aren't really any real-world analogues for the strength of an insulator like this- think of it like putting a radio in a Faraday cage or something I suppose
    /// 
    /// This also has custom editor stuff in \ref RadioInsulatorEditor
    /// </summary>
    [AddComponentMenu("Ryle Radio/Radio Insulator")]
    public class RadioInsulator : RadioComponentDataAccessor
    {
        /// <summary>
        /// The size of the inner box of the insulator- inside of this box, insulation is the highest
        /// </summary>
        [Space(8)]
        public Vector3 innerBoxSize = Vector3.one * 0.9f;

        /// <summary>
        /// The size of the outer box of the insulator- outside of this box, insulation is 0- between this and the inner box, insulation is between [0 - 1]
        /// </summary>
        public Vector3 outerBoxSize = Vector3.one;

        /// <summary>
        /// The max and min insulation in the outer and inner boxes.
        /// 
        /// If an output is on the edge of the outer box, insulation will be the x-value here.
        /// If an output is inside the inner box, insulation will be the y-value.
        /// If an output is between the inner and outer boxes, it will be somewhere between the x and y values according to the \ref insulationCurve
        /// 
        /// If the x-value is above 0, you might want to toggle \ref applyToAllOutputsOutside so this becomes global
        /// </summary>
        [Space(8)]
        [SerializeField, MinMaxSlider(0, 1)]
        private Vector2 insulation = new(0, 0.5f);

        /// <summary>
        /// A curve that defines insulation between the inner and outer boxes- the x-value is how far between the inner and outer boxes the output is
        /// </summary>
        [SerializeField, CurveRange(0, 0, 1, 1)]
        private AnimationCurve insulationCurve = AnimationCurve.Linear(0, 0, 1, 1);

        /// <summary>
        /// Effectively makes this insulator global- when this is true, any output outside of the outer box's range will be affected by the x-value on \ref insulation
        /// 
        /// This only shows when the x-value of insulation is above 0
        /// </summary>
        [SerializeField, AllowNesting, ShowIf("ShowApplyToAllOutputs")]
        private bool applyToAllOutputsOutside = false;

        /// <summary>
        /// The position of the insulator in the previous frame. We cannot access transform.position in the audio thread, so we cache it to this
        /// </summary>
        private Vector3 cachedPos;

        // the inner and outer boxes with transform.localScale applied
        /// The \ref innerBoxSize but adjusted with `transform.localScale` in \ref Update()
        public Vector3 InnerBoxSizeAdjusted { get; private set; }
        /// The \ref outerBoxSize but adjusted with `transform.localScale` in \ref Update()
        public Vector3 OuterBoxSizeAdjusted { get; private set; }

        /// <summary>
        /// Shows \ref applyToAllOutputsOutside when the x-value of \ref insulation is greater than 0
        /// </summary>
        private bool ShowApplyToAllOutputs => insulation.x > 0;


        /// <summary>
        /// Updates cached position and adjusted box sizes
        /// </summary>
        private void Update()
        {
            // applies the local scale to the inner and outer boxes
            InnerBoxSizeAdjusted = transform.localToWorldMatrix * innerBoxSize;
            OuterBoxSizeAdjusted = transform.localToWorldMatrix * outerBoxSize;

            // caches position for this frame
            cachedPos = transform.position;
        }

        /// <summary>
        /// Links this insulator to a track
        /// </summary>
        /// <param name="_track">The track to link to</param>
        protected override void AssignToTrack(RadioTrackWrapper _track)
        {
            _track.insulators.Add(this);
            _track.OnAddInsulator(this, _track);
        }

        /// <summary>
        /// Unlinks this insulator from a track
        /// </summary>
        /// <param name="_track">The track to unlink from</param>
        protected override void RemoveFromTrack(RadioTrackWrapper _track)
        {
            _track.insulators.Remove(this);
            _track.OnRemoveInsulator(this, _track);
        }

        /// <summary>
        /// Gets the power of this insulator at a specific position
        /// </summary>
        /// <param name="_position">The position to evaluate with</param>
        /// <returns>The insulation multiplier at the position. The lower the number, the stronger the insulation- it's a multiplier intended to shrink the samples rather than grow them</returns>
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