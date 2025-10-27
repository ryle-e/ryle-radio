using System;
using System.Collections.Generic;
using UnityEngine;

namespace RyleRadio.Components.Base
{

    /// <summary>
    /// A sister class to \ref RadioComponent that allows a component to access specific tracks on a \ref RadioOutput in the inspector using an int with a \ref MultiselectAttribute <br><b>See: </b>\ref RadioInsulator
    /// </summary>
    public abstract class RadioOutputTrackAccessor : MonoBehaviour
    {
        /// <summary>
        /// The RadioOutput to get tracks from
        /// </summary>
        [SerializeField] protected RadioOutput output;

        /// <summary>
        /// The names of tracks available in the Output
        /// </summary>
        protected List<string> TrackNames => output != null
            ? output.Data.TrackNames
            : new() { "Output not assigned!" };

        /// <summary>
        /// Performs an action on any selected tracks using a \ref MultiselectAttribute
        /// </summary>
        /// <param name="_trackMask">The int with the \ref MultiselectAttribute that the player uses to select tracks from the Output</param>
        /// <param name="_action">The action to perform on the tracks</param>
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