using System;
using UnityEngine;

public class ClipTest : MonoBehaviour
{
    [SerializeField] private AudioClip clip;

    private float[] samples;
    private float prog = 0;
    private float ratio;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        Debug.Log("doing thign");

        samples = new float[clip.samples * clip.channels]; // samples combined to one channel

        Debug.Log($"there are {samples.Length} samples");

        Debug.Log(clip.frequency + " " + AudioSettings.outputSampleRate);
        ratio = 1 / ((float)AudioSettings.outputSampleRate / clip.frequency);

        // if the clip is invalid for some reason, tell the user
        if (!clip.GetData(samples, 0))
        {
            Debug.LogError("Cannot access clip data from track " + clip.name);
            return;
        }
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        int monoSampleCount = data.Length / channels;

        // for every sample in the data array
        for (int index = 0; index < data.Length; index++)
        {
            lock (samples)
            {
                data[index] += samples[(int)prog];
            }
            prog += ratio / 2;

            if (prog > samples.Length)
                prog = 0;
        }
    }
}
