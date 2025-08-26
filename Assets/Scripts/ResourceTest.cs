using System.Linq;
using UnityEngine;

public class ResourceTest : MonoBehaviour
{
    public AudioSource source;

    public AudioClip sourceClip1;
    public AudioClip sourceClip2;

    private float[] sourceData1;
    private float[] sourceData2;

    private int sampleRate;

    private int startSample;
    private int repeatRate;

    private float samplePos = 0;// the progression through the source data in realtime- this is added to over the course of every OnAudioFilterRead
                                // each OnAudioFilterRead, a buffer of samples of length n is passed in
                                // the samplepos is increased by this value n each event call, and is reset when it exceeds the length of the source data- that is, it loops

    void Awake()
    {
        // First, we need to fetch data from our two source clips. Allocate an array
        // to store the data from the audio clips. "AudioClip.samples" returns the number of samples
        // in the audio clip, so we can just allocate enough space for the whole clip.
        sourceData1 = new float[sourceClip1.samples * sourceClip1.channels];
        sourceData2 = new float[sourceClip2.samples * sourceClip2.channels];

        // Now, extract the audio data from our source clips.
        // "GetData" returns false if the data couldn't be read. Usually this is because it's
        // compressed, or not loaded.
        if (!sourceClip1.GetData(sourceData1, 0))
            Debug.Log("Uh oh, sourceClip1 was unreadable!");
        if (!sourceClip2.GetData(sourceData2, 0))
            Debug.Log("Uh oh, sourceClip2 was unreadable!");

        //sampleRate = (int)(sourceClip1.samples / sourceClip1.length);
        sampleRate = AudioSettings.outputSampleRate;
        Debug.Log(sampleRate + " " + AudioSettings.outputSampleRate + " " + AudioSettings.dspTime);
        //sampleRate = sourceClip1.frequency;

        // We want our beats to repeat at a known interval, so let's just have them play
        // once every half a second or so. We need this value in samples, not seconds, so
        // we have to multiply by the sample rate.
        repeatRate = (int)(2f * sampleRate);
    }
    void OnAudioFilterRead(float[] data, int channels)
    {
        for (int i = 0; i < data.Length; i += channels)
        {
            if (samplePos < sourceData1.Count())
            {
                for (int c = i; c < i + channels; c++)
                {
                    data[c] += sourceData1[(int) samplePos];
                }
            }
            else
            {
                samplePos = 0;
                break;
            }

            samplePos += channels;

            //data[i] = 0;

            // Since we want the drum beat audio to repeat at regular intervals, 
            // we need to calculate where in the source data we want to copy from.
            // Since we don't want to read out of the bounds of the array, just clamp the
            // index to the maximum length of the sound.
            //var sourceSample1 = ;

            // Let's have the second drum sound fall between beats. Just add half the repeat
            // rate to our index.
            //var sourceSample2 = Mathf.Min((startSample + repeatRate / 2 + i) % repeatRate, sourceData2.Length - 1);

            // Finally, add the audio samples from our sources into the final mix!
            //int n = GetSourceSample(i);
            //data[i] += Mathf.Clamp(sourceData1[GetSourceSample(i)], -1, 1);
            //data[i + 1] += Mathf.Clamp(sourceData1[GetSourceSample(i + 1)], -1, 1);
            //data[i] += sourceData2[sourceSample2];



            /*
             for (int i = 0; i < data.Length; i += channels)
            {
                int sampleIndex = (int)samplePos;//GetSourceSample(i);

                if (sampleIndex < sourceData1.Count())
                {
                    for (int c = i; c < i + channels; c++)
                    {
                        data[c] = sourceData1[sampleIndex];
                    }
                }
                else
                {
                    samplePos = 0;
                    break;
                }
            }

            samplePos += channels;
             */
        }
    }

    private int GetSourceSample(int i)
    {
        return Mathf.Min((startSample + i) % repeatRate, sourceData1.Length - 1);
    }
}