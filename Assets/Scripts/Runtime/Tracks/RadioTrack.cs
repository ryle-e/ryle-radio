using System;
using UnityEditor;

[System.Serializable]
public abstract class RadioTrack<T> : IRadioTrack where T : RadioTrack<T>
{
    //public static string DisplayName { get; protected set; }

    public float SampleRate { get; set; }
    public virtual int SampleCount { get; set; }

    public abstract void Init();
    public abstract float GetSample(int _sampleIndex);

    public virtual void AddToPlayerEndCallback(ref Action<RadioTrackPlayer> _callback) { }
}
public static class RadioTrackInitializer
{
    static RadioTrackInitializer()
    {
        InitAll();
    }


    #if UNITY_EDITOR
    [InitializeOnLoadMethod]
    #endif
    private static void InitAll()
    {
        Type[] types = RadioUtils.FindDerivedTypes(typeof(RadioTrack<>));

        foreach (Type type in types)
        {
            type.GetProperty("DisplayName", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        }
    }
}