using UnityEngine;

public class ObserverTest : MonoBehaviour
{
    public void Volume(int _stage)
    {
        Debug.Log($"volume {_stage}");
    }

    public void Gain(int _stage)
    {
        Debug.Log($"gain {_stage}");
    }

    public void BroadcastPower(int _stage)
    {
        Debug.Log($"broadcast power {_stage}");
    }

    public void Insulation(int _stage)
    {
        Debug.Log($"insulation {_stage}");
    }

    public void TrackEnd()
    {
        Debug.Log($"track ended");
    }

    public void TrackStart()
    {
        Debug.Log($"track started");
    }

    public void Tune()
    {
        Debug.Log("tune in range");
    }

    public void Log(string msg)
    {
        Debug.Log(msg);
    }
}
