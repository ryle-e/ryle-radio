using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class TrackSelectorTest : MonoBehaviour
{
    public RadioData data;

    [MultiRadioTrackSelector("data")]
    public int tracks;

    public List<string> TrackNames => data.TrackNames;
}
