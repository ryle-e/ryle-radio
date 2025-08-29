using System.Collections.Generic;
using UnityEngine;

public class TrackSelectorTest : MonoBehaviour
{
    public RadioData data;

    [MultiRadioTrackSelector("data")]
    public int tracks;

    public List<string> names;

    [Multiselect("names")]
    public int selectedNames;

    // some stuff about this https://stackoverflow.com/questions/22513519/how-can-i-convert-an-int-into-its-corresponding-set-of-bitwise-flags-without-any

    public List<string> TrackNames => data.TrackNames;

    private void Start()
    {
        foreach (string s in MultiselectAttribute.To<string>(selectedNames, names.ToArray()))
            Debug.Log(s);
    }
}
