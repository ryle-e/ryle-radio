using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace RyleRadio.Components
{

    // doing this in a partial class as this is effectively an extension of RadioObserver in a different script for cleanliness
    public partial class RadioObserver
    {
        // a singular event inside a RadioObserver, tracking a variable for a certain happening
        [System.Serializable]
        public class ObserverEvent
        {
            public EventType type;

            [AllowNesting, ShowIf("NeedComparison")]
            public ComparisonType comparison;

            // a value to compare to- clamped between 0 and 1
            // shows if the event has a comparison, is not using a range, and is not set to tune
            [AllowNesting, ShowIf(EConditionOperator.And, "NeedComparison", "NotNeedVector", "NotIsTune"), Range(0, 1)]
            public float clampedValue;

            // a value to compare to- clamped between 0 and 1000
            // shows if the event has a comparison, is not using a range, and is set to tune
            [AllowNesting, ShowIf(EConditionOperator.And, "NeedComparison", "NotNeedVector", "IsTune"), Range(0.0f, 1000.0f)]
            public float tuneValue;

            // a range to compare to- clamped between 0 and 1
            // shows if the event has a comparison, is using a range, and is not set to tune
            [AllowNesting, ShowIf(EConditionOperator.And, "NeedComparison", "NeedVector", "NotIsTune"), MinMaxSlider(0, 1)]
            public Vector2 clampedRange;

            // a range to compare to- clamped between 0 and 100
            // shows if the event has a comparison, is using a range, and is set to tune
            [AllowNesting, ShowIf(EConditionOperator.And, "NeedComparison", "NeedVector", "IsTune"), MinMaxSlider(0, 1000)]
            public Vector2 tuneRange;

            // for inspector cleanliness, show/hide the events as they are quite big
            public bool showEvents;

            // called when the event is triggered
            // if this has a comparison, then it can be triggered more than once while the comparison is true- e.g the volume can remain above 0.5
            // therefore, with a comparison, this changes to mean the FIRST time the event is triggered
            [ShowIf("showEvents"), AllowNesting]
            public UnityEvent<float> onTrigger;

            // called when the event remains triggered
            // this is not called without a comparison
            // with a comparison, this is called if the event is triggered, and has been triggered just before
            [ShowIf(EConditionOperator.And, "showEvents", "NeedComparison"), AllowNesting]
            public UnityEvent<float> onStay;

            // called when the event ends
            // this is not called without a comparison
            [ShowIf(EConditionOperator.And, "showEvents", "NeedComparison"), AllowNesting]
            public UnityEvent<float> onEnd;

            // tracks if the event has happened before
            [HideInInspector] public bool staying = false;

            // if this event needs a value to compare to
            public bool NotNeedComparison => !NeedComparison;
            public bool NeedComparison =>
                type == EventType.OutputVolume
                || type == EventType.Gain
                || type == EventType.TunePower
                || type == EventType.BroadcastPower
                || type == EventType.Insulation
                || type == EventType.OutputTune;

            // if this event needs a range to compare to
            public bool NotNeedVector => !NeedVector;
            public bool NeedVector =>
                comparison == ComparisonType.BetweenInclusive
                || comparison == ComparisonType.BetweenExclusive;

            // if this event is set to track OutputTune
            public bool NotIsTune => !IsTune;
            public bool IsTune =>
                type == EventType.OutputTune;


            // if this event is a comparison, check if the comparison is satisfied
            public bool EvaluateComparison(float _cValue)
            {
                // if the event is not a comparison you should not be calling this method (unless it's being used on many events at once)
                if (NotNeedComparison)
                    return false;

                float value;
                Vector2 range;

                // if this event uses OutputTune, then make sure we're comparing to the tune values
                if (IsTune)
                {
                    range = tuneRange;
                    value = tuneValue;
                }
                else
                {
                    value = clampedValue;
                    range = clampedRange;
                }

                // evaluate the specific comparison- just logical operators
                return comparison switch
                {
                    ComparisonType.Equal => _cValue == value,
                    ComparisonType.GreaterThan => _cValue > value,
                    ComparisonType.GreaterThanOrEqual => _cValue >= value,
                    ComparisonType.LessThan => _cValue < value,
                    ComparisonType.LessThanOrEqual => _cValue <= value,

                    ComparisonType.BetweenExclusive => range.x < _cValue && _cValue < range.y, // i want (x < value < y) notation >:)
                    ComparisonType.BetweenInclusive => range.x <= _cValue && _cValue <= range.y,

                    _ => false,
                };
            }

            public override string ToString()
            {
                return type + "/" + comparison;
            }
        }
    }

}