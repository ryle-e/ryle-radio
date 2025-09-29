
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

// an expansion of a RadioComponent that can access specific tracks
public abstract class RadioOutputTrackAccessor : MonoBehaviour
{
    [SerializeField] protected RadioOutput output;

    // the tracks to choose from on the RadioData
    protected List<string> TrackNames => output != null 
        ? output.Data.TrackNames 
        : new() { "Output not assigned!" };


    protected void DoTrackAction(int _trackMask, Action<string> _action)
    {
        int[] wrapperIndexes = MultiselectAttribute.ToInt(_trackMask);

        foreach (int index in wrapperIndexes)
            _action.Invoke(output.Data.TrackIDs[index]);
            
    }
}