using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyleRadio.Components.Base
{

    // a sister class to RadioComponent that can access specific tracks on a RadioOutput
    public abstract class RadioOutputTrackAccessor : MonoBehaviour
    {
        // the output to pull tracks from
        [SerializeField] protected RadioOutput output;

        // the tracks to choose from on the output's RadioData
        protected List<string> TrackNames => output != null
            ? output.Data.TrackNames
            : new() { "Output not assigned!" };

        // performs an action using the selected tracks as a multiselect
        protected void DoTrackAction(int _trackMask, Action<string> _action)
        {
            // convert the multiselect to a list of track indexes
            int[] wrapperIndexes = MultiselectAttribute.ToInt(_trackMask);

            // perform the action on each track selected using their indexes
            foreach (int index in wrapperIndexes)
                _action.Invoke(output.Data.TrackIDs[index]);
        }
    }

}