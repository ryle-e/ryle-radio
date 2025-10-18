using NaughtyAttributes;
using RyleRadio.Components.Base;
using RyleRadio.Tracks;
using UnityEngine;

namespace RyleRadio.Components
{

    // a broadcaster for a RadioTrack- the closer the output is, the louder the track
    // this has a custom inspector in RadioBroadcasterEditor.cs
    [AddComponentMenu("Ryle Radio/Radio Broadcaster")]
    public class RadioBroadcaster : RadioComponentDataAccessor
    {
        // the inner and outer radii of this broadcaster- if the output is in the inner radius, the broadcast radiusProg is 1. if it's between the inner
        // and outer radii, the radiusProg is between 0 and 1. if it's outside both, the radiusProg is 0.
        [Space(8)]
        public Vector2 broadcastRadius;

        // the min and max broadcast power of this broadcaster- the first value is the power right on the outer edge of the broadcaster, and the second is the
        // power in the inner radius. if the outer power is > 0, you can make it global in applyToAllOutputsOutside
        [Space(8), MinMaxSlider(0, 1), SerializeField]
        protected Vector2 broadcastPowers = new(0, 1);

        // the falloff between the inner and outer broadcast ranges- you probably don't need to touch this but it's here if needed
        [SerializeField, CurveRange(0, 0, 1, 1)]
        protected AnimationCurve distanceFalloff = new(new Keyframe[2] {
            new(0, 1, 0, 0),
            new(1, 0, 0, 0)
        });

        // if the min broadcast power is > 0, it means that the broacast doesn't entirely stop once it hits the edge- this means we could make it
        // global when outside the broadcast radius.
        [SerializeField, AllowNesting, ShowIf("ShowApplyToAllOutputs")]
        private bool applyToAllOutputsOutside = false;

        // the position of the broadcaster at the last frame
        // we cache this as we cannot use transform.position in GetPower, as audio is on a different thread and we would otherwise get errors
        private Vector3 cachedPos;

        private bool ShowApplyToAllOutputs => broadcastPowers.x > 0;


        private void Update()
        {
            // cache the position
            cachedPos = transform.position;
        }

        // link and unlink this broadcaster to a track
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

        // get the radiusProg of the output at a certain position relative to this broadcaster
        // i.e a value from 0-1 for how close the output is to this broadcaster, that can be used as a multiplier to the audio
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