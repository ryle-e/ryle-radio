using NaughtyAttributes;
using RyleRadio.Components.Base;
using RyleRadio.Tracks;
using UnityEngine;

namespace RyleRadio.Components
{

    /// <summary>
    /// A "broadcaster" for a \ref RadioTrackWrapper - the closer the \ref RadioOutput that's playing the track is to a broadcaster (taking into consideration broadcast radius, power, etc), the louder the track is in the Output.
    /// 
    /// Somewhat similar to real world broadcasters- at least, that was the original intention :)
    /// 
    /// This has a custom editor in \ref RadioBroadcasterEditor
    /// </summary>
    [AddComponentMenu("Ryle Radio/Radio Broadcaster")]
    public class RadioBroadcaster : RadioComponentDataAccessor
    {
        /// <summary>
        /// The inner and outer radii of this broadcaster.
        /// 
        /// <i>radiusProg is the x-value in the \ref distanceFalloff that we use to evaluate broadcast power</i>
        /// If the Output is within the inner radius, the radiusProg is 1.
        /// If the Output is between the inner and outer radii, the radiusProg is between 0 and 1.
        /// If the Output is outside both radii, the radiusProg is 0.
        /// </summary>
        [Space(8)]
        public Vector2 broadcastRadius;

        /// <summary>
        /// The power of this broadcaster at the maximum and minimum radii.
        /// 
        /// The x-value is the broadcast power right on the outer radius, and the y-value is the broadcast power within the inner radius.
        /// Broadcast power between the inner and outer radii will be somewhere in this range, defined by the \ref distanceFalloff
        /// 
        /// If the x-value is greater than 0, you might want to enable \ref applyToAllOutputsOutside so that all Outputs are affected by this broadcaster.
        /// </summary>
        [Space(8), MinMaxSlider(0, 1), SerializeField]
        protected Vector2 broadcastPowers = new(0, 1);

        /// <summary>
        /// The falloff curve between the inner and outer broadcast radii- the x-value is how far between the radii the Output is, and the y-value is how far between \ref broadcastPowers x and y the track's broadcast power is
        /// </summary>
        [SerializeField, CurveRange(0, 0, 1, 1)]
        protected AnimationCurve distanceFalloff = new(new Keyframe[2] {
            new(0, 1, 0, 0),
            new(1, 0, 0, 0)
        });

        /// <summary>
        /// If the x-value of \ref broadcastPowers should be applied to all Outputs outside of this broadcaster's radii- effectively makes it global
        /// </summary>
        [SerializeField, AllowNesting, ShowIf("ShowApplyToAllOutputs")]
        private bool applyToAllOutputsOutside = false;

        /// <summary>
        /// The position of the broadcaster in the previous frame- we can't access `transform.position` in the audio thread, so we need to cache it in update
        /// </summary>
        private Vector3 cachedPos;

        /// <summary>
        /// Shows \ref applyToAllOutputsOutside when the x-value of \ref broadcastPowers is greater than 0
        /// </summary>
        private bool ShowApplyToAllOutputs => broadcastPowers.x > 0;


        /// <summary>
        /// Updates the position of this broadcaster
        /// </summary>
        private void Update()
        {
            // cache the position
            cachedPos = transform.position;
        }

        /// <summary>
        /// Links this broadcaster to a track
        /// </summary>
        /// <param name="_track">The track to link to</param>
        protected override void AssignToTrack(RadioTrackWrapper _track)
        {
            _track.broadcasters.Add(this);
            _track.OnAddBroadcaster(this, _track);
        }

        /// <summary>
        /// Unlinks this broadcaster from a track
        /// </summary>
        /// <param name="_track">The track to unlink from</param>
        protected override void RemoveFromTrack(RadioTrackWrapper _track)
        {
            _track.broadcasters.Remove(this);
            _track.OnRemoveBroadcaster(this, _track);
        }

        /// <summary>
        /// Gets the broadcast power of this particular broadcaster using the Output's position
        /// </summary>
        /// <remarks></remarks>
        /// <param name="_receiverPos">The position of the Output</param>
        /// <returns>A value between 0 and 1, used as a multiplier to the loudness of a track</returns>
        public float GetPower(Vector3 _receiverPos)
        {
            // the distance between the output and this broadcaster
            float distance = Vector3.Distance(cachedPos, _receiverPos);

            // how far between the inner and outer radii the distance is
            float radiusProg = Mathf.Clamp01(Mathf.InverseLerp(broadcastRadius.x, broadcastRadius.y, distance));

            // get the adjusted power according to the distance falloff
            float adjustedT = distanceFalloff.Evaluate(radiusProg);

            // if the output is outside of the range of this broadcaster and it's not global when outside, return a power of 0
            if (adjustedT <= 0 && (ShowApplyToAllOutputs && !applyToAllOutputsOutside))
                return 0;

            // return the actual broadcast power according to values supplied in the broadcastPowers variable
            return Mathf.Lerp(broadcastPowers.x, broadcastPowers.y, adjustedT);
        }
    }

}