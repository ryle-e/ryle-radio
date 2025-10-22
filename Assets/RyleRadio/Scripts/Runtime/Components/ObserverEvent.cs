using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace RyleRadio.Components
{

    // doing this in a partial class as this is effectively an extension of RadioObserver in a different script for cleanliness
    public partial class RadioObserver
    {
        /// <summary>
        /// A singular event inside a \ref RadioObserver, tracking a value or trigger and invoking methods accordingly
        /// </summary>
        [System.Serializable]
        public class ObserverEvent
        {
            /// <summary>
            /// The type of event this is looking for
            /// </summary>
            public EventType eventType;

            /// <summary>
            /// The method of comparisonType used in this event- not shown if a Trigger event is chosen
            /// </summary>
            [AllowNesting, ShowIf("NeedComparison")]
            public ComparisonType comparisonType;

            /// <summary>
            /// A value clamped between 0 and 1 to use in the comparison.
            /// <br><br>Shows when a comparison is needed, it does not use a range, and the eventType is not set to Tune.
            /// </summary>
            [AllowNesting, ShowIf(EConditionOperator.And, "NeedComparison", "NotNeedVector", "NotIsTune"), Range(0, 1)]
            public float clampedValue;

            /// <summary>
            /// A value clamped between 0 and 1000 to use in the comparison.
            /// <br><br>Shows when a comparison is needed, it does not use a range, and the eventType is set to Tune.
            /// </summary>
            [AllowNesting, ShowIf(EConditionOperator.And, "NeedComparison", "NotNeedVector", "IsTune"), Range(0.0f, 1000.0f)]
            public float tuneValue;

            /// <summary>
            /// A range between 0 and 1 to use in the comparisonType.
            /// <br><br>Shows when a comparison is needed, it needs a range, and the eventType is not set to Tune.
            /// </summary>
            [AllowNesting, ShowIf(EConditionOperator.And, "NeedComparison", "NeedVector", "NotIsTune"), MinMaxSlider(0, 1)]
            public Vector2 clampedRange;

            /// <summary>
            /// A range between 0 and 1000 to use in the comparison.
            /// <br><br>Shows when a comparison is needed, it needs a range, and the eventType is set to Tune.
            /// </summary>
            [AllowNesting, ShowIf(EConditionOperator.And, "NeedComparison", "NeedVector", "IsTune"), MinMaxSlider(0, 1000)]
            public Vector2 tuneRange;

            /// <summary>
            /// Show/hide events- useful when there's a lot of them
            /// </summary>
            public bool showEvents;

            /// <summary>
            /// Called when \ref eventType is fulfilled for the first time. Only called once until \ref onEnd is called
            /// </summary>
            [ShowIf("showEvents"), AllowNesting]
            public UnityEvent<float> onTrigger;

            /// <summary>
            /// Called every frame while \ref eventType is fulfilled, including the first. Cannot be called if \ref eventType is a Trigger event
            /// </summary>
            [ShowIf(EConditionOperator.And, "showEvents", "NeedComparison"), AllowNesting]
            public UnityEvent<float> onStay;

            /// <summary>
            /// Called when \ref eventType is no longer fulfilled. Only called once until the event is fulfilled again. Cannot be called if \ref eventType is a Trigger event
            /// </summary>
            [ShowIf(EConditionOperator.And, "showEvents", "NeedComparison"), AllowNesting]
            public UnityEvent<float> onEnd;

            /// <summary>
            /// Tracks if this event has been called in this or the previous frame
            /// </summary>
            [HideInInspector] public bool staying = false;

            /// Does this event not need a value or range to compare to?
            public bool NotNeedComparison => !NeedComparison;
            /// Does this event need a value or range to compare to?
            public bool NeedComparison =>
                eventType == EventType.OutputVolume
                || eventType == EventType.Gain
                || eventType == EventType.TunePower
                || eventType == EventType.BroadcastPower
                || eventType == EventType.Insulation
                || eventType == EventType.OutputTune;

            /// Does this event not need a range to compare to, and uses a single value instead?
            public bool NotNeedVector => !NeedVector;
            /// Does this event need a range to compare to, not a single value?
            public bool NeedVector =>
                comparisonType == ComparisonType.BetweenInclusive
                || comparisonType == ComparisonType.BetweenExclusive;

            /// Does this event not use Tune?
            public bool NotIsTune => !IsTune;
            /// Does this event use Tune?
            public bool IsTune =>
                eventType == EventType.OutputTune;


            /// <summary>
            /// If this event uses a comparison, check if its fulfilled
            /// </summary>
            /// <param name="_cValue">The value to compare against</param>
            /// <returns>True if the comparison is fulfilled, false if not</returns>
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
                return comparisonType switch
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

            /// <summary>
            /// Makes the event more readable when printed, in the form "{eventType}/{comparisonType}"
            /// </summary>
            public override string ToString()
            {
                return eventType + "/" + comparisonType;
            }
        }
    }

}